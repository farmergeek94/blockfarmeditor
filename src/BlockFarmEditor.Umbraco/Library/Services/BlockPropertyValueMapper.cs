using BlockFarmEditor.Umbraco.Core.Attributes;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using BlockFarmEditor.Umbraco.Core.Models.ConfigModels;
using BlockFarmEditor.Umbraco.Library.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;
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
        IJsonSerializer jsonSerializer,
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

        private Dictionary<string, JsonNode?> MapProperties(IContentType contentType, Dictionary<string, JsonNode?> properties, MapContext context)
        {
            var result = new Dictionary<string, JsonNode?>();

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
                    var value = ToClrValue(property.Value, valueEditor.ValueType);

                    object? mappedValue;
                    if (context.ToEditor)
                    {
                        var propertyValue = Property.CreateWithValues(-1, propertyType, new Property.InitialPropertyValue(null, null, false, value));
                        mappedValue = valueEditor.ToEditor(propertyValue);
                    }
                    else
                    {
                        var configuration = GetConfiguration(contentType, propertyType, propertyEditor, context);
                        mappedValue = valueEditor.FromEditor(new ContentPropertyData(value, configuration), value);
                    }

                    result[property.Key] = ToJsonNode(mappedValue);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error mapping property {PropertyName} {Direction} the editor", propertyType.Alias, context.ToEditor ? "to" : "from");
                }
            }
            return result;
        }

        public BlockDefinition<IPublishedElement>? ToBlockDefinition(BlockData block)
        {
            if (!Guid.TryParse(block.Unique, out var unique))
            {
                return null;
            }

            var result = new BlockDefinition<IPublishedElement>
            {
                Blocks = [.. (block.Blocks ?? []).Select(x => x == null ? null : ToBlockDefinition(x)).OfType<BlockDefinition<IPublishedElement>>()],
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

                    var valueType = propertyEditors.TryGet(propertyType.EditorAlias, out var propertyEditor) ? propertyEditor.GetValueEditor().ValueType : null;

                    // Store the source value - let Umbraco's property value converters handle the rest
                    sourceValues[propertyType.Alias] = ToClrValue(property.Value, valueType);
                }
            }

            var element = new PublishedElement(contentType, unique, sourceValues, false, PropertyCacheLevel.Element, variationContextAccessor.VariationContext!, null);

            // Use model factory to get strongly-typed model
            result.Properties = publishedModelFactory.CreateModel(element);

            return result;
        }

        // Same conversion the management api uses when handing values to the value editors: numbers, bools and strings become their clr types, objects and arrays stay json.
        private object? ToClrValue(JsonNode? node, string? valueType)
        {
            if (node == null)
            {
                return null;
            }

            // json has no decimal, so read it directly rather than lose precision going through a double.
            if (valueType.InvariantEquals(ValueTypes.Decimal)
                && node.GetValueKind() == JsonValueKind.Number
                && decimal.TryParse(node.ToJsonString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var decimalValue))
            {
                return decimalValue;
            }

            return jsonSerializer.Deserialize<object>(node.ToJsonString());
        }

        private static JsonNode? ToJsonNode(object? value) => value switch
        {
            null => null,
            JsonNode node => node.Parent == null ? node : node.DeepClone(),
            _ => JsonSerializer.SerializeToNode(value, value.GetType(), BlockData.SerializerOptions)
        };

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
