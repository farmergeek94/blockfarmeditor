using System.Text.Json.Nodes;
using BlockFarmEditor.Umbraco.Library.Models;

namespace BlockFarmEditor.Umbraco.Tests.Models;

public class BlockDataTests
{
    [Fact]
    public void Parse_ReadsTheBlockShape_Recursively()
    {
        var block = BlockData.Parse("""
            {"contentTypeKey":"ctk","unique":"u1","properties":{"title":"Hello","count":3,"link":{"url":"/about"},"nothing":null},
             "blocks":[{"unique":"u2","blocks":[{"unique":"u3"}]}]}
            """);

        Assert.NotNull(block);
        Assert.Equal("ctk", block.ContentTypeKey);
        Assert.Equal("u1", block.Unique);
        Assert.Equal(["title", "count", "link", "nothing"], block.Properties!.Keys);
        Assert.Equal("Hello", block.Properties["title"]!.GetValue<string>());
        Assert.Equal("/about", block.Properties["link"]!["url"]!.GetValue<string>());
        Assert.Null(block.Properties["nothing"]);
        Assert.Equal("u3", block.Blocks!.Single()!.Blocks!.Single()!.Unique);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("not-a-guid", "also-not-a-guid")]
    public void Parse_AcceptsEmptyOrMalformedKeys_SoOneBadBlockCannotFailTheTree(string contentTypeKey, string unique)
    {
        var block = BlockData.Parse($$"""{"contentTypeKey":"{{contentTypeKey}}","unique":"{{unique}}"}""");

        Assert.Equal(contentTypeKey, block!.ContentTypeKey);
        Assert.Equal(unique, block.Unique);
    }

    [Fact]
    public void Parse_KeepsNullEntriesInBlocks()
    {
        var block = BlockData.Parse("""{"blocks":[null,{"unique":"u"}]}""");

        Assert.Equal(2, block!.Blocks!.Count);
        Assert.Null(block.Blocks[0]);
    }

    [Fact]
    public void Parse_OfJsonNull_IsNull()
    {
        Assert.Null(BlockData.Parse("null"));
    }

    [Fact]
    public void Parse_FromAJsonNode_MatchesParsingItsText()
    {
        const string json = """{"unique":"u1","identifier":"main","blocks":[{"unique":"u2"}]}""";

        Assert.Equal(BlockData.Parse(json)!.ToJson(), BlockData.Parse(JsonNode.Parse(json)!)!.ToJson());
    }

    [Fact]
    public void ToJson_UsesCamelCase_AndOmitsMembersThatWereNeverSet()
    {
        Assert.Equal("{}", new BlockData().ToJson());
        Assert.Equal("""{"unique":"u1","blocks":[]}""", new BlockData { Unique = "u1", Blocks = [] }.ToJson());
    }

    [Fact]
    public void ToJson_KeepsNullPropertyValues()
    {
        var block = new BlockData { Properties = new Dictionary<string, JsonNode?> { ["title"] = null } };

        Assert.Equal("""{"properties":{"title":null}}""", block.ToJson());
    }

    [Fact]
    public void ToJson_KeepsPropertyAliasCasing()
    {
        var block = new BlockData { Properties = new Dictionary<string, JsonNode?> { ["MyTitle"] = "x" } };

        Assert.Equal("""{"properties":{"MyTitle":"x"}}""", block.ToJson());
    }

    [Fact]
    public void UnknownMembers_AreRoundTrippedUntouched()
    {
        const string json = """{"unique":"u1","identifier":"main","clientState":{"selected":true,"order":[1,2]}}""";

        var block = BlockData.Parse(json)!;

        Assert.Equal(["identifier", "clientState"], block.AdditionalData!.Keys);
        Assert.Equal(json, block.ToJson());
    }

    [Fact]
    public void ToJsonNode_ProducesTheSameDocumentAsToJson()
    {
        var block = BlockData.Parse("""{"unique":"u1","properties":{"title":"x"},"blocks":[{"unique":"u2"}]}""")!;

        Assert.Equal(block.ToJson(), block.ToJsonNode()!.ToJsonString());
    }
}
