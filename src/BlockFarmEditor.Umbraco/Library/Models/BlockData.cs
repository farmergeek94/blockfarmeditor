using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Umbraco.Cms.Infrastructure.Serialization;

namespace BlockFarmEditor.Umbraco.Library.Models
{
    /// <summary>
    /// The JSON shape of a block (or the page root) as it is stored and as it is sent to / received from the editor.
    /// This only describes the shape - converting the property values is handled by <see cref="Services.IBlockPropertyValueMapper"/>.
    /// </summary>
    public sealed class BlockData
    {
        public static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // the converter umbraco reads its own property values with: numbers, bools and strings become their clr types, objects and arrays stay json.
            Converters = { new JsonObjectConverter() }
        };

        // kept as strings so that an empty or malformed value from the editor never fails the whole tree.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ContentTypeKey { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Unique { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object?>? Properties { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<BlockData?>? Blocks { get; set; }

        // anything we don't know about (ex. the page root's "identifier") is round tripped untouched.
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalData { get; set; }

        public static BlockData? Parse(string json) => JsonSerializer.Deserialize<BlockData>(json, SerializerOptions);

        public static BlockData? Parse(JsonNode json) => json.Deserialize<BlockData>(SerializerOptions);

        public string ToJson() => JsonSerializer.Serialize(this, SerializerOptions);

        public JsonNode? ToJsonNode() => JsonSerializer.SerializeToNode(this, SerializerOptions);
    }
}
