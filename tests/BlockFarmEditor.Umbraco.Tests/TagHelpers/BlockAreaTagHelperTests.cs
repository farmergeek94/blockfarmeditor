using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Core.Models;
using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using BlockFarmEditor.Umbraco.TagHelpers;
using BlockFarmEditor.Umbraco.Tests.Helpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace BlockFarmEditor.Umbraco.Tests.TagHelpers;

public class BlockAreaTagHelperTests
{
    // uuid5(PageDefinition.GuidUnique, identifier) as produced by an independent RFC 4122 implementation (python's uuid.uuid5)
    private static readonly Guid MainAreaUnique = Guid.Parse("5280d691-7534-5910-a0fc-267be0522621");
    private static readonly Guid SidebarAreaUnique = Guid.Parse("e256a7bd-7c88-5a3a-a71a-e35b5f7542ba");

    private readonly Mock<IBlockFarmEditorContext> _context = new();
    private readonly Mock<IBlockFarmEditorRenderService> _renderService = new();
    private readonly Mock<IHtmlHelper> _htmlHelper = new();
    private readonly ViewContext _viewContext = new();

    public BlockAreaTagHelperTests()
    {
        _context.SetupProperty(x => x._currentScope);
        _context
            .Setup(x => x.GetBlockScope(It.IsAny<IContainerDefinition>()))
            .Returns((IContainerDefinition block) => _context.Object._currentScope = new BlockFarmEditorContainerScope(_context.Object, block, _context.Object._currentScope));
    }

    private BlockAreaTagHelper TagHelper(string identifier, params string[] allowedBlocks) =>
        new(_context.Object, _renderService.Object, _htmlHelper.Object)
        {
            Identifier = identifier,
            ViewContext = _viewContext,
            AllowedBlocks = allowedBlocks
        };

    private void CurrentContainer(IContainerDefinition container) =>
        _context.Setup(x => x.GetBlockScope()).Returns(new BlockFarmEditorContainerScope(_context.Object, container, null));

    /// <summary>A block area is persisted as a property-less block whose unique is derived from the area identifier.</summary>
    private static BlockDefinition<IPublishedElement> Area(Guid unique, params BlockDefinition<IPublishedElement>[] blocks) => new()
    {
        Unique = unique,
        Blocks = blocks
    };

    private static async Task<TagHelperOutput> Process(BlockAreaTagHelper tagHelper)
    {
        var output = TagHelperTestHelper.Output("block-area");
        await tagHelper.ProcessAsync(TagHelperTestHelper.Context(), output);
        return output;
    }

    #region Edit mode

    [Fact]
    public async Task EditMode_RendersABlockAreaElement_WithIdentifierAndDeterministicUnique()
    {
        _context.SetupGet(x => x.IsEditMode).Returns(true);

        var output = await Process(TagHelper("main"));

        Assert.Equal("block-area", output.TagName);
        Assert.Equal("main", output.Attributes["identifier"].Value);
        Assert.Equal(MainAreaUnique, output.Attributes["unique"].Value);
        Assert.False(output.Attributes.ContainsName("allowedblocks"));
    }

    [Theory]
    [InlineData("main", "5280d691-7534-5910-a0fc-267be0522621")]
    [InlineData("sidebar", "e256a7bd-7c88-5a3a-a71a-e35b5f7542ba")]
    [InlineData("Main", "738c761c-d93c-59b1-b9e0-f956170a4c7d")]
    [InlineData("héllo wörld", "affeb27e-9b03-536f-b1f4-60d419e22a84")]
    public async Task EditMode_Unique_IsTheRfc4122Version5Guid_OfTheIdentifierInThePageNamespace(string identifier, string expected)
    {
        _context.SetupGet(x => x.IsEditMode).Returns(true);

        var output = await Process(TagHelper(identifier));

        Assert.Equal(Guid.Parse(expected), output.Attributes["unique"].Value);
    }

    [Fact]
    public async Task EditMode_WithAllowedBlocks_RendersThemCommaSeparated()
    {
        _context.SetupGet(x => x.IsEditMode).Returns(true);

        var output = await Process(TagHelper("main", "heroBlock", "cardBlock"));

        Assert.Equal("heroBlock,cardBlock", output.Attributes["allowedblocks"].Value);
    }

    [Fact]
    public async Task EditMode_NeverRendersBlocksServerSide()
    {
        _context.SetupGet(x => x.IsEditMode).Returns(true);

        await Process(TagHelper("main"));

        _renderService.VerifyNoOtherCalls();
    }

    #endregion

    #region Live mode

    [Fact]
    public async Task LiveMode_WithNoCurrentScope_SuppressesOutput()
    {
        _context.Setup(x => x.GetBlockScope()).Returns((BlockFarmEditorContainerScope?)null);

        var output = await Process(TagHelper("main"));

        Assert.Null(output.TagName);
        Assert.True(output.Content.IsEmptyOrWhiteSpace);
    }

    [Fact]
    public async Task LiveMode_WhenContainerHasNoBlocks_SuppressesOutput()
    {
        CurrentContainer(new PageDefinition());

        var output = await Process(TagHelper("main"));

        Assert.Null(output.TagName);
        Assert.True(output.Content.IsEmptyOrWhiteSpace);
    }

    [Fact]
    public async Task LiveMode_WhenAreaIsNotInTheContainer_RendersAHiddenNotFoundMarker()
    {
        CurrentContainer(new PageDefinition { Blocks = [Area(SidebarAreaUnique)] });

        var output = await Process(TagHelper("main"));

        Assert.Null(output.TagName);
        Assert.Equal("<div class=\"block-not-found\" style=\"display:none;\">Block not found</div>", output.Content.GetContent());
        _renderService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LiveMode_RendersTheAreasBlocks_InOrder_WithoutAWrapperElement()
    {
        var first = Definitions.Block(Guid.NewGuid());
        var second = Definitions.Block(Guid.NewGuid());
        var elsewhere = Definitions.Block(Guid.NewGuid());
        CurrentContainer(new PageDefinition { Blocks = [Area(SidebarAreaUnique, elsewhere), Area(MainAreaUnique, first, second)] });
        _renderService.Setup(x => x.RenderComponent(_htmlHelper.Object, first)).ReturnsAsync(new HtmlString("<p>first</p>"));
        _renderService.Setup(x => x.RenderComponent(_htmlHelper.Object, second)).ReturnsAsync(new HtmlString("<p>second</p>"));

        var output = await Process(TagHelper("main"));

        Assert.Null(output.TagName);
        Assert.Equal("<p>first</p><p>second</p>", output.Content.GetContent());
        _renderService.Verify(x => x.RenderComponent(_htmlHelper.Object, elsewhere), Times.Never);
    }

    [Fact]
    public async Task LiveMode_BlocksThatRenderNothing_AreSkipped()
    {
        var block = Definitions.Block(Guid.NewGuid());
        CurrentContainer(new PageDefinition { Blocks = [Area(MainAreaUnique, block)] });
        _renderService.Setup(x => x.RenderComponent(_htmlHelper.Object, block)).ReturnsAsync((IHtmlContent?)null);

        var output = await Process(TagHelper("main"));

        Assert.Equal(string.Empty, output.Content.GetContent());
    }

    [Fact]
    public async Task LiveMode_EachBlockRendersInsideItsOwnScope_WhichIsReleasedAfterwards()
    {
        var first = Definitions.Block(Guid.NewGuid());
        var second = Definitions.Block(Guid.NewGuid());
        CurrentContainer(new PageDefinition { Blocks = [Area(MainAreaUnique, first, second)] });
        var scopesDuringRender = new List<IContainerDefinition?>();
        _renderService
            .Setup(x => x.RenderComponent(_htmlHelper.Object, It.IsAny<BlockDefinition<IPublishedElement>>()))
            .Callback(() => scopesDuringRender.Add(_context.Object._currentScope?.Block))
            .ReturnsAsync(HtmlString.Empty);

        await Process(TagHelper("main"));

        Assert.Equal([first, second], scopesDuringRender);
        Assert.Null(_context.Object._currentScope);
    }

    [Fact]
    public async Task LiveMode_NestedArea_ResolvesAgainstTheCurrentBlock_NotThePage()
    {
        var nested = Definitions.Block(Guid.NewGuid());
        var container = Definitions.Block(Guid.NewGuid(), Area(MainAreaUnique, nested));
        CurrentContainer(container);
        _renderService.Setup(x => x.RenderComponent(_htmlHelper.Object, nested)).ReturnsAsync(new HtmlString("<p>nested</p>"));

        var output = await Process(TagHelper("main"));

        Assert.Equal("<p>nested</p>", output.Content.GetContent());
    }

    [Fact]
    public async Task LiveMode_ContextualizesTheHtmlHelperBeforeRendering()
    {
        var contextAware = _htmlHelper.As<IViewContextAware>();
        var block = Definitions.Block(Guid.NewGuid());
        CurrentContainer(new PageDefinition { Blocks = [Area(MainAreaUnique, block)] });

        await Process(TagHelper("main"));

        contextAware.Verify(x => x.Contextualize(_viewContext), Times.Once);
    }

    #endregion
}
