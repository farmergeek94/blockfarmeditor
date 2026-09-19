using BlockFarmEditor.Umbraco.Core.Attributes;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using BlockFarmEditor.Umbraco.Core.Models.ConfigModels;
using BlockFarmEditor.Umbraco.Library.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Editors;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace BlockFarmEditor.Umbraco.Library.Services
{
    internal class BlockPropertyValueMapper(
        IServiceProvider serviceProvider,
        IContentTypeService contentTypeService,
        IPublishedContentTypeCache publishedContentTypeCache,
        IPublishedModelFactory publishedModelFactory,
        IVariationContextAccessor variationContextAccessor,
        PropertyEditorCollection propertyEditors,
        IDataTypeConfigurationCache dataTypeConfigurationCache,
        IConfigurationEditorJsonSerializer configurationEditorJsonSerializer,
        IBlockDefinitionService blockDefinitionService,
        ILogger<BlockPropertyValueMapper> logger) : IBlockPropertyValueMapper
    {
        // State for a single pass over a tree, so configurations are only resolved once per content type property.
        private sealed class MapContext(bool toEditor, Dictionary<string, IEnumerable<BlockFarmEditorConfigurationAttribute>> configMaps)
        {
            public bool ToEditor { get; } = toEditor;
            public Dictionary<string, IEnumerable<BlockFarmEditorConfigurationAttribute>> ConfigMaps { get; } = configMaps;
            public Dictionary<(string ContentTypeAlias, string PropertyAlias), object?> Configurations { get; } = [];
        }

        public void FromEditor(BlockData root) => MapBlock(root, new MapContext(false, blockDefinitionService.GetConfigMaps()));

        public void ToEditor(BlockData root) => MapBlock(root, new MapContext(true, blockDefinitionService.GetConfigMaps()));

        private void MapBlock(BlockData block, MapContext context)
        {
            if (block.Blocks != null)
            {
                block.Blocks.RemoveAll(x => x == null);
                foreach (var child in block.Blocks)
                {
                    MapBlock(child!, context);
                }
            }

            if (block.Properties == null || !Guid.TryParse(block.ContentTypeKey, out var contentTypeKey))
                return;

            var contentType = contentTypeService.Get(contentTypeKey);

            if (contentType == null)
                return;

            block.Properties = MapProperties(contentType, block.Properties, context);
        }

        private Dictionary<string, object?> MapProperties(IContentType contentType, Dictionary<string, object?> properties, MapContext context)
        {
            var result = new Dictionary<string, object?>();

            foreach (var propertyType in contentType.CompositionPropertyTypes)
            {
                try
                {
                    if (!propertyEditors.TryGet(propertyType.PropertyEditorAlias, out var propertyEditor))
                    {
                        logger.LogWarning("Property editor '{EditorAlias}' not found for property {PropertyName}", propertyType.PropertyEditorAlias, propertyType.Alias);
                        continue;
                    }

                    var property = properties.FirstOrDefault(x => x.Key.InvariantEquals(propertyType.Alias));

                    if (property.Key == null)
                    {
                        continue;
                    }

                    var valueEditor = propertyEditor.GetValueEditor();

                    if (context.ToEditor)
                    {
                        var propertyValue = Property.CreateWithValues(-1, AsInvariant(propertyType), new Property.InitialPropertyValue(null, null, false, property.Value));
                        result[property.Key] = valueEditor.ToEditor(propertyValue);
                    }
                    else
                    {
                        var configuration = GetConfiguration(contentType, propertyType, propertyEditor, context);
                        result[property.Key] = valueEditor.FromEditor(new ContentPropertyData(property.Value, configuration), property.Value);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error mapping property {PropertyName} {Direction} the editor", propertyType.Alias, context.ToEditor ? "to" : "from");
                }
            }
            return result;
        }

        // Block values are stored invariantly in the page json, so variance on the element type is ignored - a property will not hand back an invariant value for a type that varies by culture.
        private static IPropertyType AsInvariant(IPropertyType propertyType)
        {
            if (!propertyType.VariesByCulture())
            {
                return propertyType;
            }

            // the content type service hands out cached instances, so the variance is changed on a copy.
            var invariant = (IPropertyType)propertyType.DeepClone();
            invariant.Variations = ContentVariation.Nothing;
            return invariant;
        }

        public PageDefinition ToPageDefinition(BlockData root)
        {
            var result = new PageDefinition
            {
                Blocks = ToBlockDefinitions(root.Blocks)
            };

            if (Guid.TryParse(root.Unique, out var unique))
            {
                result.Unique = unique;
            }

            return result;
        }

        private List<BlockDefinition<IPublishedElement>> ToBlockDefinitions(List<BlockData?>? blocks) =>
            [.. (blocks ?? []).Select(x => x == null ? null : ToBlockDefinition(x)).OfType<BlockDefinition<IPublishedElement>>()];

        public BlockDefinition<IPublishedElement>? ToBlockDefinition(BlockData block)
        {
            if (!Guid.TryParse(block.Unique, out var unique))
            {
                return null;
            }

            var result = new BlockDefinition<IPublishedElement>
            {
                Blocks = ToBlockDefinitions(block.Blocks),
                Unique = unique
            };

            if (!Guid.TryParse(block.ContentTypeKey, out var contentTypeKey))
                return result;

            var contentType = publishedContentTypeCache.Get(PublishedItemType.Element, contentTypeKey);
            if (contentType == null)
                return result;

            var sourceValues = new Dictionary<string, object?>();

            if (block.Properties != null)
            {
                foreach (var propertyType in contentType.PropertyTypes)
                {
                    var property = block.Properties.FirstOrDefault(x => x.Key.InvariantEquals(propertyType.Alias));
                    if (property.Key == null)
                    {
                        continue;
                    }

                    // Store the source value - let Umbraco's property value converters handle the rest
                    sourceValues[propertyType.Alias] = property.Value;
                }
            }

            var element = new PublishedElement(contentType, unique, sourceValues, false, PropertyCacheLevel.Element, variationContextAccessor.VariationContext!, null);

            // Use model factory to get strongly-typed model
            result.Properties = publishedModelFactory.CreateModel(element);

            return result;
        }

        private object? GetConfiguration(IContentType contentType, IPropertyType propertyType, IDataEditor propertyEditor, MapContext context)
        {
            var key = (contentType.Alias, propertyType.Alias);
            if (context.Configurations.TryGetValue(key, out var configuration))
            {
                return configuration;
            }

            // A BlockFarmEditorConfigurationAttribute overrides the data type's configuration.
            var configAttr = context.ConfigMaps.GetValueOrDefault(contentType.Alias)?.FirstOrDefault(x => x.PropertyAlias.InvariantEquals(propertyType.Alias));
            if (configAttr != null && ActivatorUtilities.CreateInstance(serviceProvider, configAttr.GetConfigType) is IBlockFarmEditorConfig config)
            {
                var items = config.GetItems().GetAwaiter().GetResult().ToDictionary(x => x.Alias, x => x.Value);
                try
                {
                    // The value editors expect the editor's configuration object, the same as the data type configuration below.
                    configuration = propertyEditor.GetConfigurationEditor().ToConfigurationObject(
                        items.Where(x => x.Value != null).ToDictionary(x => x.Key, x => x.Value!),
                        configurationEditorJsonSerializer);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Unable to convert the configuration override for property {PropertyName} to the editor's configuration object", propertyType.Alias);
                    configuration = items;
                }
            }

            configuration ??= dataTypeConfigurationCache.GetConfiguration(propertyType.DataTypeKey);

            context.Configurations[key] = configuration;
            return configuration;
        }
    }
}
