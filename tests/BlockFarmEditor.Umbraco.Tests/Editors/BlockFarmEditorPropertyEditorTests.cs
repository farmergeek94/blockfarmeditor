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

    private readonly BlockFarmServiceProvider _services = new();
    private readonly BlockFarmEditorValueEditor _valueEditor;

    public BlockFarmEditorPropertyEditorTests()
    {
        _services.BlockDefinitionService.SetupGet(x => x.JsonSerializerWriterOptions).Returns(() => _services.WriterOptions());
        _valueEditor = new BlockFarmEditorValueEditor(
            UmbracoModels.ShortStringHelper,
            _services.JsonSerializer,
            Mock.Of<IIOHelper>(),
            new DataEditorAttribute(EditorAlias),
            _services.BlockDefinitionService.Object);
    }

    private static IProperty PropertyWithValue(object? value, string? culture = null, string? segment = null)
    {
        var property = new Mock<IProperty>();
        property.Setup(x => x.GetValue(culture, segment, false)).Returns(value);
        return property.Object;
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

    [Fact]
    public void FromEditor_SerializesTheJsonTree_ToAStringForStorage()
    {
        JsonNode node = JsonNode.Parse("""{"blocks":[{"unique":"5280d691-7534-5910-a0fc-267be0522621","blocks":[]}]}""")!;

        var result = _valueEditor.FromEditor(new ContentPropertyData(node, null), null);

        Assert.Equal("""{"blocks":[{"unique":"5280d691-7534-5910-a0fc-267be0522621","blocks":[]}]}""", Assert.IsType<string>(result));
    }

    [Fact]
    public void FromEditor_UsesTheBlockFarmWriterOptions_EachTime()
    {
        JsonNode node = JsonNode.Parse("{}")!;

        _valueEditor.FromEditor(new ContentPropertyData(node, null), null);

        _services.BlockDefinitionService.VerifyGet(x => x.JsonSerializerWriterOptions, Times.Once);
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
    public void ToEditor_ReadsTheRequestedVariant()
    {
        var property = PropertyWithValue("""{"blocks":[]}""", culture: "da-DK", segment: "vip");

        var result = _valueEditor.ToEditor(property, "da-DK", "vip");

        Assert.IsAssignableFrom<JsonNode>(result);
    }

    [Fact]
    public void ToEditor_WithCorruptStoredJson_Throws()
    {
        Assert.ThrowsAny<JsonException>(() => _valueEditor.ToEditor(PropertyWithValue("not json")));
    }

    [Fact]
    public void FromEditorThenToEditor_RoundTripsTheDocument()
    {
        JsonNode node = JsonNode.Parse("""{"blocks":[{"unique":"5280d691-7534-5910-a0fc-267be0522621","blocks":[]}]}""")!;

        var stored = _valueEditor.FromEditor(new ContentPropertyData(node, null), null);
        var result = _valueEditor.ToEditor(PropertyWithValue(stored));

        Assert.Equal(node.ToJsonString(), Assert.IsAssignableFrom<JsonNode>(result).ToJsonString());
    }
}
