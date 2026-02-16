using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Core.Models.ConfigModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Editors;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;

namespace BlockFarmEditor.Umbraco.Library.Converters
{
    internal class BuilderPropertiesWriter(
        IServiceProvider serviceProvider) : JsonConverter<JsonNode>
    {
        private readonly IContentTypeService _contentTypeService = serviceProvider.GetRequiredService<IContentTypeService>();
        private readonly ILogger<BuilderPropertiesWriter> _logger = serviceProvider.GetRequiredService<ILogger<BuilderPropertiesWriter>>();
        private readonly PropertyEditorCollection _propertyEditors = serviceProvider.GetRequiredService<PropertyEditorCollection>();

        private readonly IJsonSerializer _jsonSerializer = serviceProvider.GetRequiredService<IJsonSerializer>();

        private readonly IDataTypeService _dataTypeService = serviceProvider.GetRequiredService<IDataTypeService>();

        private readonly IBlockDefinitionService _blockDefinitionService = serviceProvider.GetRequiredService<IBlockDefinitionService>();


        private object? ParseJsonNodeValue(JsonNode? jsonNode, IDataValueEditor dataValueEditor)
        {

            if (jsonNode == null)
            {
                return null;
            }
            var node = jsonNode;

            object? result = null;
            var valueKind = node.GetValueKind();
            if (valueKind == JsonValueKind.Array || valueKind == JsonValueKind.Object)
            {
                result = _jsonSerializer.Deserialize<object>(node.ToString());
            }

            else if (valueKind == JsonValueKind.Number)
            {
                // Handle number values, possibly with custom deserialization
                if (dataValueEditor.ValueType == ValueTypes.Integer)
                {
                    if (int.TryParse(node.ToString(), out var intValue))
                    {
                        result = intValue;
                    }
                }
                else if (dataValueEditor.ValueType == ValueTypes.Decimal)
                {
                    if (decimal.TryParse(node.ToString(), out var decimalValue))
                    {
                        result = decimalValue;
                    }
                }
                else
                {
                    // Fallback for other numeric types
                    result = node.ToString();
                }
            }
            else if (valueKind == JsonValueKind.True || valueKind == JsonValueKind.False)
            {
                if (bool.TryParse(node.ToString(), out var boolValue))
                {
                    result = boolValue;
                }
                else
                {                     // Fallback for boolean parsing failure
                    result = node.ToString();
                }
            }
            else
            {
                // Fallback for other types
                result = node.ToString();
            }
            return result;
        }

        private ExpandoObject GetPropertiesDictionary(IContentType contentType, JsonObject props, bool toEditor)
        {
            var obj = new ExpandoObject();

            var dict = (IDictionary<string, object?>)obj;

            var propertyTypes = contentType.CompositionPropertyTypes;

            var configurations = _blockDefinitionService.GetConfigMaps().TryGetValue(contentType.Alias, out var configs) ? configs : [];


            foreach (var propertyType in propertyTypes)
            {
                try
                {

                    if (!_propertyEditors.TryGet(propertyType.PropertyEditorAlias, out var propertyEditor))
                    {
                        _logger?.LogWarning("Property editor '{EditorAlias}' not found for property {PropertyName}", propertyType.PropertyEditorAlias, propertyType.Alias);
                        continue;
                    }

                    var valueEditor = propertyEditor.GetValueEditor();

                    var valueKeyPair = props.FirstOrDefault(x => x.Key.InvariantEquals(propertyType.Alias));

                    if (valueKeyPair.Key == null)
                    {
                        continue;
                    }

                    var parsedJsonNode = ParseJsonNodeValue(valueKeyPair.Value, valueEditor);

                    if (!toEditor)
                    {
                        var configAttr = configurations.FirstOrDefault(x => x.PropertyAlias.InvariantEquals(propertyType.Alias));
                        object? editorConfig = null;
                        if(configAttr != null)
                        {
                            var typeInstance = ActivatorUtilities.CreateInstance(serviceProvider, configAttr.GetConfigType);
                            if (typeInstance is IBlockFarmEditorConfig config)
                            {
                                editorConfig = config.GetItems().GetAwaiter().GetResult().ToDictionary(x => x.Alias, x => x.Value);
                            }
                        }

                        editorConfig ??= _dataTypeService.GetAsync(propertyType.DataTypeKey).GetAwaiter().GetResult()?.ConfigurationData;

                        var propertyData = new ContentPropertyData(parsedJsonNode, editorConfig);
                        var processedValue = valueEditor.FromEditor(propertyData, parsedJsonNode);
                        dict.Add(valueKeyPair.Key, processedValue);
                    }
                    else
                    {
                        var propertyValue = Property.CreateWithValues(-1, propertyType, new Property.InitialPropertyValue(null, null, false, parsedJsonNode));
                        var editorValue = valueEditor.ToEditor(propertyValue);
                        dict.Add(valueKeyPair.Key, editorValue);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error processing property {PropertyName} in GetPropertiesDictionary", propertyType.Alias);
                }
            }
            return (ExpandoObject)dict;
        }



        public override void Write(Utf8JsonWriter writer, JsonNode value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            try
            {
                if (value is JsonObject obj && obj.ContainsKey("contentTypeKey") && Guid.TryParse(obj["contentTypeKey"]?.ToString(), out var guid))
                {
                    var contentType = _contentTypeService.Get(guid);

                    if (contentType != null)
                    {

                        writer.WriteStartObject();
                        writer.WritePropertyName("contentTypeKey");
                        writer.WriteStringValue(guid);
                        writer.WritePropertyName("unique");
                        writer.WriteStringValue(obj["unique"]?.ToString());
                        if (obj["blocks"] is JsonArray blocks)
                        {
                            writer.WriteStartArray("blocks");
                            foreach (var block in blocks)
                                if (block != null) JsonSerializer.Serialize(writer, block, options);
                            writer.WriteEndArray();
                        }
                        if(obj.ContainsKey("properties") && obj["properties"] is JsonObject jObj)
                        {
                            var propertyDictionary = GetPropertiesDictionary(contentType, jObj, toEditor: false);

                            writer.WritePropertyName("properties");
                            JsonSerializer.Serialize(writer, propertyDictionary, propertyDictionary.GetType(), options);
                        }
                        writer.WriteEndObject();
                        return;
                    }
                }

                if (value is JsonObject o)
                {
                    writer.WriteStartObject();
                    foreach (var kv in o)
                    {
                        writer.WritePropertyName(kv.Key);
                        if (kv.Value != null) JsonSerializer.Serialize(writer, kv.Value, options);
                        else writer.WriteNullValue();
                    }
                    writer.WriteEndObject();
                }
                else if (value is JsonArray arr)
                {
                    writer.WriteStartArray();
                    foreach (var v in arr)
                        if (v != null) JsonSerializer.Serialize(writer, v, options);
                    writer.WriteEndArray();
                }
                else
                {
                    JsonSerializer.Serialize(writer, value, value.GetType(), options);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing IPublishedElement to editor format");
                throw;
            }
        }

        private void ReadJsonProperties(JsonObject obj, JsonSerializerOptions options)
        {
            if (obj.ContainsKey("blocks") && obj["blocks"] is JsonArray jsonArr && jsonArr.Count > 0)
            {
                foreach (var jsonItm in jsonArr)
                {
                    if (jsonItm is JsonObject jsonObj)
                        ReadJsonProperties(jsonObj, options);
                }
            }

            if (!obj.ContainsKey("contentTypeKey"))
                return;

            if (!Guid.TryParse(obj["contentTypeKey"]?.ToString(), out var contentTypeKey))
                return;

            var contentType = _contentTypeService.Get(contentTypeKey);

            if (contentType == null)
                return;

            if (obj.ContainsKey("properties") && obj["properties"] is JsonObject jObj)
            {
                var propertyDictionary = GetPropertiesDictionary(contentType, jObj, toEditor: true);

                obj["properties"] = JsonSerializer.SerializeToNode(propertyDictionary, propertyDictionary.GetType(), options);
            }
        }

        public override JsonNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var doc = JsonNode.Parse(ref reader);

            if (doc != null && doc is JsonObject root)
            {
                ReadJsonProperties(root, options);
            }
            return doc;
        }
    }
}
