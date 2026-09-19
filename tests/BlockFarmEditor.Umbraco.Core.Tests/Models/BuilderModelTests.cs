using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using BlockFarmEditor.Umbraco.Core.Models.ConfigModels;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace BlockFarmEditor.Umbraco.Core.Tests.Models;

public class BuilderModelTests
{
    private static IPublishedElement Element(Guid elementKey, Guid contentTypeKey)
    {
        var contentType = new Mock<IPublishedContentType>();
        contentType.SetupGet(x => x.Key).Returns(contentTypeKey);

        var element = new Mock<IPublishedElement>();
        element.SetupGet(x => x.Key).Returns(elementKey);
        element.SetupGet(x => x.ContentType).Returns(contentType.Object);
        return element.Object;
    }

    [Fact]
    public void PageDefinition_UsesTheWellKnownGuid_ForUniqueAndContentTypeKey()
    {
        var page = new PageDefinition();

        Assert.Equal("64D51B94-10C1-4225-9329-B2A54D0E47CE", PageDefinition.GuidUnique);
        Assert.Equal(new Guid(PageDefinition.GuidUnique), page.Unique);
        Assert.Equal(new Guid(PageDefinition.GuidUnique), page.ContentTypeKey);
        Assert.Empty(page.Blocks);
    }

    [Fact]
    public void PageDefinition_ContentTypeKey_IsIndependentOfUnique()
    {
        var page = new PageDefinition { Unique = Guid.NewGuid() };

        Assert.Equal(new Guid(PageDefinition.GuidUnique), page.ContentTypeKey);
    }

    [Fact]
    public void BlockDefinition_WithoutProperties_HasNoContentTypeKey()
    {
        var block = new BlockDefinition<IPublishedElement>();

        Assert.Null(block.Properties);
        Assert.Null(block.ContentTypeKey);
        Assert.Equal(Guid.Empty, block.Unique);
        Assert.Empty(block.Blocks);
    }

    [Fact]
    public void BlockDefinition_SettingProperties_TakesUniqueFromElementKey_AndContentTypeKeyFromContentType()
    {
        var elementKey = Guid.NewGuid();
        var contentTypeKey = Guid.NewGuid();

        var block = new BlockDefinition<IPublishedElement> { Properties = Element(elementKey, contentTypeKey) };

        Assert.Equal(elementKey, block.Unique);
        Assert.Equal(contentTypeKey, block.ContentTypeKey);
    }

    [Fact]
    public void BlockDefinition_SettingPropertiesToNull_KeepsExistingUnique()
    {
        var unique = Guid.NewGuid();

        var block = new BlockDefinition<IPublishedElement> { Unique = unique, Properties = null };

        Assert.Equal(unique, block.Unique);
        Assert.Null(block.ContentTypeKey);
    }

    [Fact]
    public void BlockDefinition_IsAContainerDefinition()
    {
        var child = new BlockDefinition<IPublishedElement> { Unique = Guid.NewGuid() };
        IContainerDefinition container = new BlockDefinition<IPublishedElement> { Blocks = [child] };

        Assert.Same(child, Assert.Single(container.Blocks));
    }

    [Fact]
    public void PropertyEditorModel_Defaults()
    {
        var model = new BlockFarmEditorPropertyEditorModel();

        Assert.Equal(string.Empty, model.Alias);
        Assert.Null(model.Label);
        Assert.Null(model.Description);
        Assert.Null(model.Validation);
        Assert.Empty(model.Configurations);
    }

    [Fact]
    public void PropertyGroupModel_ConstructorAssignsValues()
    {
        var model = new BlockFarmEditorPropertyGroupModel("content", "Content", PropertyGroupType.Tab);

        Assert.Equal("content", model.Alias);
        Assert.Equal("Content", model.Label);
        Assert.Equal(PropertyGroupType.Tab, model.Type);
        Assert.Null(model.Editors);
        Assert.Null(model.Groups);
    }

    [Fact]
    public void ConfigItem_Defaults_AndImplementsInterface()
    {
        IBlockFarmEditorConfigItem item = new BlockFarmEditorConfigItem();

        Assert.Equal(string.Empty, item.Alias);
        Assert.Null(item.Value);
    }

    [Fact]
    public void DropdownEditorConfigItem_ParameterlessConstructor_UsesDefaults()
    {
        var item = new DropdownEditorConfigItem();

        Assert.Equal(string.Empty, item.Name);
        Assert.Null(item.Value);
    }

    [Fact]
    public void DropdownEditorConfigItem_Constructor_AssignsNameAndValue()
    {
        var item = new DropdownEditorConfigItem("Red", "#f00");

        Assert.Equal("Red", item.Name);
        Assert.Equal("#f00", item.Value);
    }
}
