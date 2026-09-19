using System.Text.Json;
using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using BlockFarmEditor.Umbraco.Tests.Helpers;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace BlockFarmEditor.Umbraco.Tests.Converters;

public class BuilderPropertiesConverterTests
{
    private readonly BlockFarmServiceProvider _services = new();

    private BlockDefinition<IPublishedElement>? Read(string json) =>
        JsonSerializer.Deserialize<BlockDefinition<IPublishedElement>>(json, _services.ReaderOptions());

    private static object? SourceValue(BlockDefinition<IPublishedElement> block, string alias) =>
        block.Properties!.Properties.Single(x => x.Alias == alias).GetSourceValue();

    #region Read

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"unique\":\"not-a-guid\"}")]
    [InlineData("{\"unique\":\"\"}")]
    [InlineData("{\"blocks\":[]}")]
    public void Read_WithoutAValidUnique_ReturnsNull(string json)
    {
        Assert.Null(Read(json));
    }

    [Fact]
    public void Read_WithoutContentTypeKey_ReturnsAPropertylessBlock()
    {
        var unique = Guid.NewGuid();

        var block = Read($$$"""{"unique":"{{{unique}}}"}""");

        Assert.NotNull(block);
        Assert.Equal(unique, block.Unique);
        Assert.Null(block.Properties);
        Assert.Null(block.ContentTypeKey);
        Assert.Empty(block.Blocks);
    }

    [Fact]
    public void Read_WithUnparseableContentTypeKey_ReturnsAPropertylessBlock()
    {
        var block = Read($$$"""{"unique":"{{{Guid.NewGuid()}}}","contentTypeKey":"not-a-guid"}""");

        Assert.NotNull(block);
        Assert.Null(block.Properties);
    }

    [Fact]
    public void Read_WithUnknownContentType_ReturnsAPropertylessBlock()
    {
        var block = Read($$$"""{"unique":"{{{Guid.NewGuid()}}}","contentTypeKey":"{{{Guid.NewGuid()}}}","properties":{"title":"x"}}""");

        Assert.NotNull(block);
        Assert.Null(block.Properties);
    }

    [Fact]
    public void Read_WithKnownContentType_BuildsAPublishedElement_KeyedByUnique()
    {
        var contentTypeKey = _services.AddPublishedElementType(("title", typeof(string)));
        var unique = Guid.NewGuid();

        var block = Read($$$"""{"unique":"{{{unique}}}","contentTypeKey":"{{{contentTypeKey}}}","properties":{"title":"Hello"}}""");

        Assert.NotNull(block);
        Assert.NotNull(block.Properties);
        Assert.Equal(unique, block.Unique);
        Assert.Equal(unique, block.Properties.Key);
        Assert.Equal(contentTypeKey, block.ContentTypeKey);
        Assert.Equal("Hello", SourceValue(block, "title"));
    }

    [Fact]
    public void Read_PassesTheElementThroughTheModelFactory()
    {
        var contentTypeKey = _services.AddPublishedElementType(("title", typeof(string)));
        var typedModel = Definitions.Element(contentTypeKey);
        _services.ModelFactory.Setup(x => x.CreateModel(It.IsAny<IPublishedElement>())).Returns(typedModel);

        var block = Read($$$"""{"unique":"{{{Guid.NewGuid()}}}","contentTypeKey":"{{{contentTypeKey}}}"}""");

        Assert.Same(typedModel, block!.Properties);
    }

    [Fact]
    public void Read_MapsJsonValueKinds_ToRawSourceValues()
    {
        var contentTypeKey = _services.AddPublishedElementType(
            ("text", typeof(string)),
            ("number", typeof(decimal)),
            ("yes", typeof(bool)),
            ("no", typeof(bool)),
            ("nothing", typeof(string)));

        var block = Read($$$"""
            {"unique":"{{{Guid.NewGuid()}}}","contentTypeKey":"{{{contentTypeKey}}}",
             "properties":{"text":"Hello","number":12.5,"yes":true,"no":false,"nothing":null}}
            """);

        Assert.NotNull(block);
        Assert.Equal("Hello", SourceValue(block, "text"));
        Assert.Equal(12.5d, SourceValue(block, "number"));
        Assert.Equal(true, SourceValue(block, "yes"));
        Assert.Equal(false, SourceValue(block, "no"));
        Assert.Null(SourceValue(block, "nothing"));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(2, false)]
    public void Read_NumericValueForABooleanProperty_IsTreatedAsAToggle(int json, bool expected)
    {
        var contentTypeKey = _services.AddPublishedElementType(("visible", typeof(bool)));

        var block = Read($$$"""{"unique":"{{{Guid.NewGuid()}}}","contentTypeKey":"{{{contentTypeKey}}}","properties":{"visible":{{{json}}}}}""");

        Assert.Equal(expected, SourceValue(block!, "visible"));
    }

    [Fact]
    public void Read_ObjectAndArrayValues_AreHandedToTheUmbracoSerializer()
    {
        var contentTypeKey = _services.AddPublishedElementType(("link", typeof(object)), ("tags", typeof(object)));

        var block = Read($$$"""
            {"unique":"{{{Guid.NewGuid()}}}","contentTypeKey":"{{{contentTypeKey}}}",
             "properties":{"link":{"url":"/about","target":"_blank"},"tags":["a","b"]}}
            """);

        // whatever object model the Umbraco serializer produces, it must still represent the same JSON
        Assert.Equal("""{"url":"/about","target":"_blank"}""", _services.JsonSerializer.Serialize(SourceValue(block!, "link")));
        Assert.Equal("""["a","b"]""", _services.JsonSerializer.Serialize(SourceValue(block!, "tags")));
    }

    [Fact]
    public void Read_IgnoresJsonPropertiesThatAreNotOnTheContentType()
    {
        var contentTypeKey = _services.AddPublishedElementType(("title", typeof(string)));

        var block = Read($$$"""{"unique":"{{{Guid.NewGuid()}}}","contentTypeKey":"{{{contentTypeKey}}}","properties":{"title":"x","rogue":"y"}}""");

        Assert.Equal(["title"], block!.Properties!.Properties.Select(x => x.Alias));
    }

    [Fact]
    public void Read_PropertiesMissingFromJson_HaveNoSourceValue()
    {
        var contentTypeKey = _services.AddPublishedElementType(("title", typeof(string)), ("subtitle", typeof(string)));

        var block = Read($$$"""{"unique":"{{{Guid.NewGuid()}}}","contentTypeKey":"{{{contentTypeKey}}}","properties":{"title":"x"}}""");

        Assert.Null(SourceValue(block!, "subtitle"));
    }

    [Fact]
    public void Read_NestedBlocks_AreReadRecursively_AndInvalidOnesDropped()
    {
        var contentTypeKey = _services.AddPublishedElementType(("title", typeof(string)));
        var child = Guid.NewGuid();
        var grandChild = Guid.NewGuid();

        var block = Read($$$"""
            {"unique":"{{{Guid.NewGuid()}}}","blocks":[
                {"unique":"{{{child}}}","blocks":[
                    {"unique":"{{{grandChild}}}","contentTypeKey":"{{{contentTypeKey}}}","properties":{"title":"deep"}}]},
                {"unique":"invalid"}]}
            """);

        var childBlock = Assert.Single(block!.Blocks);
        Assert.Equal(child, childBlock.Unique);
        var grandChildBlock = Assert.Single(childBlock.Blocks);
        Assert.Equal(grandChild, grandChildBlock.Unique);
        Assert.Equal("deep", SourceValue(grandChildBlock, "title"));
    }

    [Fact]
    public void Read_APageDefinition_UsesTheConverterForItsBlocks()
    {
        var area = Guid.NewGuid();

        var page = JsonSerializer.Deserialize<PageDefinition>($$$"""{"blocks":[{"unique":"{{{area}}}","blocks":[]}]}""", _services.ReaderOptions());

        Assert.NotNull(page);
        Assert.Equal(area, Assert.Single(page.Blocks).Unique);
    }

    #endregion

    #region Write

    private string Write(BlockDefinition<IPublishedElement> block) => JsonSerializer.Serialize(block, _services.ReaderOptions());

    private static IPublishedElement ElementWithSourceValues(Guid contentTypeKey, Guid key, params (string Alias, object? Value)[] values)
    {
        var element = Mock.Get(Definitions.Element(contentTypeKey, key));
        element.SetupGet(x => x.Properties).Returns(values.Select(value =>
        {
            var property = new Mock<IPublishedProperty>();
            property.SetupGet(x => x.Alias).Returns(value.Alias);
            property.Setup(x => x.GetSourceValue()).Returns(value.Value);
            return property.Object;
        }).ToList());
        return element.Object;
    }

    [Fact]
    public void Write_PropertylessBlock_WritesUniqueAndEmptyBlocksOnly()
    {
        var unique = Guid.NewGuid();

        var json = Write(new BlockDefinition<IPublishedElement> { Unique = unique });

        Assert.Equal($$$"""{"unique":"{{{unique}}}","blocks":[]}""", json);
    }

    [Fact]
    public void Write_BlockWithProperties_WritesContentTypeKey_AndSourceValues_SkippingNulls()
    {
        var contentTypeKey = Guid.NewGuid();
        var unique = Guid.NewGuid();
        var block = new BlockDefinition<IPublishedElement>
        {
            Properties = ElementWithSourceValues(contentTypeKey, unique, ("title", "Hello"), ("count", 3), ("visible", true), ("empty", null))
        };

        using var json = JsonDocument.Parse(Write(block));
        var root = json.RootElement;

        Assert.Equal(unique, root.GetProperty("unique").GetGuid());
        Assert.Equal(contentTypeKey, root.GetProperty("contentTypeKey").GetGuid());
        var properties = root.GetProperty("properties");
        Assert.Equal("Hello", properties.GetProperty("title").GetString());
        Assert.Equal(3, properties.GetProperty("count").GetInt32());
        Assert.True(properties.GetProperty("visible").GetBoolean());
        Assert.False(properties.TryGetProperty("empty", out _));
        Assert.Equal(0, root.GetProperty("blocks").GetArrayLength());
    }

    [Fact]
    public void Write_NestedBlocks_AreWrittenRecursively()
    {
        var child = Guid.NewGuid();
        var grandChild = Guid.NewGuid();
        var block = new BlockDefinition<IPublishedElement>
        {
            Unique = Guid.NewGuid(),
            Blocks = [new BlockDefinition<IPublishedElement> { Unique = child, Blocks = [new BlockDefinition<IPublishedElement> { Unique = grandChild }] }]
        };

        using var json = JsonDocument.Parse(Write(block));

        var childJson = Assert.Single(json.RootElement.GetProperty("blocks").EnumerateArray());
        Assert.Equal(child, childJson.GetProperty("unique").GetGuid());
        Assert.Equal(grandChild, Assert.Single(childJson.GetProperty("blocks").EnumerateArray()).GetProperty("unique").GetGuid());
    }

    [Fact]
    public void WriteThenRead_RoundTripsABlockTree()
    {
        var contentTypeKey = _services.AddPublishedElementType(("title", typeof(string)));
        var unique = Guid.NewGuid();
        var block = new BlockDefinition<IPublishedElement>
        {
            Properties = ElementWithSourceValues(contentTypeKey, unique, ("title", "Hello")),
            Blocks = [new BlockDefinition<IPublishedElement> { Unique = Guid.NewGuid() }]
        };

        var result = Read(Write(block));

        Assert.NotNull(result);
        Assert.Equal(unique, result.Unique);
        Assert.Equal(contentTypeKey, result.ContentTypeKey);
        Assert.Equal("Hello", SourceValue(result, "title"));
        Assert.Equal(block.Blocks.Single().Unique, Assert.Single(result.Blocks).Unique);
    }

    #endregion
}
