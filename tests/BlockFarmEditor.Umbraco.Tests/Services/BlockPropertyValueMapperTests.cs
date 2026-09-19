using System.Text.Json.Nodes;
using BlockFarmEditor.Umbraco.Core.Attributes;
using BlockFarmEditor.Umbraco.Core.Models.ConfigModels;
using BlockFarmEditor.Umbraco.Library.Models;
using BlockFarmEditor.Umbraco.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;

namespace BlockFarmEditor.Umbraco.Tests.Services;

public class BlockPropertyValueMapperTests
{
    public interface IThemeSource
    {
        int Reads { get; }
        IEnumerable<string> Themes { get; }
    }

    /// <summary>Resolved through DI by the mapper, like a consumer's <see cref="IBlockFarmEditorConfig"/> would be.</summary>
    public class ThemeConfig(IThemeSource source) : IBlockFarmEditorConfig
    {
        public Task<IEnumerable<BlockFarmEditorConfigItem>> GetItems() =>
            Task.FromResult<IEnumerable<BlockFarmEditorConfigItem>>(
            [
                new BlockFarmEditorConfigItem { Alias = "items", Value = source.Themes },
                new BlockFarmEditorConfigItem { Alias = "unset", Value = null }
            ]);
    }

    private sealed class ThemeSource : IThemeSource
    {
        public int Reads { get; private set; }

        public IEnumerable<string> Themes
        {
            get
            {
                Reads++;
                return ["light", "dark"];
            }
        }
    }

    private const string ContentTypeAlias = "heroBlock";

    private readonly ThemeSource _themes = new();
    private readonly BlockFarmServiceProvider _services;

    public BlockPropertyValueMapperTests()
    {
        _services = new BlockFarmServiceProvider(services => services.AddSingleton<IThemeSource>(_themes));
    }

    #region Arrange helpers

    private Guid AddContentType(params IPropertyType[] propertyTypes) => _services.AddContentType(ContentTypeAlias, propertyTypes).Object.Key;

    private static BlockData Block(Guid contentTypeKey, JsonObject properties, params BlockData[] blocks) => new()
    {
        ContentTypeKey = contentTypeKey.ToString(),
        Unique = Guid.NewGuid().ToString(),
        // parsed, so the values are the clr values they would be when read from the editor or the database
        Properties = BlockData.Parse(new JsonObject { ["properties"] = properties.DeepClone() })!.Properties,
        Blocks = [.. blocks]
    };

    /// <summary>A property-less block, like the page root or a block area.</summary>
    private static BlockData Container(params BlockData?[] blocks) => new() { Unique = Guid.NewGuid().ToString(), Blocks = [.. blocks] };

    private void ConfigureThemeOverride(string propertyAlias = "theme") =>
        _services.BlockDefinitionService.Setup(x => x.GetConfigMaps()).Returns(new Dictionary<string, IEnumerable<BlockFarmEditorConfigurationAttribute>>
        {
            [ContentTypeAlias] = [new BlockFarmEditorConfigurationAttribute(ContentTypeAlias, propertyAlias, typeof(ThemeConfig))]
        });

    private static string Json(object? value) => System.Text.Json.JsonSerializer.Serialize(value, BlockData.SerializerOptions);

    private static object? SourceValue(IPublishedElement element, string alias) => element.Properties.Single(x => x.Alias == alias).GetSourceValue();

    #endregion

    #region FromEditor

    [Fact]
    public void FromEditor_RunsPropertiesThroughTheirValueEditors_InPlace()
    {
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias, fromEditor: value => $"stored:{value}");
        var block = Block(AddContentType(UmbracoModels.PropertyType("title")), new JsonObject { ["title"] = "Hello" });
        var (contentTypeKey, unique) = (block.ContentTypeKey, block.Unique);

        _services.Mapper.FromEditor(block);

        Assert.Equal("stored:Hello", block.Properties!["title"]);
        Assert.Equal(contentTypeKey, block.ContentTypeKey);
        Assert.Equal(unique, block.Unique);
    }

    [Fact]
    public void FromEditor_MapsNestedBlocks_AtEveryDepth()
    {
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias, fromEditor: value => $"stored:{value}");
        var contentTypeKey = AddContentType(UmbracoModels.PropertyType("title"));
        var inner = Block(contentTypeKey, new JsonObject { ["title"] = "inner" });
        var outer = Block(contentTypeKey, new JsonObject { ["title"] = "outer" }, Container(inner));

        _services.Mapper.FromEditor(Container(Container(outer)));

        Assert.Equal("stored:outer", outer.Properties!["title"]);
        Assert.Equal("stored:inner", inner.Properties!["title"]);
    }

    [Fact]
    public void FromEditor_RemovesNullBlocks()
    {
        var root = Container(null, Container(), null);

        _services.Mapper.FromEditor(root);

        Assert.Single(root.Blocks!);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void FromEditor_BlockWithoutAValidContentTypeKey_KeepsItsPropertiesUntouched(string? contentTypeKey)
    {
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias, fromEditor: value => $"stored:{value}");
        var block = new BlockData { ContentTypeKey = contentTypeKey, Properties = new Dictionary<string, object?> { ["title"] = "Hello" } };

        _services.Mapper.FromEditor(block);

        Assert.Equal("Hello", block.Properties!["title"]);
        _services.ContentTypeService.VerifyNoOtherCalls();
    }

    [Fact]
    public void FromEditor_BlockOfAnUnknownContentType_KeepsItsPropertiesUntouched()
    {
        var block = Block(Guid.NewGuid(), new JsonObject { ["title"] = "Hello" });

        _services.Mapper.FromEditor(block);

        Assert.Equal("Hello", block.Properties!["title"]);
    }

    [Theory]
    [InlineData(ValueTypes.Integer, "42", typeof(int), "42")]
    [InlineData(ValueTypes.String, "42", typeof(int), "42")]
    [InlineData(ValueTypes.Integer, "99999999999", typeof(long), "99999999999")]
    [InlineData(ValueTypes.String, "12.5", typeof(double), "12.5")]
    [InlineData(ValueTypes.Decimal, "12.5", typeof(double), "12.5")]
    [InlineData(ValueTypes.Decimal, "7", typeof(int), "7")]
    [InlineData(ValueTypes.String, "true", typeof(bool), "True")]
    [InlineData(ValueTypes.String, "false", typeof(bool), "False")]
    [InlineData(ValueTypes.String, "\"text\"", typeof(string), "text")]
    [InlineData(ValueTypes.Decimal, "\"12.5\"", typeof(string), "12.5")]
    public void FromEditor_HandsValueEditorsClrValues_LikeTheManagementApiDoes(string valueType, string json, Type expectedType, string expectedValue)
    {
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias, valueType);
        var block = Block(AddContentType(UmbracoModels.PropertyType("value")), new JsonObject { ["value"] = JsonNode.Parse(json) });

        _services.Mapper.FromEditor(block);

        var received = Assert.Single(_services.FromEditorCalls).Value;
        Assert.IsType(expectedType, received);
        Assert.Equal(expectedValue, Convert.ToString(received, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void FromEditor_ObjectValues_ReachTheValueEditorAsJson_AndAreStoredAsJson()
    {
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias);
        var block = Block(AddContentType(UmbracoModels.PropertyType("link")), new JsonObject { ["link"] = new JsonObject { ["url"] = "/about" } });

        _services.Mapper.FromEditor(block);

        Assert.IsAssignableFrom<JsonNode>(Assert.Single(_services.FromEditorCalls).Value);
        Assert.Equal("""{"url":"/about"}""", Json(block.Properties!["link"]));
    }

    [Fact]
    public void FromEditor_ArrayValues_SurviveTheRoundTrip()
    {
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias);
        var block = Block(AddContentType(UmbracoModels.PropertyType("tags")), new JsonObject { ["tags"] = new JsonArray("a", "b") });

        _services.Mapper.FromEditor(block);

        Assert.Equal("""["a","b"]""", Json(block.Properties!["tags"]));
    }

    [Fact]
    public void FromEditor_ValueEditorResults_AreStoredAsJson_WhateverTheirClrType()
    {
        _services.AddEditor("Test.Number", fromEditor: _ => 42);
        _services.AddEditor("Test.Bool", fromEditor: _ => true);
        _services.AddEditor("Test.Object", fromEditor: _ => new { Url = "/about", IsExternal = false });
        _services.AddEditor("Test.Null", fromEditor: _ => null);
        var block = Block(
            AddContentType(
                UmbracoModels.PropertyType("number", editorAlias: "Test.Number"),
                UmbracoModels.PropertyType("flag", editorAlias: "Test.Bool"),
                UmbracoModels.PropertyType("link", editorAlias: "Test.Object"),
                UmbracoModels.PropertyType("nothing", editorAlias: "Test.Null")),
            new JsonObject { ["number"] = "x", ["flag"] = "x", ["link"] = "x", ["nothing"] = "x" });

        _services.Mapper.FromEditor(block);

        Assert.Equal("""{"properties":{"number":42,"flag":true,"link":{"url":"/about","isExternal":false},"nothing":null}}""",
            new BlockData { Properties = block.Properties }.ToJson());
    }

    [Fact]
    public void FromEditor_NullPropertyValue_ReachesTheValueEditorAsNull()
    {
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias);
        var block = Block(AddContentType(UmbracoModels.PropertyType("title")), new JsonObject { ["title"] = null });

        _services.Mapper.FromEditor(block);

        Assert.Null(Assert.Single(_services.FromEditorCalls).Value);
        Assert.True(block.Properties!.ContainsKey("title"));
        Assert.Null(block.Properties["title"]);
    }

    [Fact]
    public void FromEditor_MatchesPropertiesCaseInsensitively_AndKeepsTheIncomingKey()
    {
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias);
        var block = Block(AddContentType(UmbracoModels.PropertyType("title")), new JsonObject { ["Title"] = "Hello" });

        _services.Mapper.FromEditor(block);

        Assert.Equal(["Title"], block.Properties!.Keys);
    }

    [Fact]
    public void FromEditor_DropsValuesThatDoNotBelongToTheContentType()
    {
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias);
        var block = Block(AddContentType(UmbracoModels.PropertyType("title")), new JsonObject { ["title"] = "Hello", ["rogue"] = "x" });

        _services.Mapper.FromEditor(block);

        Assert.Equal(["title"], block.Properties!.Keys);
    }

    [Fact]
    public void FromEditor_PropertiesWithoutAValueInTheJson_AreNotSentToTheValueEditor()
    {
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias);
        var block = Block(AddContentType(UmbracoModels.PropertyType("title"), UmbracoModels.PropertyType("subtitle")), new JsonObject { ["title"] = "Hello" });

        _services.Mapper.FromEditor(block);

        Assert.Single(_services.FromEditorCalls);
        Assert.Equal(["title"], block.Properties!.Keys);
    }

    [Fact]
    public void FromEditor_PropertyWhoseEditorIsNotInstalled_IsDropped_WithoutAffectingOthers()
    {
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias);
        var block = Block(
            AddContentType(UmbracoModels.PropertyType("title"), UmbracoModels.PropertyType("map", editorAlias: "Missing.Editor")),
            new JsonObject { ["title"] = "Hello", ["map"] = "somewhere" });

        _services.Mapper.FromEditor(block);

        Assert.Equal(["title"], block.Properties!.Keys);
    }

    [Fact]
    public void FromEditor_PropertyWhoseValueEditorThrows_IsDropped_WithoutAffectingOthers()
    {
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias);
        _services.AddEditor("Broken.Editor", fromEditor: _ => throw new InvalidOperationException("boom"));
        var block = Block(
            AddContentType(UmbracoModels.PropertyType("title"), UmbracoModels.PropertyType("broken", editorAlias: "Broken.Editor")),
            new JsonObject { ["title"] = "Hello", ["broken"] = "x" });

        _services.Mapper.FromEditor(block);

        Assert.Equal(["title"], block.Properties!.Keys);
    }

    #endregion

    #region FromEditor - configuration

    [Fact]
    public void FromEditor_PassesTheDataTypesConfiguration_ToTheValueEditor()
    {
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias);
        var dataTypeKey = Guid.NewGuid();
        var configuration = new object();
        _services.DataTypeConfigurationCache.Setup(x => x.GetConfiguration(dataTypeKey)).Returns(configuration);
        var block = Block(AddContentType(UmbracoModels.PropertyType("title", dataTypeKey)), new JsonObject { ["title"] = "Hello" });

        _services.Mapper.FromEditor(block);

        Assert.Same(configuration, Assert.Single(_services.FromEditorCalls).DataTypeConfiguration);
    }

    [Fact]
    public void FromEditor_ARegisteredBlockFarmEditorConfig_IsConvertedToTheEditorsConfigurationObject_AndReplacesTheDataTypes()
    {
        var editorConfiguration = new object();
        IDictionary<string, object>? convertedFrom = null;
        var configurationEditor = new Mock<IConfigurationEditor>();
        configurationEditor
            .Setup(x => x.ToConfigurationObject(It.IsAny<IDictionary<string, object>>(), It.IsAny<IConfigurationEditorJsonSerializer>()))
            .Callback((IDictionary<string, object> items, IConfigurationEditorJsonSerializer _) => convertedFrom = items)
            .Returns(editorConfiguration);
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias).Setup(x => x.GetConfigurationEditor()).Returns(configurationEditor.Object);
        ConfigureThemeOverride("Theme"); // alias matching is case-insensitive
        var block = Block(AddContentType(UmbracoModels.PropertyType("theme"), UmbracoModels.PropertyType("title")), new JsonObject { ["theme"] = "dark", ["title"] = "Hello" });

        _services.Mapper.FromEditor(block);

        Assert.Same(editorConfiguration, _services.FromEditorCalls.Single(x => Equals(x.Value, "dark")).DataTypeConfiguration);
        Assert.NotSame(editorConfiguration, _services.FromEditorCalls.Single(x => Equals(x.Value, "Hello")).DataTypeConfiguration);
        // null items cannot be represented in a data type configuration, so they are left out
        Assert.Equal(["items"], convertedFrom!.Keys);
        Assert.Equal(["light", "dark"], Assert.IsAssignableFrom<IEnumerable<string>>(convertedFrom["items"]));
    }

    [Fact]
    public void FromEditor_WhenTheOverrideCannotBeConverted_FallsBackToTheRawItems()
    {
        var configurationEditor = new Mock<IConfigurationEditor>();
        configurationEditor
            .Setup(x => x.ToConfigurationObject(It.IsAny<IDictionary<string, object>>(), It.IsAny<IConfigurationEditorJsonSerializer>()))
            .Throws(new InvalidOperationException("cannot convert"));
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias).Setup(x => x.GetConfigurationEditor()).Returns(configurationEditor.Object);
        ConfigureThemeOverride();
        var block = Block(AddContentType(UmbracoModels.PropertyType("theme")), new JsonObject { ["theme"] = "dark" });

        _services.Mapper.FromEditor(block);

        var configuration = Assert.IsType<Dictionary<string, object?>>(Assert.Single(_services.FromEditorCalls).DataTypeConfiguration);
        Assert.Equal(["items", "unset"], configuration.Keys);
    }

    [Fact]
    public void FromEditor_ResolvesEachPropertysConfigurationOncePerPass_HoweverManyBlocksUseIt()
    {
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias);
        ConfigureThemeOverride();
        var dataTypeKey = Guid.NewGuid();
        var contentTypeKey = AddContentType(UmbracoModels.PropertyType("theme"), UmbracoModels.PropertyType("title", dataTypeKey));
        var blocks = Enumerable.Range(0, 3).Select(_ => Block(contentTypeKey, new JsonObject { ["theme"] = "dark", ["title"] = "Hello" })).ToArray();

        _services.Mapper.FromEditor(Container(blocks));

        Assert.Equal(6, _services.FromEditorCalls.Count);
        Assert.Equal(1, _themes.Reads);
        _services.DataTypeConfigurationCache.Verify(x => x.GetConfiguration(dataTypeKey), Times.Once);
    }

    [Fact]
    public void FromEditor_DoesNotReuseConfigurationsBetweenPasses()
    {
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias);
        ConfigureThemeOverride();
        var contentTypeKey = AddContentType(UmbracoModels.PropertyType("theme"));

        _services.Mapper.FromEditor(Block(contentTypeKey, new JsonObject { ["theme"] = "dark" }));
        _services.Mapper.FromEditor(Block(contentTypeKey, new JsonObject { ["theme"] = "dark" }));

        Assert.Equal(2, _themes.Reads);
    }

    #endregion

    #region ToEditor

    [Fact]
    public void ToEditor_RunsStoredValuesThroughToEditor_AtEveryDepth()
    {
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias, toEditor: value => $"editor:{value}");
        var contentTypeKey = AddContentType(UmbracoModels.PropertyType("title"));
        var inner = Block(contentTypeKey, new JsonObject { ["title"] = "inner" });
        var outer = Block(contentTypeKey, new JsonObject { ["title"] = "outer" }, Container(inner));

        _services.Mapper.ToEditor(Container(outer));

        Assert.Equal("editor:outer", outer.Properties!["title"]);
        Assert.Equal("editor:inner", inner.Properties!["title"]);
    }

    [Fact]
    public void ToEditor_DoesNotResolveConfigurations_OrCallFromEditor()
    {
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias);
        ConfigureThemeOverride();
        var block = Block(AddContentType(UmbracoModels.PropertyType("theme")), new JsonObject { ["theme"] = "dark" });

        _services.Mapper.ToEditor(block);

        Assert.Empty(_services.FromEditorCalls);
        Assert.Equal(0, _themes.Reads);
        _services.DataTypeConfigurationCache.VerifyNoOtherCalls();
    }

    [Fact]
    public void ToEditor_BlockOfAnUnknownContentType_KeepsItsStoredProperties()
    {
        var block = Block(Guid.NewGuid(), new JsonObject { ["title"] = "Hello" });

        _services.Mapper.ToEditor(block);

        Assert.Equal("Hello", block.Properties!["title"]);
    }

    [Fact]
    public void ToEditor_PropertyThatVariesByCulture_KeepsItsValue_BecauseBlocksAreStoredInvariantly()
    {
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias);
        var propertyType = UmbracoModels.PropertyType("title");
        propertyType.Variations = ContentVariation.CultureAndSegment;
        var block = Block(AddContentType(propertyType), new JsonObject { ["title"] = "Hello" });

        _services.Mapper.ToEditor(block);

        Assert.Equal("Hello", block.Properties!["title"]);
        // the content type service hands out cached instances, so the variance is ignored on a copy
        Assert.Equal(ContentVariation.CultureAndSegment, propertyType.Variations);
    }

    [Fact]
    public void ToEditor_AJsonNodeAlreadyAttachedElsewhere_IsSerialized_RatherThanFailing()
    {
        var owner = new JsonObject { ["shared"] = new JsonObject { ["url"] = "/about" } };
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias, toEditor: _ => owner["shared"]);
        var block = Block(AddContentType(UmbracoModels.PropertyType("link")), new JsonObject { ["link"] = "x" });

        _services.Mapper.ToEditor(block);

        Assert.Equal("""{"url":"/about"}""", Json(block.Properties!["link"]));
        Assert.NotNull(owner["shared"]);
        Assert.NotNull(block.ToJson());
    }

    [Fact]
    public void FromEditorThenToEditor_RoundTripsValues_ThroughSymmetricEditors()
    {
        _services.AddEditor(UmbracoModels.TextBoxEditorAlias, fromEditor: value => $"[{value}]", toEditor: value => ((string)value!).Trim('[', ']'));
        var block = Block(AddContentType(UmbracoModels.PropertyType("title")), new JsonObject { ["title"] = "Hello" });

        _services.Mapper.FromEditor(block);
        var stored = BlockData.Parse(block.ToJson())!;
        _services.Mapper.ToEditor(stored);

        Assert.Equal("[Hello]", block.Properties!["title"]);
        Assert.Equal("Hello", stored.Properties!["title"]);
    }

    #endregion

    #region ToBlockDefinition

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void ToBlockDefinition_WithoutAValidUnique_ReturnsNull(string? unique)
    {
        Assert.Null(_services.Mapper.ToBlockDefinition(new BlockData { Unique = unique }));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    public void ToBlockDefinition_WithoutAValidContentTypeKey_ReturnsAPropertylessBlock(string? contentTypeKey)
    {
        var unique = Guid.NewGuid();

        var block = _services.Mapper.ToBlockDefinition(new BlockData { Unique = unique.ToString(), ContentTypeKey = contentTypeKey });

        Assert.NotNull(block);
        Assert.Equal(unique, block.Unique);
        Assert.Null(block.Properties);
        Assert.Null(block.ContentTypeKey);
        Assert.Empty(block.Blocks);
    }

    [Fact]
    public void ToBlockDefinition_WithUnknownContentType_ReturnsAPropertylessBlock()
    {
        var block = _services.Mapper.ToBlockDefinition(Block(Guid.NewGuid(), new JsonObject { ["title"] = "x" }));

        Assert.NotNull(block);
        Assert.Null(block.Properties);
    }

    [Fact]
    public void ToBlockDefinition_WithKnownContentType_BuildsAPublishedElement_KeyedByUnique()
    {
        var contentTypeKey = _services.AddPublishedElementType("title");
        var data = Block(contentTypeKey, new JsonObject { ["title"] = "Hello" });

        var block = _services.Mapper.ToBlockDefinition(data);

        Assert.NotNull(block?.Properties);
        Assert.Equal(Guid.Parse(data.Unique!), block.Unique);
        Assert.Equal(block.Unique, block.Properties.Key);
        Assert.Equal(contentTypeKey, block.ContentTypeKey);
        Assert.Equal("Hello", SourceValue(block.Properties, "title"));
    }

    [Fact]
    public void ToBlockDefinition_PassesTheElementThroughTheModelFactory()
    {
        var contentTypeKey = _services.AddPublishedElementType("title");
        var typedModel = Definitions.Element(contentTypeKey);
        _services.ModelFactory.Setup(x => x.CreateModel(It.IsAny<IPublishedElement>())).Returns(typedModel);

        var block = _services.Mapper.ToBlockDefinition(Block(contentTypeKey, []));

        Assert.Same(typedModel, block!.Properties);
    }

    [Fact]
    public void ToBlockDefinition_TurnsStoredJson_IntoTheSourceValuesPropertyValueConvertersExpect()
    {
        _services.AddEditor("Test.Decimal", ValueTypes.Decimal);
        var contentTypeKey = _services.AddPublishedElementType(
            ("text", UmbracoModels.TextBoxEditorAlias), ("count", UmbracoModels.TextBoxEditorAlias), ("price", "Test.Decimal"),
            ("yes", UmbracoModels.TextBoxEditorAlias), ("nothing", UmbracoModels.TextBoxEditorAlias), ("link", UmbracoModels.TextBoxEditorAlias));

        var block = _services.Mapper.ToBlockDefinition(Block(contentTypeKey, new JsonObject
        {
            ["text"] = "Hello", ["count"] = 3, ["price"] = 12.5, ["yes"] = true, ["nothing"] = null, ["link"] = new JsonObject { ["url"] = "/about" }
        }))!;

        Assert.Equal("Hello", SourceValue(block.Properties!, "text"));
        Assert.Equal(3, SourceValue(block.Properties!, "count"));
        Assert.Equal(12.5, SourceValue(block.Properties!, "price"));
        Assert.Equal(true, SourceValue(block.Properties!, "yes"));
        Assert.Null(SourceValue(block.Properties!, "nothing"));
        Assert.Equal("""{"url":"/about"}""", Assert.IsAssignableFrom<JsonNode>(SourceValue(block.Properties!, "link")).ToJsonString());
    }

    [Fact]
    public void ToBlockDefinition_MatchesStoredPropertiesCaseInsensitively_AndIgnoresUnknownOnes()
    {
        var contentTypeKey = _services.AddPublishedElementType("title", "subtitle");

        var block = _services.Mapper.ToBlockDefinition(Block(contentTypeKey, new JsonObject { ["Title"] = "x", ["rogue"] = "y" }))!;

        Assert.Equal(["title", "subtitle"], block.Properties!.Properties.Select(x => x.Alias));
        Assert.Equal("x", SourceValue(block.Properties, "title"));
        Assert.Null(SourceValue(block.Properties, "subtitle"));
    }

    [Fact]
    public void ToBlockDefinition_BuildsNestedBlocks_DroppingInvalidOnes()
    {
        var contentTypeKey = _services.AddPublishedElementType("title");
        var grandChild = Block(contentTypeKey, new JsonObject { ["title"] = "deep" });
        var child = Container(grandChild);

        var block = _services.Mapper.ToBlockDefinition(Container(child, null, new BlockData { Unique = "invalid" }))!;

        var childBlock = Assert.Single(block.Blocks);
        Assert.Equal(Guid.Parse(child.Unique!), childBlock.Unique);
        Assert.Equal("deep", SourceValue(Assert.Single(childBlock.Blocks).Properties!, "title"));
    }

    #endregion

    #region ToPageDefinition

    [Fact]
    public void ToPageDefinition_BuildsThePagesBlocks_DroppingInvalidOnes()
    {
        var area = Container();

        var page = _services.Mapper.ToPageDefinition(new BlockData { Blocks = [area, null, new BlockData { Unique = "invalid" }] });

        Assert.Equal(Guid.Parse(area.Unique!), Assert.Single(page.Blocks).Unique);
    }

    [Fact]
    public void ToPageDefinition_KeepsTheWellKnownPageUnique_UnlessTheStoredRootHasItsOwn()
    {
        var unique = Guid.NewGuid();

        Assert.Equal(new Guid(Core.Models.BuilderModels.PageDefinition.GuidUnique), _services.Mapper.ToPageDefinition(new BlockData()).Unique);
        Assert.Equal(new Guid(Core.Models.BuilderModels.PageDefinition.GuidUnique), _services.Mapper.ToPageDefinition(new BlockData { Unique = "invalid" }).Unique);
        Assert.Equal(unique, _services.Mapper.ToPageDefinition(new BlockData { Unique = unique.ToString() }).Unique);
    }

    [Fact]
    public void ToPageDefinition_OfAnEmptyRoot_IsAnEmptyPage()
    {
        Assert.Empty(_services.Mapper.ToPageDefinition(new BlockData()).Blocks);
    }

    #endregion
}
