using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using BlockFarmEditor.Umbraco.Library.Models;
using BlockFarmEditor.Umbraco.Library.Services;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;

namespace BlockFarmEditor.Umbraco.Library.Converters
{
    // The mapper is lazy as it depends on the published content type cache, which in turn depends on the property value converters.
    [DefaultPropertyValueConverter]
    public class BlockFarmEditorUmbracoConverter(Lazy<IBlockPropertyValueMapper> blockPropertyValueMapper, ILogger<BlockFarmEditorUmbracoConverter> logger) : IPropertyValueConverter
    {
        public object? ConvertIntermediateToObject(IPublishedElement owner, IPublishedPropertyType propertyType, PropertyCacheLevel referenceCacheLevel, object? inter, bool preview)
        {
            return inter;
        }

        public object? ConvertSourceToIntermediate(IPublishedElement owner, IPublishedPropertyType propertyType, object? source, bool preview)
        {
            if (source is not string json || string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                var root = BlockData.Parse(json);
                return root == null ? null : blockPropertyValueMapper.Value.ToPageDefinition(root);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error converting the stored value of property {PropertyName} to a page definition", propertyType.Alias);
                return null;
            }
        }

        public PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType propertyType) => PropertyCacheLevel.Element;

        public Type GetPropertyValueType(IPublishedPropertyType propertyType) =>  typeof(PageDefinition);

        public bool IsConverter(IPublishedPropertyType propertyType) => propertyType.EditorAlias.Equals(BlockFarmEditorContext.BlockFarmEditorEditorAlias, StringComparison.InvariantCultureIgnoreCase);

        // The source value is the stored json string, it only becomes a PageDefinition from the intermediate level on.
        public bool? IsValue(object? value, PropertyValueLevel level) => level switch
        {
            PropertyValueLevel.Source => value is string json && !string.IsNullOrWhiteSpace(json),
            _ => value is PageDefinition
        };
    }
}
