using System.Text.Json;
using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using BlockFarmEditor.Umbraco.Library.Converters;
using BlockFarmEditor.Umbraco.Tests.Helpers;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;

namespace BlockFarmEditor.Umbraco.Tests.Converters;

public class BlockFarmEditorUmbracoConverterTests
{
    private readonly BlockFarmServiceProvider _services = new();
    private readonly BlockFarmEditorUmbracoConverter _converter;
    private readonly IPublishedElement _owner = Mock.Of<IPublishedElement>();

    public BlockFarmEditorUmbracoConverterTests()
    {
        _services.BlockDefinitionService.SetupGet(x => x.JsonSerializerReaderOptions).Returns(() => _services.ReaderOptions());
        _converter = new BlockFarmEditorUmbracoConverter(_services.BlockDefinitionService.Object);
    }

    private static IPublishedPropertyType PropertyType(string editorAlias = "blockfarmeditor_page_propertyeditor")
    {
        var propertyType = new Mock<IPublishedPropertyType>();
        propertyType.SetupGet(x => x.EditorAlias).Returns(editorAlias);
        return propertyType.Object;
    }

    [Theory]
    [InlineData("blockfarmeditor_page_propertyeditor", true)]
    [InlineData("BLOCKFARMEDITOR_PAGE_PROPERTYEDITOR", true)]
    [InlineData("Umbraco.BlockList", false)]
    [InlineData("", false)]
    public void IsConverter_OnlyForTheBlockFarmEditorPropertyEditor(string editorAlias, bool expected)
    {
        Assert.Equal(expected, _converter.IsConverter(PropertyType(editorAlias)));
    }

    [Fact]
    public void IsRegisteredAsADefaultConverter_SoSiteSpecificConvertersCanOverrideIt()
    {
        Assert.NotEmpty(typeof(BlockFarmEditorUmbracoConverter).GetCustomAttributes(typeof(DefaultPropertyValueConverterAttribute), false));
    }

    [Fact]
    public void PropertyValueType_IsPageDefinition_CachedPerElement()
    {
        Assert.Equal(typeof(PageDefinition), _converter.GetPropertyValueType(PropertyType()));
        Assert.Equal(PropertyCacheLevel.Element, _converter.GetPropertyCacheLevel(PropertyType()));
    }

    [Fact]
    public void IsValue_OnlyForPageDefinitions()
    {
        Assert.True(_converter.IsValue(new PageDefinition(), PropertyValueLevel.Object));
        Assert.False(_converter.IsValue(null, PropertyValueLevel.Source));
        Assert.False(_converter.IsValue("{\"blocks\":[]}", PropertyValueLevel.Source));
    }

    [Fact]
    public void ConvertSourceToIntermediate_FromJsonString_ReturnsThePageDefinition()
    {
        var area = Guid.NewGuid();

        var result = _converter.ConvertSourceToIntermediate(_owner, PropertyType(), $$$"""{"blocks":[{"unique":"{{{area}}}","blocks":[]}]}""", false);

        var page = Assert.IsType<PageDefinition>(result);
        Assert.Equal(area, Assert.Single(page.Blocks).Unique);
    }

    [Fact]
    public void ConvertSourceToIntermediate_FromJsonDocument_ReturnsThePageDefinition()
    {
        var area = Guid.NewGuid();
        using var document = JsonDocument.Parse($$$"""{"blocks":[{"unique":"{{{area}}}"}]}""");

        var result = _converter.ConvertSourceToIntermediate(_owner, PropertyType(), document, false);

        var page = Assert.IsType<PageDefinition>(result);
        Assert.Equal(area, Assert.Single(page.Blocks).Unique);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("{\"blocks\":\"nope\"}")]
    public void ConvertSourceToIntermediate_FromUnusableJson_ReturnsNull_InsteadOfThrowing(string source)
    {
        Assert.Null(_converter.ConvertSourceToIntermediate(_owner, PropertyType(), source, false));
    }

    [Fact]
    public void ConvertSourceToIntermediate_FromAJsonDocumentThatIsNotAPage_ReturnsNull_InsteadOfThrowing()
    {
        using var document = JsonDocument.Parse("[1,2,3]");

        Assert.Null(_converter.ConvertSourceToIntermediate(_owner, PropertyType(), document, false));
    }

    [Fact]
    public void ConvertSourceToIntermediate_FromUnsupportedSourceTypes_ReturnsNull()
    {
        Assert.Null(_converter.ConvertSourceToIntermediate(_owner, PropertyType(), null, false));
        Assert.Null(_converter.ConvertSourceToIntermediate(_owner, PropertyType(), 42, false));
    }

    [Fact]
    public void ConvertIntermediateToObject_ReturnsTheIntermediateValueAsIs()
    {
        var page = new PageDefinition();

        Assert.Same(page, _converter.ConvertIntermediateToObject(_owner, PropertyType(), PropertyCacheLevel.Element, page, false));
        Assert.Null(_converter.ConvertIntermediateToObject(_owner, PropertyType(), PropertyCacheLevel.Element, null, false));
    }
}
