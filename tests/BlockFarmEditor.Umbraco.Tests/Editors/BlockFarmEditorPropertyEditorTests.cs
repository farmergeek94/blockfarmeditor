using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using BlockFarmEditor.Umbraco.Library.Editors;
using BlockFarmEditor.Umbraco.Tests.Helpers;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Editors;
using Umbraco.Cms.Core.PropertyEditors;

namespace BlockFarmEditor.Umbraco.Tests.Editors;

public class BlockFarmEditorPropertyEditorTests
{
    private const string EditorAlias = "blockfarmeditor_page_propertyeditor";
    private const string EmptyPage = """{"blocks":[{"unique":"5280d691-7534-5910-a0fc-267be0522621","blocks":[]}]}""";

    private readonly BlockFarmServiceProvider _services = new();
    private readonly BlockFarmEditorValueEditor _valueEditor;

    public BlockFarmEditorPropertyEditorTests()
    {
        _valueEditor = new BlockFarmEditorValueEditor(
            UmbracoModels.ShortStringHelper,
            _services.JsonSerializer,
            Mock.Of<IIOHelper>(),
            new DataEditorAttribute(EditorAlias),
            _services.Mapper);
    }

    private static IProperty PropertyWithValue(object? value, string? culture = null, string? segment = null)
    {
        var property = new Mock<IProperty>();
        property.Setup(x => x.GetValue(culture, segment, false)).Returns(value);
        return property.Object;
    }

    /// <summary>A page with one text block, whose text editor marks values as they pass through it.</summary>
    private string PageWithTitle(string title)
    {
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias, fromEditor: value => $"stored:{value}", toEditor: value => $"editor:{value}");
        var contentType = _services.AddContentType("heroBlock", UmbracoModels.PropertyType("title"));
        return $$$"""{"blocks":[{"contentTypeKey":"{{{contentType.Object.Key}}}","unique":"{{{Guid.NewGuid()}}}","properties":{"title":"{{{title}}}"},"blocks":[]}]}""";
    }

    [Fact]
    public void PropertyEditor_IsRegisteredUnderTheBlockFarmEditorAlias()
    {
        var attribute = typeof(BlockFarmEditorPropertyEditor).GetCustomAttribute<DataEditorAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(EditorAlias, attribute.Alias);
    }

    [Fact]
    public void PropertyEditor_CreatesTheBlockFarmValueEditor()
    {
        var factory = new Mock<IDataValueEditorFactory>();
        factory.Setup(x => x.Create<BlockFarmEditorValueEditor>(It.IsAny<object[]>())).Returns(_valueEditor);

        var valueEditor = new BlockFarmEditorPropertyEditor(factory.Object).GetValueEditor();

        Assert.Same(_valueEditor, valueEditor);
        factory.Verify(x => x.Create<BlockFarmEditorValueEditor>(It.Is<object[]>(args => args.OfType<DataEditorAttribute>().Single().Alias == EditorAlias)));
    }

    #region FromEditor

    [Fact]
    public void FromEditor_SerializesTheBlockTree_ToAStringForStorage()
    {
        var result = _valueEditor.FromEditor(new ContentPropertyData(JsonNode.Parse(EmptyPage), null), null);

        Assert.Equal(EmptyPage, Assert.IsType<string>(result));
    }

    [Fact]
    public void FromEditor_RunsBlockPropertiesThroughTheirValueEditors()
    {
        var page = PageWithTitle("Hello");

        var result = _valueEditor.FromEditor(new ContentPropertyData(JsonNode.Parse(page), null), null);

        var stored = JsonNode.Parse(Assert.IsType<string>(result))!;
        Assert.Equal("stored:Hello", stored["blocks"]![0]!["properties"]!["title"]!.GetValue<string>());
    }

    [Fact]
    public void FromEditor_KeepsMembersItDoesNotKnowAbout()
    {
        var result = _valueEditor.FromEditor(new ContentPropertyData(JsonNode.Parse("""{"identifier":"main","blocks":[]}"""), null), null);

        Assert.Equal("main", JsonNode.Parse(Assert.IsType<string>(result))!["identifier"]!.GetValue<string>());
    }

    [Fact]
    public void FromEditor_WithJsonThatIsNotABlockTree_StoresItAsIs()
    {
        var result = _valueEditor.FromEditor(new ContentPropertyData(JsonNode.Parse("[1,2]"), null), null);

        Assert.Equal("[1,2]", result);
    }

    [Fact]
    public void FromEditor_WithAPlainString_StoresItAsIs()
    {
        var result = _valueEditor.FromEditor(new ContentPropertyData("{\"blocks\":[]}", null), null);

        Assert.Equal("{\"blocks\":[]}", result);
    }

    [Fact]
    public void FromEditor_WithNoValue_StoresNull()
    {
        Assert.Null(_valueEditor.FromEditor(new ContentPropertyData(null, null), null));
    }

    #endregion

    #region ToEditor

    [Fact]
    public void ToEditor_WithNoStoredValue_ReturnsAnEmptyString()
    {
        Assert.Equal(string.Empty, _valueEditor.ToEditor(PropertyWithValue(null)));
    }

    [Fact]
    public void ToEditor_ParsesTheStoredJson_IntoAJsonTree()
    {
        var result = _valueEditor.ToEditor(PropertyWithValue("""{"blocks":[{"unique":"abc"}]}"""));

        var node = Assert.IsAssignableFrom<JsonNode>(result);
        Assert.Equal("abc", node["blocks"]![0]!["unique"]!.GetValue<string>());
    }

    [Fact]
    public void ToEditor_RunsBlockPropertiesThroughTheirValueEditors()
    {
        var page = PageWithTitle("Hello");

        var result = Assert.IsAssignableFrom<JsonNode>(_valueEditor.ToEditor(PropertyWithValue(page)));

        Assert.Equal("editor:Hello", result["blocks"]![0]!["properties"]!["title"]!.GetValue<string>());
    }

    [Fact]
    public void ToEditor_ReadsTheRequestedVariant()
    {
        var property = PropertyWithValue("""{"blocks":[]}""", culture: "da-DK", segment: "vip");

        var result = _valueEditor.ToEditor(property, "da-DK", "vip");

        Assert.IsAssignableFrom<JsonNode>(result);
    }

    [Fact]
    public void ToEditor_WithStoredJsonThatIsNotABlockTree_ReturnsItUnchanged()
    {
        var result = Assert.IsAssignableFrom<JsonNode>(_valueEditor.ToEditor(PropertyWithValue("[1,2]")));

        Assert.Equal("[1,2]", result.ToJsonString());
    }

    [Fact]
    public void ToEditor_WithCorruptStoredJson_Throws()
    {
        Assert.ThrowsAny<JsonException>(() => _valueEditor.ToEditor(PropertyWithValue("not json")));
    }

    #endregion

    [Fact]
    public void FromEditorThenToEditor_RoundTripsTheDocument()
    {
        var stored = _valueEditor.FromEditor(new ContentPropertyData(JsonNode.Parse(EmptyPage), null), null);
        var result = _valueEditor.ToEditor(PropertyWithValue(stored));

        Assert.Equal(EmptyPage, Assert.IsAssignableFrom<JsonNode>(result).ToJsonString());
    }
}
