using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using BlockFarmEditor.Umbraco.Library.Services;
using System.Text.Json;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;

namespace BlockFarmEditor.Umbraco.Library.Converters
{
    [DefaultPropertyValueConverter]
    public class BlockFarmEditorUmbracoConverter(IBlockDefinitionService blockDefinitionService) : IPropertyValueConverter
    {
        public object? ConvertIntermediateToObject(IPublishedElement owner, IPublishedPropertyType propertyType, PropertyCacheLevel referenceCacheLevel, object? inter, bool preview)
        {
            return inter;
        }

        public object? ConvertSourceToIntermediate(IPublishedElement owner, IPublishedPropertyType propertyType, object? source, bool preview)
        {
            if (source is JsonDocument jsonDocument)
            {
                return DeserializePageDefinition(jsonDocument);
            } else if(source is string json)
            {
                return DeserializePageDefinition(json);
            }
            return null;
        }

        public PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType propertyType) => PropertyCacheLevel.Element;

        public Type GetPropertyValueType(IPublishedPropertyType propertyType) =>  typeof(PageDefinition);

        public bool IsConverter(IPublishedPropertyType propertyType) => propertyType.EditorAlias.Equals(BlockFarmEditorContext.BlockFarmEditorEditorAlias, StringComparison.InvariantCultureIgnoreCase);

        public bool? IsValue(object? value, PropertyValueLevel level) => value != null && value is PageDefinition;

        private PageDefinition? DeserializePageDefinition(JsonDocument json)
        {
            try
            {
                // You can use a JSON library like Newtonsoft.Json or System.Text.Json here
                return json.Deserialize<PageDefinition>(blockDefinitionService.JsonSerializerReaderOptions);
            }
            catch
            {
                // Handle deserialization errors
                return null;
            }
        }
        private PageDefinition? DeserializePageDefinition(string json)
        {
            try
            {
                // You can use a JSON library like Newtonsoft.Json or System.Text.Json here
                return JsonSerializer.Deserialize<PageDefinition>(json, blockDefinitionService.JsonSerializerReaderOptions);
            }
            catch
            {
                // Handle deserialization errors
                return null;
            }
        }
    }
}
