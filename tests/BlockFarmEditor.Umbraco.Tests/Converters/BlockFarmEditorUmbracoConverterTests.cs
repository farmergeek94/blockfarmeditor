using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using BlockFarmEditor.Umbraco.Library.Converters;
using BlockFarmEditor.Umbraco.Library.Models;
using BlockFarmEditor.Umbraco.Library.Services;
using BlockFarmEditor.Umbraco.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;

namespace BlockFarmEditor.Umbraco.Tests.Converters;

public class BlockFarmEditorUmbracoConverterTests
{
    private readonly BlockFarmServiceProvider _services = new();
    private readonly IPublishedElement _owner = Mock.Of<IPublishedElement>();
    private int _mapperResolutions;

    private BlockFarmEditorUmbracoConverter Converter(IBlockPropertyValueMapper? mapper = null) =>
        new(new Lazy<IBlockPropertyValueMapper>(() =>
        {
            _mapperResolutions++;
            return mapper ?? _services.Mapper;
        }), NullLogger<BlockFarmEditorUmbracoConverter>.Instance);

    private static IPublishedPropertyType PropertyType(string editorAlias = "blockfarmeditor_page_propertyeditor")
    {
        var propertyType = new Mock<IPublishedPropertyType>();
        propertyType.SetupGet(x => x.EditorAlias).Returns(editorAlias);
        propertyType.SetupGet(x => x.Alias).Returns("blocks");
        return propertyType.Object;
    }

    [Theory]
    [InlineData("blockfarmeditor_page_propertyeditor", true)]
    [InlineData("BLOCKFARMEDITOR_PAGE_PROPERTYEDITOR", true)]
    [InlineData("Umbraco.BlockList", false)]
    [InlineData("", false)]
    public void IsConverter_OnlyForTheBlockFarmEditorPropertyEditor(string editorAlias, bool expected)
    {
        Assert.Equal(expected, Converter().IsConverter(PropertyType(editorAlias)));
    }

    [Fact]
    public void IsRegisteredAsADefaultConverter_SoSiteSpecificConvertersCanOverrideIt()
    {
        Assert.NotEmpty(typeof(BlockFarmEditorUmbracoConverter).GetCustomAttributes(typeof(DefaultPropertyValueConverterAttribute), false));
    }

    [Fact]
    public void PropertyValueType_IsPageDefinition_CachedPerElement()
    {
        Assert.Equal(typeof(PageDefinition), Converter().GetPropertyValueType(PropertyType()));
        Assert.Equal(PropertyCacheLevel.Element, Converter().GetPropertyCacheLevel(PropertyType()));
    }

    [Fact]
    public void Construction_DoesNotResolveTheMapper_BecauseThatWouldBeACircularDependency()
    {
        // the mapper needs the published content type cache, which needs the property value converters
        var converter = Converter();
        converter.IsConverter(PropertyType());
        converter.IsValue("{}", PropertyValueLevel.Source);

        Assert.Equal(0, _mapperResolutions);
    }

    [Theory]
    [InlineData("{\"blocks\":[]}", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void IsValue_AtSourceLevel_IsAnyNonEmptyJsonString(string? source, bool expected)
    {
        Assert.Equal(expected, Converter().IsValue(source, PropertyValueLevel.Source));
    }

    [Fact]
    public void IsValue_AtSourceLevel_IsNotAPageDefinition()
    {
        Assert.False(Converter().IsValue(new PageDefinition(), PropertyValueLevel.Source));
    }

    [Theory]
    [InlineData(PropertyValueLevel.Inter)]
    [InlineData(PropertyValueLevel.Object)]
    public void IsValue_BeyondSourceLevel_IsOnlyAPageDefinition(PropertyValueLevel level)
    {
        Assert.True(Converter().IsValue(new PageDefinition(), level));
        Assert.False(Converter().IsValue("{\"blocks\":[]}", level));
        Assert.False(Converter().IsValue(null, level));
    }

    [Fact]
    public void ConvertSourceToIntermediate_FromStoredJson_ReturnsThePageDefinition()
    {
        var area = Guid.NewGuid();

        var result = Converter().ConvertSourceToIntermediate(_owner, PropertyType(), $$$"""{"blocks":[{"unique":"{{{area}}}","blocks":[]}]}""", false);

        var page = Assert.IsType<PageDefinition>(result);
        Assert.Equal(area, Assert.Single(page.Blocks).Unique);
    }

    [Fact]
    public void ConvertSourceToIntermediate_HandsTheParsedRootToTheMapper()
    {
        var page = new PageDefinition();
        BlockData? received = null;
        var mapper = new Mock<IBlockPropertyValueMapper>();
        mapper.Setup(x => x.ToPageDefinition(It.IsAny<BlockData>())).Callback((BlockData root) => received = root).Returns(page);

        var result = Converter(mapper.Object).ConvertSourceToIntermediate(_owner, PropertyType(), """{"unique":"abc","blocks":[{"unique":"def"}]}""", false);

        Assert.Same(page, result);
        Assert.Equal("abc", received!.Unique);
        Assert.Equal("def", Assert.Single(received.Blocks!)!.Unique);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{\"blocks\":\"nope\"}")]
    public void ConvertSourceToIntermediate_FromUnusableJson_ReturnsNull_InsteadOfThrowing(string source)
    {
        Assert.Null(Converter().ConvertSourceToIntermediate(_owner, PropertyType(), source, false));
    }

    [Fact]
    public void ConvertSourceToIntermediate_WhenTheMapperFails_ReturnsNull_InsteadOfThrowing()
    {
        var mapper = new Mock<IBlockPropertyValueMapper>();
        mapper.Setup(x => x.ToPageDefinition(It.IsAny<BlockData>())).Throws(new InvalidOperationException("boom"));

        Assert.Null(Converter(mapper.Object).ConvertSourceToIntermediate(_owner, PropertyType(), "{}", false));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("")]
    [InlineData("  ")]
    public void ConvertSourceToIntermediate_FromAnEmptyValue_ReturnsNull(string source)
    {
        Assert.Null(Converter().ConvertSourceToIntermediate(_owner, PropertyType(), source, false));
    }

    [Fact]
    public void ConvertSourceToIntermediate_FromNonStringSources_ReturnsNull_WithoutTouchingTheMapper()
    {
        Assert.Null(Converter().ConvertSourceToIntermediate(_owner, PropertyType(), null, false));
        Assert.Null(Converter().ConvertSourceToIntermediate(_owner, PropertyType(), 42, false));
        Assert.Equal(0, _mapperResolutions);
    }

    [Fact]
    public void ConvertIntermediateToObject_ReturnsTheIntermediateValueAsIs()
    {
        var page = new PageDefinition();

        Assert.Same(page, Converter().ConvertIntermediateToObject(_owner, PropertyType(), PropertyCacheLevel.Element, page, false));
        Assert.Null(Converter().ConvertIntermediateToObject(_owner, PropertyType(), PropertyCacheLevel.Element, null, false));
    }
}
