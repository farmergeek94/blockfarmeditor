using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using BlockFarmEditor.Umbraco.Library.Services;
using BlockFarmEditor.Umbraco.Tests.Helpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace BlockFarmEditor.Umbraco.Tests.Services;

public class BlockFarmEditorRenderServiceTests
{
    // Only the method signatures matter - the service inspects them via reflection.
    private class NoArgumentComponent { public string Invoke() => string.Empty; }
    private class SingleArgumentComponent { public Task<string> InvokeAsync(IPublishedElement heroModel) => Task.FromResult(string.Empty); }
    private class MultiArgumentComponent { public string Invoke(string title, int count) => string.Empty; }
    private class NoInvokeMethodComponent { }

    private static readonly Guid ContentTypeKey = Guid.NewGuid();
    private static readonly IHtmlContent Rendered = new HtmlString("<p>rendered</p>");

    private readonly Mock<IViewComponentHelper> _viewComponentHelper = new();
    private readonly Mock<IHtmlHelper> _htmlHelper = new();
    private readonly ViewContext _viewContext = new();

    private void UseServices(Action<ServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        configure?.Invoke(services);
        _viewContext.HttpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        _htmlHelper.SetupGet(x => x.ViewContext).Returns(_viewContext);
    }

    private void UseViewComponentHelper() => UseServices(services => services.AddSingleton(_viewComponentHelper.Object));

    private BlockFarmEditorRenderService Service(Type? viewComponentType = null, string viewPath = "~/Views/Partials/Hero.cshtml") =>
        new(Definitions.ToService(Definitions.Expanded("heroBlock", ContentTypeKey, viewPath: viewPath, viewComponentType: viewComponentType)).Object,
            NullLogger<BlockFarmEditorRenderService>.Instance);

    [Fact]
    public async Task UnknownContentType_RendersNothing()
    {
        UseViewComponentHelper();

        var result = await Service().RenderComponent(_htmlHelper.Object, Definitions.Block(Guid.NewGuid()));

        Assert.Null(result);
    }

    [Fact]
    public async Task PartialDefinition_RendersThePartialWithTheElementAsModel()
    {
        UseViewComponentHelper();
        var block = Definitions.Block(ContentTypeKey);
        _htmlHelper.Setup(x => x.PartialAsync("~/Views/Partials/Hero.cshtml", block.Properties, null)).ReturnsAsync(Rendered);

        var result = await Service().RenderComponent(_htmlHelper.Object, block);

        Assert.Same(Rendered, result);
        _viewComponentHelper.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ViewComponentWithoutParameters_IsInvokedWithoutArguments()
    {
        UseViewComponentHelper();
        _viewComponentHelper.Setup(x => x.InvokeAsync(typeof(NoArgumentComponent), null)).ReturnsAsync(Rendered);

        var result = await Service(typeof(NoArgumentComponent)).RenderComponent(_htmlHelper.Object, Definitions.Block(ContentTypeKey));

        Assert.Same(Rendered, result);
    }

    [Fact]
    public async Task ViewComponentWithoutAnInvokeMethod_IsInvokedWithoutArguments()
    {
        UseViewComponentHelper();
        _viewComponentHelper.Setup(x => x.InvokeAsync(typeof(NoInvokeMethodComponent), null)).ReturnsAsync(Rendered);

        var result = await Service(typeof(NoInvokeMethodComponent)).RenderComponent(_htmlHelper.Object, Definitions.Block(ContentTypeKey));

        Assert.Same(Rendered, result);
    }

    [Fact]
    public async Task ViewComponentWithOneParameter_ReceivesTheElementUnderThatParametersName()
    {
        UseViewComponentHelper();
        var block = Definitions.Block(ContentTypeKey);
        object? arguments = null;
        _viewComponentHelper
            .Setup(x => x.InvokeAsync(typeof(SingleArgumentComponent), It.IsAny<object?>()))
            .Callback((Type _, object? args) => arguments = args)
            .ReturnsAsync(Rendered);

        var result = await Service(typeof(SingleArgumentComponent)).RenderComponent(_htmlHelper.Object, block);

        Assert.Same(Rendered, result);
        var wrapped = Assert.IsType<Dictionary<string, object?>>(arguments);
        Assert.Same(block.Properties, Assert.Single(wrapped, x => x.Key == "heroModel").Value);
    }

    [Fact]
    public async Task ViewComponentWithSeveralParameters_ReceivesTheElementItselfAsArguments()
    {
        UseViewComponentHelper();
        var block = Definitions.Block(ContentTypeKey);
        _viewComponentHelper.Setup(x => x.InvokeAsync(typeof(MultiArgumentComponent), block.Properties)).ReturnsAsync(Rendered);

        var result = await Service(typeof(MultiArgumentComponent)).RenderComponent(_htmlHelper.Object, block);

        Assert.Same(Rendered, result);
    }

    [Fact]
    public async Task ViewComponent_TakesPrecedenceOverTheViewPath()
    {
        UseViewComponentHelper();
        _viewComponentHelper.Setup(x => x.InvokeAsync(typeof(NoArgumentComponent), null)).ReturnsAsync(Rendered);

        await Service(typeof(NoArgumentComponent), viewPath: "~/Views/Partials/Hero.cshtml").RenderComponent(_htmlHelper.Object, Definitions.Block(ContentTypeKey));

        _htmlHelper.Verify(x => x.PartialAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<ViewDataDictionary>()), Times.Never);
    }

    [Fact]
    public async Task ViewComponentHelper_IsContextualizedWithTheCurrentViewContext()
    {
        var contextAware = _viewComponentHelper.As<IViewContextAware>();
        UseViewComponentHelper();

        await Service(typeof(NoArgumentComponent)).RenderComponent(_htmlHelper.Object, Definitions.Block(ContentTypeKey));

        contextAware.Verify(x => x.Contextualize(_viewContext), Times.Once);
    }

    [Fact]
    public async Task ViewComponent_WhenNoHelperIsRegistered_RendersNothing()
    {
        UseServices();

        var result = await Service(typeof(NoArgumentComponent)).RenderComponent(_htmlHelper.Object, Definitions.Block(ContentTypeKey));

        Assert.Null(result);
    }

    [Fact]
    public async Task WhenRenderingThrows_ReturnsAHiddenErrorPlaceholder()
    {
        UseViewComponentHelper();
        _htmlHelper
            .Setup(x => x.PartialAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<ViewDataDictionary>()))
            .ThrowsAsync(new InvalidOperationException("view <exploded>"));

        var result = await Service().RenderComponent(_htmlHelper.Object, Definitions.Block(ContentTypeKey));

        Assert.NotNull(result);
        var html = result.Render();
        Assert.Contains("class=\"block-render-error\"", html);
        Assert.Contains("display:none;", html);
        Assert.Contains(ContentTypeKey.ToString(), html);
        Assert.Contains("view &lt;exploded&gt;", html);
        Assert.DoesNotContain("<exploded>", html);
    }

    [Fact]
    public async Task BlockWithoutProperties_ReturnsTheErrorPlaceholder_InsteadOfThrowing()
    {
        UseViewComponentHelper();

        var result = await Service().RenderComponent(_htmlHelper.Object, new BlockDefinition<IPublishedElement>());

        Assert.NotNull(result);
        Assert.Contains("block-render-error", result.Render());
    }
}
