using System.Text.Json;
using System.Text.Json.Nodes;
using BlockFarmEditor.Umbraco.Core.Attributes;
using BlockFarmEditor.Umbraco.Core.Models.ConfigModels;
using BlockFarmEditor.Umbraco.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Editors;
using Umbraco.Cms.Core.PropertyEditors;

namespace BlockFarmEditor.Umbraco.Tests.Converters;

public class BuilderPropertiesWriterTests
{
    public interface IThemeSource { IEnumerable<string> Themes { get; } }

    /// <summary>Resolved through DI by the writer, like a consumer's <see cref="IBlockFarmEditorConfig"/> would be.</summary>
    public class ThemeConfig(IThemeSource source) : IBlockFarmEditorConfig
    {
        public Task<IEnumerable<BlockFarmEditorConfigItem>> GetItems() =>
            Task.FromResult<IEnumerable<BlockFarmEditorConfigItem>>([new BlockFarmEditorConfigItem { Alias = "items", Value = source.Themes }]);
    }

    private const string ContentTypeAlias = "heroBlock";

    private readonly BlockFarmServiceProvider _services;
    private readonly List<ContentPropertyData> _fromEditorCalls = [];

    public BuilderPropertiesWriterTests()
    {
        var themes = new Mock<IThemeSource>();
        themes.SetupGet(x => x.Themes).Returns(["light", "dark"]);
        _services = new BlockFarmServiceProvider(services => services.AddSingleton(themes.Object));
    }

    #region Arrange helpers

    /// <summary>Registers a property editor whose value editor echoes values, optionally transformed.</summary>
    private Mock<IDataValueEditor> AddEditor(string alias, string valueType = ValueTypes.String, Func<object?, object?>? fromEditor = null, Func<object?, object?>? toEditor = null)
    {
        var valueEditor = new Mock<IDataValueEditor>();
        valueEditor.SetupGet(x => x.ValueType).Returns(valueType);
        valueEditor
            .Setup(x => x.FromEditor(It.IsAny<ContentPropertyData>(), It.IsAny<object?>()))
            .Returns((ContentPropertyData data, object? _) =>
            {
                _fromEditorCalls.Add(data);
                return (fromEditor ?? (value => value))(data.Value);
            });
        valueEditor
            .Setup(x => x.ToEditor(It.IsAny<IProperty>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns((IProperty property, string? _, string? _) => (toEditor ?? (value => value))(property.GetValue()));

        var editor = new Mock<IDataEditor>();
        editor.SetupGet(x => x.Alias).Returns(alias);
        editor.Setup(x => x.GetValueEditor()).Returns(valueEditor.Object);
        _services.DataEditors.Add(editor.Object);
        return valueEditor;
    }

    private Guid AddContentType(params IPropertyType[] propertyTypes)
    {
        var contentType = UmbracoModels.ContentType(ContentTypeAlias, noGroupPropertyTypes: propertyTypes);
        _services.ContentTypeService.Setup(x => x.Get(contentType.Object.Key)).Returns(contentType.Object);
        return contentType.Object.Key;
    }

    /// <summary>Serializes the way the value editor does when content is saved from the backoffice.</summary>
    private JsonElement Write(JsonNode node)
    {
        var json = JsonSerializer.Serialize(node, _services.WriterOptions());
        return JsonDocument.Parse(json).RootElement;
    }

    /// <summary>Deserializes the way the value editor does when content is loaded into the backoffice.</summary>
    private JsonNode? Read(string json) => JsonSerializer.Deserialize<JsonNode>(json, _services.WriterOptions());

    private static JsonObject Block(Guid contentTypeKey, JsonObject properties, params JsonNode[] blocks) => new()
    {
        ["contentTypeKey"] = contentTypeKey.ToString(),
        ["unique"] = Guid.NewGuid().ToString(),
        ["blocks"] = new JsonArray(blocks),
        ["properties"] = properties
    };

    #endregion

    #region Write (editor -> storage)

    [Fact]
    public void Write_ObjectWithoutContentTypeKey_IsWrittenUnchanged()
    {
        var result = Write(JsonNode.Parse("""{"name":"x","nested":{"a":[1,2,{"b":null}]},"nothing":null}""")!);

        Assert.Equal("""{"name":"x","nested":{"a":[1,2,{"b":null}]},"nothing":null}""", result.GetRawText());
    }

    [Fact]
    public void Write_Array_IsWrittenUnchanged()
    {
        var result = Write(JsonNode.Parse("""[1,"two",{"three":3}]""")!);

        Assert.Equal("""[1,"two",{"three":3}]""", result.GetRawText());
    }

    [Fact]
    public void Write_BlockOfUnknownContentType_IsWrittenUnchanged()
    {
        var block = Block(Guid.NewGuid(), new JsonObject { ["title"] = "Hello" });

        var result = Write(block);

        Assert.Equal("Hello", result.GetProperty("properties").GetProperty("title").GetString());
        Assert.Empty(_fromEditorCalls);
    }

    [Fact]
    public void Write_KnownBlock_KeepsIdentity_AndRunsPropertiesThroughTheirValueEditors()
    {
        AddEditor(UmbracoModels.TextBoxEditorAlias, fromEditor: value => $"stored:{value}");
        var contentTypeKey = AddContentType(UmbracoModels.PropertyType("title"));
        var block = Block(contentTypeKey, new JsonObject { ["title"] = "Hello" });

        var result = Write(block);

        Assert.Equal(contentTypeKey, result.GetProperty("contentTypeKey").GetGuid());
        Assert.Equal(block["unique"]!.ToString(), result.GetProperty("unique").GetString());
        Assert.Equal(0, result.GetProperty("blocks").GetArrayLength());
        Assert.Equal("stored:Hello", result.GetProperty("properties").GetProperty("title").GetString());
    }

    [Fact]
    public void Write_KnownBlock_DropsMembersThatAreNotPartOfTheBlockContract()
    {
        AddEditor(UmbracoModels.TextBoxEditorAlias);
        var contentTypeKey = AddContentType(UmbracoModels.PropertyType("title"));
        var block = Block(contentTypeKey, new JsonObject { ["title"] = "Hello" });
        block["clientOnlyState"] = "selected";

        var result = Write(block);

        Assert.Equal(["contentTypeKey", "unique", "blocks", "properties"], result.EnumerateObject().Select(x => x.Name));
    }

    [Fact]
    public void Write_NestedBlocks_AreProcessedRecursively()
    {
        AddEditor(UmbracoModels.TextBoxEditorAlias, fromEditor: value => $"stored:{value}");
        var contentTypeKey = AddContentType(UmbracoModels.PropertyType("title"));
        var page = new JsonObject
        {
            ["blocks"] = new JsonArray(new JsonObject
            {
                ["unique"] = Guid.NewGuid().ToString(),
                ["blocks"] = new JsonArray(Block(contentTypeKey, new JsonObject { ["title"] = "outer" }, Block(contentTypeKey, new JsonObject { ["title"] = "inner" })))
            })
        };

        var result = Write(page);

        var outer = result.GetProperty("blocks")[0].GetProperty("blocks")[0];
        Assert.Equal("stored:outer", outer.GetProperty("properties").GetProperty("title").GetString());
        Assert.Equal("stored:inner", outer.GetProperty("blocks")[0].GetProperty("properties").GetProperty("title").GetString());
    }

    [Theory]
    [InlineData(ValueTypes.Integer, "42", typeof(int), "42")]
    [InlineData(ValueTypes.Decimal, "12.5", typeof(decimal), "12.5")]
    [InlineData(ValueTypes.String, "42", typeof(string), "42")]
    [InlineData(ValueTypes.String, "true", typeof(bool), "True")]
    [InlineData(ValueTypes.String, "false", typeof(bool), "False")]
    [InlineData(ValueTypes.String, "\"text\"", typeof(string), "text")]
    public void Write_ConvertsJsonValues_ToTheClrTypeTheValueEditorExpects(string valueType, string json, Type expectedType, string expectedValue)
    {
        AddEditor(UmbracoModels.TextBoxEditorAlias, valueType);
        var contentTypeKey = AddContentType(UmbracoModels.PropertyType("value"));

        Write(Block(contentTypeKey, new JsonObject { ["value"] = JsonNode.Parse(json) }));

        var received = Assert.Single(_fromEditorCalls).Value;
        Assert.IsType(expectedType, received);
        Assert.Equal(expectedValue, Convert.ToString(received, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Write_IntegerEditor_ReceivingAFraction_GetsNoValue()
    {
        AddEditor(UmbracoModels.TextBoxEditorAlias, ValueTypes.Integer);
        var contentTypeKey = AddContentType(UmbracoModels.PropertyType("value"));

        Write(Block(contentTypeKey, new JsonObject { ["value"] = 1.5 }));

        Assert.Null(Assert.Single(_fromEditorCalls).Value);
    }

    [Fact]
    public void Write_ObjectAndArrayValues_AreHandedToTheValueEditorAsDeserializedObjects()
    {
        AddEditor(UmbracoModels.TextBoxEditorAlias);
        var contentTypeKey = AddContentType(UmbracoModels.PropertyType("link"));

        var result = Write(Block(contentTypeKey, new JsonObject { ["link"] = new JsonObject { ["url"] = "/about" } }));

        var received = Assert.Single(_fromEditorCalls).Value;
        Assert.NotNull(received);
        Assert.IsNotType<string>(received);
        Assert.Equal("/about", result.GetProperty("properties").GetProperty("link").GetProperty("url").GetString());
    }

    [Fact]
    public void Write_NullPropertyValue_ReachesTheValueEditorAsNull()
    {
        AddEditor(UmbracoModels.TextBoxEditorAlias);
        var contentTypeKey = AddContentType(UmbracoModels.PropertyType("title"));

        var result = Write(Block(contentTypeKey, new JsonObject { ["title"] = null }));

        Assert.Null(Assert.Single(_fromEditorCalls).Value);
        Assert.Equal(JsonValueKind.Null, result.GetProperty("properties").GetProperty("title").ValueKind);
    }

    [Fact]
    public void Write_MatchesPropertiesCaseInsensitively_AndKeepsTheIncomingKey()
    {
        AddEditor(UmbracoModels.TextBoxEditorAlias);
        var contentTypeKey = AddContentType(UmbracoModels.PropertyType("title"));

        var result = Write(Block(contentTypeKey, new JsonObject { ["Title"] = "Hello" }));

        Assert.Equal("Hello", result.GetProperty("properties").GetProperty("Title").GetString());
    }

    [Fact]
    public void Write_DropsValuesThatDoNotBelongToTheContentType()
    {
        AddEditor(UmbracoModels.TextBoxEditorAlias);
        var contentTypeKey = AddContentType(UmbracoModels.PropertyType("title"));

        var result = Write(Block(contentTypeKey, new JsonObject { ["title"] = "Hello", ["rogue"] = "x" }));

        Assert.Equal(["title"], result.GetProperty("properties").EnumerateObject().Select(x => x.Name));
    }

    [Fact]
    public void Write_PropertiesWithoutAValueInTheJson_AreNotSentToTheValueEditor()
    {
        AddEditor(UmbracoModels.TextBoxEditorAlias);
        var contentTypeKey = AddContentType(UmbracoModels.PropertyType("title"), UmbracoModels.PropertyType("subtitle"));

        var result = Write(Block(contentTypeKey, new JsonObject { ["title"] = "Hello" }));

        Assert.Single(_fromEditorCalls);
        Assert.False(result.GetProperty("properties").TryGetProperty("subtitle", out _));
    }

    [Fact]
    public void Write_PropertyWhoseEditorIsNotInstalled_IsDropped_WithoutAffectingOthers()
    {
        AddEditor(UmbracoModels.TextBoxEditorAlias);
        var contentTypeKey = AddContentType(UmbracoModels.PropertyType("title"), UmbracoModels.PropertyType("map", editorAlias: "Missing.Editor"));

        var result = Write(Block(contentTypeKey, new JsonObject { ["title"] = "Hello", ["map"] = "somewhere" }));

        Assert.Equal(["title"], result.GetProperty("properties").EnumerateObject().Select(x => x.Name));
    }

    [Fact]
    public void Write_PropertyWhoseValueEditorThrows_IsDropped_WithoutAffectingOthers()
    {
        AddEditor(UmbracoModels.TextBoxEditorAlias);
        AddEditor("Broken.Editor", fromEditor: _ => throw new InvalidOperationException("boom"));
        var contentTypeKey = AddContentType(UmbracoModels.PropertyType("title"), UmbracoModels.PropertyType("broken", editorAlias: "Broken.Editor"));

        var result = Write(Block(contentTypeKey, new JsonObject { ["title"] = "Hello", ["broken"] = "x" }));

        Assert.Equal(["title"], result.GetProperty("properties").EnumerateObject().Select(x => x.Name));
    }

    [Fact]
    public void Write_PassesTheDataTypesConfiguration_ToTheValueEditor()
    {
        AddEditor(UmbracoModels.TextBoxEditorAlias);
        var dataTypeKey = Guid.NewGuid();
        var configuration = new Dictionary<string, object> { ["maxChars"] = 50 };
        _services.DataTypeService.Setup(x => x.GetAsync(dataTypeKey)).ReturnsAsync(UmbracoModels.DataType(dataTypeKey, configuration: configuration).Object);
        var contentTypeKey = AddContentType(UmbracoModels.PropertyType("title", dataTypeKey));

        Write(Block(contentTypeKey, new JsonObject { ["title"] = "Hello" }));

        Assert.Same(configuration, Assert.Single(_fromEditorCalls).DataTypeConfiguration);
    }

    [Fact]
    public void Write_WhenABlockFarmEditorConfigIsRegisteredForTheProperty_ItReplacesTheDataTypesConfiguration()
    {
        AddEditor(UmbracoModels.TextBoxEditorAlias);
        var contentTypeKey = AddContentType(UmbracoModels.PropertyType("theme"), UmbracoModels.PropertyType("title"));
        _services.BlockDefinitionService.Setup(x => x.GetConfigMaps()).Returns(new Dictionary<string, IEnumerable<BlockFarmEditorConfigurationAttribute>>
        {
            [ContentTypeAlias] = [new BlockFarmEditorConfigurationAttribute(ContentTypeAlias, "Theme", typeof(ThemeConfig))]
        });

        Write(Block(contentTypeKey, new JsonObject { ["theme"] = "dark", ["title"] = "Hello" }));

        var themeCall = _fromEditorCalls.Single(x => Equals(x.Value, "dark"));
        var configuration = Assert.IsType<Dictionary<string, object?>>(themeCall.DataTypeConfiguration);
        Assert.Equal(["light", "dark"], Assert.IsAssignableFrom<IEnumerable<string>>(configuration["items"]));

        var titleCall = _fromEditorCalls.Single(x => Equals(x.Value, "Hello"));
        Assert.IsNotType<Dictionary<string, object?>>(titleCall.DataTypeConfiguration);
    }

    #endregion

    #region Read (storage -> editor)

    [Fact]
    public void Read_ObjectWithoutBlocks_IsReturnedUnchanged()
    {
        var result = Read("""{"name":"x","list":[1,2]}""");

        Assert.Equal("""{"name":"x","list":[1,2]}""", result!.ToJsonString());
    }

    [Fact]
    public void Read_NonObjectJson_IsReturnedUnchanged()
    {
        Assert.Equal("[1,2]", Read("[1,2]")!.ToJsonString());
        Assert.Equal("\"text\"", Read("\"text\"")!.ToJsonString());
        Assert.Null(Read("null"));
    }

    [Fact]
    public void Read_KnownBlock_RunsPropertiesThroughToEditor()
    {
        AddEditor(UmbracoModels.TextBoxEditorAlias, toEditor: value => $"editor:{value}");
        var contentTypeKey = AddContentType(UmbracoModels.PropertyType("title"));

        var result = Read(Block(contentTypeKey, new JsonObject { ["title"] = "Hello" }).ToJsonString());

        Assert.Equal("editor:Hello", result!["properties"]!["title"]!.GetValue<string>());
    }

    [Fact]
    public void Read_NestedBlocks_AreProcessedRecursively()
    {
        AddEditor(UmbracoModels.TextBoxEditorAlias, toEditor: value => $"editor:{value}");
        var contentTypeKey = AddContentType(UmbracoModels.PropertyType("title"));
        var page = new JsonObject
        {
            ["blocks"] = new JsonArray(new JsonObject
            {
                ["unique"] = Guid.NewGuid().ToString(),
                ["blocks"] = new JsonArray(Block(contentTypeKey, new JsonObject { ["title"] = "outer" }, Block(contentTypeKey, new JsonObject { ["title"] = "inner" })))
            })
        };

        var result = Read(page.ToJsonString());

        var outer = result!["blocks"]![0]!["blocks"]![0]!;
        Assert.Equal("editor:outer", outer["properties"]!["title"]!.GetValue<string>());
        Assert.Equal("editor:inner", outer["blocks"]![0]!["properties"]!["title"]!.GetValue<string>());
    }

    [Fact]
    public void Read_BlockOfUnknownContentType_KeepsItsStoredProperties()
    {
        var block = Block(Guid.NewGuid(), new JsonObject { ["title"] = "Hello" });

        var result = Read(block.ToJsonString());

        Assert.Equal("Hello", result!["properties"]!["title"]!.GetValue<string>());
    }

    [Fact]
    public void Read_DoesNotConsultBlockFarmEditorConfigs()
    {
        AddEditor(UmbracoModels.TextBoxEditorAlias);
        var contentTypeKey = AddContentType(UmbracoModels.PropertyType("title"));

        Read(Block(contentTypeKey, new JsonObject { ["title"] = "Hello" }).ToJsonString());

        _services.DataTypeService.VerifyNoOtherCalls();
    }

    #endregion
}
