using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Serialization;

namespace BlockFarmEditor.Umbraco.Library.Converters;
public class BuilderPropertiesConverter(IServiceProvider serviceProvider) : JsonConverter<BlockDefinition<IPublishedElement>>
{
    private IJsonSerializer _jsonSerializer => serviceProvider.GetRequiredService<IJsonSerializer>();
        private IPublishedContentTypeCache contentTypeCache = serviceProvider.GetRequiredService<IPublishedContentTypeCache>();
        private IPublishedModelFactory modelFactory = serviceProvider.GetRequiredService<IPublishedModelFactory>();
        private IVariationContextAccessor variationContextAccessor = serviceProvider.GetRequiredService<IVariationContextAccessor>();

    public override void Write(Utf8JsonWriter writer, BlockDefinition<IPublishedElement> value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();

        // Core element data
        writer.WriteString("unique", value.Unique.ToString());
        if (value.Properties?.ContentType.Key != null)
        {
            writer.WriteString("contentTypeKey", value.Properties.ContentType.Key);
        }

        if (value.Properties != null)
        {
            writer.WriteStartObject("properties");

            foreach (var property in value.Properties.Properties)
            {
                var propValue = property.GetSourceValue();
                if (propValue != null)
                {
                    writer.WritePropertyName(property.Alias);
                    JsonSerializer.Serialize(writer, propValue, propValue.GetType(), options);
                }
            }

            writer.WriteEndObject(); // properties
        }

        writer.WriteStartArray("blocks");
        if (value.Blocks.Count() > 0)
        {
            foreach (var block in value.Blocks)
            {
                JsonSerializer.Serialize(writer, block, options);
            }
        }
        writer.WriteEndArray(); // blocks

        writer.WriteEndObject(); // main object
    }

    public override BlockDefinition<IPublishedElement>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        IEnumerable<BlockDefinition<IPublishedElement>> blocks = [];
        if (root.TryGetProperty("blocks", out var blocksElement))
        {
            var blocksResult = blocksElement.Deserialize<IEnumerable<BlockDefinition<IPublishedElement>>>(options);
            if (blocksResult != null)
            {
                blocks = blocksResult.Where(x => x != null);
            }
        }

        var uniqueString = root.TryGetProperty("unique", out var uniqueElement)
        ? uniqueElement.GetString()
        : "";

            if (!Guid.TryParse(uniqueString, out var unique))
            {
            return null;
        }

          var result = new BlockDefinition<IPublishedElement>
          {
              Blocks = blocks,
              Unique = unique
          };

        if (!root.TryGetProperty("contentTypeKey", out var contentKeyElement))
            return result;
        if (!contentKeyElement.TryGetGuid(out var contentTypeKey))
            return result;



        var contentType = contentTypeCache.Get(PublishedItemType.Element, contentTypeKey);
        if (contentType == null)
            return result;

        var rawPropertyValues = new Dictionary<string, object?>();

        if (root.TryGetProperty("properties", out var propertiesElement))
        {
            foreach (var propertyType in contentType.PropertyTypes)
            {
                if (propertiesElement.TryGetProperty(propertyType.Alias, out var propElement))
                {
                    // Store raw value - let Umbraco's property system handle conversion
                    object? rawValue = propElement.ValueKind switch
                    {
                        JsonValueKind.Number => propertyType.ModelClrType.Name == "Boolean" ? 1.Equals(propElement.GetInt32()) : propElement.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.String => propElement.GetString(),
                        JsonValueKind.Undefined => null,
                        JsonValueKind.Null => null,
                        JsonValueKind.Array => _jsonSerializer.Deserialize<object>(propElement.GetRawText()),
                        JsonValueKind.Object => _jsonSerializer.Deserialize<object>(propElement.GetRawText()),
                        _ => propElement.GetRawText()
                    };

                    rawPropertyValues[propertyType.Alias] = rawValue;
                }
            }
        }

        // Create the element using Umbraco 16's factory pattern
        var element = new PublishedElement(contentType, unique, rawPropertyValues, false, PropertyCacheLevel.Element, variationContextAccessor.VariationContext, null);

        var properties = modelFactory.CreateModel(element);

        result.Properties = properties;

        // Use model factory to get strongly-typed model
        return result;
    }
}