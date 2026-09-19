using BlockFarmEditor.Umbraco.Controllers;
using BlockFarmEditor.Umbraco.Core.DTO;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Core.Models;
using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using BlockFarmEditor.Umbraco.Tests.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Routing;

namespace BlockFarmEditor.Umbraco.Tests.Controllers;

public class BlockFarmEditorControllerTests
{
    // Only the method signatures matter - the controller inspects them via reflection.
    private class NoArgumentComponent { public string Invoke() => string.Empty; }
    private class SingleArgumentComponent { public Task<string> InvokeAsync(IPublishedElement heroModel) => Task.FromResult(string.Empty); }
    private class MultiArgumentComponent { public string Invoke(string title, int count) => string.Empty; }

    private readonly BlockFarmServiceProvider _services = new();
    private readonly Mock<IUmbracoContextFactory> _umbracoContextFactory = new();
    private readonly Mock<IUmbracoContext> _umbracoContext = new();
    private readonly Mock<IPublishedContentCache> _contentCache = new();
    private readonly Mock<IBlockFarmEditorContext> _blockFarmEditorContext = new();
    private readonly Mock<IBlockFarmEditorLayoutService> _layoutService = new();
    private readonly Mock<IUmbracoDatabase> _database = new();
    private readonly List<BlockFarmEditorDefinitionExpanded> _definitions = [];

    public BlockFarmEditorControllerTests()
    {
        UmbracoStaticServices.EnsureInitialized();

        _umbracoContext.SetupGet(x => x.Content).Returns(_contentCache.Object);
        _umbracoContext.SetupProperty(x => x.PublishedRequest);
        _umbracoContextFactory
            .Setup(x => x.EnsureUmbracoContext())
            .Returns(() => new UmbracoContextReference(_umbracoContext.Object, false, Mock.Of<IUmbracoContextAccessor>()));

        _services.BlockDefinitionService
            .Setup(x => x.RetrieveBlockFarmEditorDefinitions(It.IsAny<bool>()))
            .Returns(() => _definitions.ToDictionary(x => x.ContentType!.Key, x => x));
    }

    private BlockFarmEditorController Controller(string? body = null) =>
        new BlockFarmEditorController(
            _umbracoContextFactory.Object,
            _blockFarmEditorContext.Object,
            _services.BlockDefinitionService.Object,
            _services.Mapper,
            _layoutService.Object,
            _database.ToFactory().Object,
            Mock.Of<IFileService>(),
            NullLogger<BlockFarmEditorController>.Instance).WithHttpContext(body);

    #region Authorization

    [Theory]
    [InlineData(nameof(BlockFarmEditorController.GetPropertyEditors))]
    [InlineData(nameof(BlockFarmEditorController.GetLayouts))]
    public void BackofficeOnlyActions_RequireBackOfficeAccess(string action)
    {
        var attribute = Assert.Single(typeof(BlockFarmEditorController).GetMethod(action)!.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());

        Assert.Equal(AuthorizationPolicies.BackOfficeAccess, attribute.Policy);
    }

    [Theory]
    [InlineData(nameof(BlockFarmEditorController.RenderBlock))]
    [InlineData(nameof(BlockFarmEditorController.GetBlockDefinitions))]
    public void EditorActions_RequireAnAuthenticatedUser(string action)
    {
        Assert.Single(typeof(BlockFarmEditorController).GetMethod(action)!.GetCustomAttributes(typeof(AuthorizeAttribute), true));
    }

    #endregion

    #region GetPropertyEditors

    [Fact]
    public async Task GetPropertyEditors_WithoutAContentTypeKey_ReturnsAnEmptyList()
    {
        var result = Assert.IsType<JsonResult>(await Controller().GetPropertyEditors(null));

        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<string>>(result.Value));
        _services.BlockDefinitionService.Verify(x => x.RetrievePropertyEditors(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetPropertyEditors_ReturnsTheGroupsForTheContentType()
    {
        var contentTypeKey = Guid.NewGuid();
        var groups = new[] { new BlockFarmEditorPropertyGroupModel("content", "Content", PropertyGroupType.Group) };
        _services.BlockDefinitionService.Setup(x => x.RetrievePropertyEditors(contentTypeKey)).ReturnsAsync(groups);

        var result = Assert.IsType<JsonResult>(await Controller().GetPropertyEditors(contentTypeKey));

        Assert.Same(groups, result.Value);
    }

    #endregion

    #region GetBlockDefinitions

    private static Dictionary<string, List<BlockFarmEditorController.BlockFarmEditorBlockDefinition>> Blocks(IActionResult result) =>
        Assert.IsType<Dictionary<string, List<BlockFarmEditorController.BlockFarmEditorBlockDefinition>>>(Assert.IsType<JsonResult>(result).Value);

    [Fact]
    public void GetBlockDefinitions_GroupsBlocksByCategory_WithElementTypeDetails()
    {
        var heroKey = Guid.NewGuid();
        _definitions.Add(Definitions.Expanded("heroBlock", heroKey, category: "Content", name: "Hero", icon: "icon-star", description: "A hero"));
        _definitions.Add(Definitions.Expanded("cardBlock", Guid.NewGuid(), category: "Content"));
        _definitions.Add(Definitions.Expanded("mapBlock", Guid.NewGuid(), category: "Embeds"));

        var blocks = Blocks(Controller().GetBlockDefinitions(new BlockFarmEditorController.AllowedBlocksRequest(null)));

        Assert.Equal(["Content", "Embeds"], blocks.Keys);
        Assert.Equal(2, blocks["Content"].Count);
        var hero = blocks["Content"].Single(x => x.ContentTypeKey == heroKey);
        Assert.Equal("Hero", hero.Name);
        Assert.Equal("icon-star", hero.Icon);
        Assert.Equal("A hero", hero.Description);
        Assert.Equal("Content", hero.Category);
    }

    [Theory]
    [InlineData("Container")]
    [InlineData("Containers")]
    [InlineData("Cont")]
    public void GetBlockDefinitions_ListsContainerCategoriesFirst_ThenAlphabetically(string containerCategory)
    {
        _definitions.Add(Definitions.Expanded("textBlock", Guid.NewGuid(), category: "Basics"));
        _definitions.Add(Definitions.Expanded("zebraBlock", Guid.NewGuid(), category: "Zebra"));
        _definitions.Add(Definitions.Expanded("rowBlock", Guid.NewGuid(), category: containerCategory));
        _definitions.Add(Definitions.Expanded("appleBlock", Guid.NewGuid(), category: "Apple"));

        var blocks = Blocks(Controller().GetBlockDefinitions(new BlockFarmEditorController.AllowedBlocksRequest(null)));

        Assert.Equal([containerCategory, "Apple", "Basics", "Zebra"], blocks.Keys);
    }

    [Fact]
    public void GetBlockDefinitions_WithAllowedBlocks_FiltersByAlias_IgnoringCaseAndWhitespace()
    {
        _definitions.Add(Definitions.Expanded("heroBlock", Guid.NewGuid()));
        _definitions.Add(Definitions.Expanded("cardBlock", Guid.NewGuid()));
        _definitions.Add(Definitions.Expanded("mapBlock", Guid.NewGuid()));

        var blocks = Blocks(Controller().GetBlockDefinitions(new BlockFarmEditorController.AllowedBlocksRequest(" HEROBLOCK , mapBlock,, unknownBlock")));

        Assert.Equal(["heroBlock", "mapBlock"], blocks.Values.SelectMany(x => x).Select(x => x.Name).Order());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetBlockDefinitions_WithoutAnAllowList_ReturnsEverything(string? allowedBlocks)
    {
        _definitions.Add(Definitions.Expanded("heroBlock", Guid.NewGuid()));
        _definitions.Add(Definitions.Expanded("cardBlock", Guid.NewGuid()));

        var blocks = Blocks(Controller().GetBlockDefinitions(new BlockFarmEditorController.AllowedBlocksRequest(allowedBlocks)));

        Assert.Equal(2, blocks.Values.Sum(x => x.Count));
    }

    [Fact]
    public void GetBlockDefinitions_WithoutARequestBody_ReturnsEverything()
    {
        _definitions.Add(Definitions.Expanded("heroBlock", Guid.NewGuid()));

        var blocks = Blocks(Controller().GetBlockDefinitions(null!));

        Assert.Single(blocks.Values.SelectMany(x => x));
    }

    #endregion

    #region GetLayouts

    [Fact]
    public async Task GetLayouts_GroupsLayoutsByCategory_Alphabetically()
    {
        var hero = Dtos.Layout("Hero", category: "Heroes");
        var grid = Dtos.Layout("Grid", category: "Grids");
        var wideGrid = Dtos.Layout("Wide grid", category: "Grids");
        _layoutService.Setup(x => x.GetAllAsync(_database.Object)).ReturnsAsync(new[] { hero, grid, wideGrid }.ToAsyncEnumerable());

        var result = Assert.IsType<JsonResult>(await Controller().GetLayouts());

        var layouts = Assert.IsType<Dictionary<string, List<BlockFarmEditorLayoutDTO>>>(result.Value);
        Assert.Equal(["Grids", "Heroes"], layouts.Keys);
        Assert.Equal([grid, wideGrid], layouts["Grids"]);
        Assert.Equal([hero], layouts["Heroes"]);
    }

    [Fact]
    public async Task GetLayouts_WhenTheLayoutTableIsUnavailable_ReturnsAnEmptyObject()
    {
        _layoutService.Setup(x => x.GetAllAsync(_database.Object)).ReturnsAsync((IAsyncEnumerable<BlockFarmEditorLayoutDTO>?)null);

        var result = Assert.IsType<JsonResult>(await Controller().GetLayouts());

        Assert.Equal("{}", System.Text.Json.JsonSerializer.Serialize(result.Value));
    }

    #endregion

    #region RenderBlock

    private IPublishedContent AddPage(Guid key)
    {
        var contentType = new Mock<IPublishedContentType>();
        contentType.SetupGet(x => x.ItemType).Returns(PublishedItemType.Content);
        var page = new Mock<IPublishedContent>();
        page.SetupGet(x => x.Key).Returns(key);
        page.SetupGet(x => x.ContentType).Returns(contentType.Object);
        _contentCache.Setup(x => x.GetByIdAsync(key, It.IsAny<bool?>())).ReturnsAsync(page.Object);
        return page.Object;
    }

    private string BlockJson(Guid contentTypeKey, string title = "Hello") =>
        $$$"""{"unique":"{{{Guid.NewGuid()}}}","contentTypeKey":"{{{contentTypeKey}}}","properties":{"title":"{{{title}}}"},"blocks":[]}""";

    /// <summary>A block type that exists both as a published element type and as a backoffice content type.</summary>
    private Guid AddBlockType(string alias, Type? viewComponentType = null, string viewPath = "~/Views/Partials/Hero.cshtml")
    {
        var contentTypeKey = _services.AddPublishedElementType("title");
        _definitions.Add(Definitions.Expanded(alias, contentTypeKey, viewPath: viewPath, viewComponentType: viewComponentType));
        return contentTypeKey;
    }

    [Fact]
    public async Task RenderBlock_WhenThePageDoesNotExist_SaysSo()
    {
        var result = Assert.IsType<ContentResult>(await Controller("{}").RenderBlock(Guid.NewGuid(), null));

        Assert.Equal("<div>Block not found</div>", result.Content);
        _blockFarmEditorContext.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RenderBlock_PartialBlock_RendersItsViewWithTheSubmittedValues()
    {
        var pageKey = Guid.NewGuid();
        AddPage(pageKey);
        var contentTypeKey = AddBlockType("heroBlock", viewPath: "~/Views/Partials/Hero.cshtml");

        var result = Assert.IsType<PartialViewResult>(await Controller(BlockJson(contentTypeKey, "Hello")).RenderBlock(pageKey, null));

        Assert.Equal("~/Views/Partials/Hero.cshtml", result.ViewName);
        var model = Assert.IsAssignableFrom<IPublishedElement>(result.Model);
        Assert.Equal(contentTypeKey, model.ContentType.Key);
        Assert.Equal("Hello", model.Properties.Single(x => x.Alias == "title").GetSourceValue());
    }

    [Fact]
    public async Task RenderBlock_PutsThePageInEditMode_ForTheRequestedCulture()
    {
        var pageKey = Guid.NewGuid();
        var page = AddPage(pageKey);
        var contentTypeKey = AddBlockType("heroBlock");
        var controller = Controller(BlockJson(contentTypeKey));
        controller.Request.Host = new Microsoft.AspNetCore.Http.HostString("example.test", 8080);

        await controller.RenderBlock(pageKey, "da-DK");

        _blockFarmEditorContext.Verify(x => x.SetPageDefinition(_umbracoContext.Object, page, "example.test", "da-DK", true, true), Times.Once);
    }

    [Fact]
    public async Task RenderBlock_ExposesThePageAsTheCurrentPublishedRequest_SoViewsCanResolveIt()
    {
        var pageKey = Guid.NewGuid();
        var page = AddPage(pageKey);
        var contentTypeKey = AddBlockType("heroBlock");
        var controller = Controller(BlockJson(contentTypeKey));

        await controller.RenderBlock(pageKey, "da-DK");

        var publishedRequest = _umbracoContext.Object.PublishedRequest;
        Assert.NotNull(publishedRequest);
        Assert.Same(page, publishedRequest.PublishedContent);
        Assert.Equal("da-DK", publishedRequest.Culture);
        Assert.Equal(new Uri(UmbracoStaticServices.ContentUrl), publishedRequest.Uri);
        Assert.Same(publishedRequest, controller.HttpContext.Features.Get<UmbracoRouteValues>()?.PublishedRequest);
    }

    [Fact]
    public async Task RenderBlock_ViewComponentWithoutParameters_IsInvokedWithoutArguments()
    {
        var pageKey = Guid.NewGuid();
        AddPage(pageKey);
        var contentTypeKey = AddBlockType("heroBlock", typeof(NoArgumentComponent));

        var result = Assert.IsType<ViewComponentResult>(await Controller(BlockJson(contentTypeKey)).RenderBlock(pageKey, null));

        Assert.Equal(typeof(NoArgumentComponent), result.ViewComponentType);
        Assert.Null(result.Arguments);
    }

    [Fact]
    public async Task RenderBlock_ViewComponentWithOneParameter_ReceivesTheElementUnderThatParametersName()
    {
        var pageKey = Guid.NewGuid();
        AddPage(pageKey);
        var contentTypeKey = AddBlockType("heroBlock", typeof(SingleArgumentComponent));

        var result = Assert.IsType<ViewComponentResult>(await Controller(BlockJson(contentTypeKey)).RenderBlock(pageKey, null));

        Assert.Equal(typeof(SingleArgumentComponent), result.ViewComponentType);
        var arguments = Assert.IsType<Dictionary<string, object?>>(result.Arguments);
        Assert.IsAssignableFrom<IPublishedElement>(Assert.Single(arguments, x => x.Key == "heroModel").Value);
    }

    [Fact]
    public async Task RenderBlock_ViewComponentWithSeveralParameters_ReceivesTheElementItselfAsArguments()
    {
        var pageKey = Guid.NewGuid();
        AddPage(pageKey);
        var contentTypeKey = AddBlockType("heroBlock", typeof(MultiArgumentComponent));

        var result = Assert.IsType<ViewComponentResult>(await Controller(BlockJson(contentTypeKey)).RenderBlock(pageKey, null));

        Assert.IsAssignableFrom<IPublishedElement>(result.Arguments);
    }

    [Fact]
    public async Task RenderBlock_BlockWithoutADefinition_ExplainsThatItWillBeHidden()
    {
        var pageKey = Guid.NewGuid();
        AddPage(pageKey);
        var contentTypeKey = _services.AddPublishedElementType("title");

        var result = Assert.IsType<ContentResult>(await Controller(BlockJson(contentTypeKey)).RenderBlock(pageKey, null));

        Assert.Equal($"Block type {contentTypeKey} not found.  This will be hidden in the live view.", result.Content);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{\"blocks\":[]}")]
    public async Task RenderBlock_WhenTheBodyIsNotABlock_SaysSo(string body)
    {
        var pageKey = Guid.NewGuid();
        AddPage(pageKey);

        var result = Assert.IsType<ContentResult>(await Controller(body).RenderBlock(pageKey, null));

        Assert.Equal("<div>Block definition not found in request body</div>", result.Content);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    public async Task RenderBlock_WhenTheBodyIsNotJson_ReturnsAGenericErrorInsteadOfThrowing(string body)
    {
        var pageKey = Guid.NewGuid();
        AddPage(pageKey);

        var result = Assert.IsType<ContentResult>(await Controller(body).RenderBlock(pageKey, null));

        Assert.Equal("<div>An error occurred rendering the block</div>", result.Content);
    }

    [Fact]
    public async Task RenderBlock_WhenUmbracoFails_ReturnsAGenericErrorInsteadOfThrowing()
    {
        _umbracoContextFactory.Setup(x => x.EnsureUmbracoContext()).Throws(new InvalidOperationException("no context"));

        var result = Assert.IsType<ContentResult>(await Controller("{}").RenderBlock(Guid.NewGuid(), null));

        Assert.Equal("<div>An error occurred rendering the block</div>", result.Content);
    }

    #endregion
}
