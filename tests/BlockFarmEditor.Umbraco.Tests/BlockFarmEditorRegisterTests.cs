using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Library.Services;
using BlockFarmEditor.Umbraco.Library.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace BlockFarmEditor.Umbraco.Tests;

public class BlockFarmEditorRegisterTests
{
    private readonly ServiceCollection _services = new();
    private readonly IUmbracoBuilder _builder;

    public BlockFarmEditorRegisterTests()
    {
        var builder = new Mock<IUmbracoBuilder>();
        builder.SetupGet(x => x.Services).Returns(_services);
        _builder = builder.Object;
    }

    [Fact]
    public void AddBlockFarmEditor_ReturnsTheBuilder_ForChaining()
    {
        Assert.Same(_builder, _builder.AddBlockFarmEditor());
    }

    [Theory]
    [InlineData(typeof(IBlockDefinitionService), typeof(BlockDefinitionService), ServiceLifetime.Singleton)]
    [InlineData(typeof(IBlockFarmEditorDefinitionService), typeof(BlockFarmEditorDefinitionService), ServiceLifetime.Singleton)]
    [InlineData(typeof(IBlockFarmEditorLayoutService), typeof(BlockFarmEditorLayoutService), ServiceLifetime.Singleton)]
    [InlineData(typeof(IBlockFarmEditorRenderService), typeof(BlockFarmEditorRenderService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IBlockFarmEditorContext), typeof(BlockFarmEditorContext), ServiceLifetime.Scoped)]
    [InlineData(typeof(IBlockFarmEditorExportService), typeof(BlockFarmEditorExportService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IFolderStructureService), typeof(FolderStructureService), ServiceLifetime.Scoped)]
    public void AddBlockFarmEditor_RegistersEachService_WithTheExpectedLifetime(Type service, Type implementation, ServiceLifetime lifetime)
    {
        _builder.AddBlockFarmEditor();

        var descriptor = Assert.Single(_services, x => x.ServiceType == service);
        Assert.Equal(implementation, descriptor.ImplementationType);
        Assert.Equal(lifetime, descriptor.Lifetime);
    }

    [Fact]
    public void AddBlockFarmEditor_TheRequestScopedContext_IsNotASingleton()
    {
        // the context holds per-request state (edit mode, current page); sharing it would leak between visitors
        _builder.AddBlockFarmEditor();

        Assert.NotEqual(ServiceLifetime.Singleton, _services.Single(x => x.ServiceType == typeof(IBlockFarmEditorContext)).Lifetime);
    }

    [Theory]
    [InlineData(typeof(INotificationAsyncHandler<ContentTypeDeletedNotification>), typeof(BlockFarmEditorDefinitionCleanUp))]
    [InlineData(typeof(INotificationAsyncHandler<ContentTypeChangedNotification>), typeof(BlockFarmEditorDefinitionRefresh))]
    public void AddBlockFarmEditor_SubscribesToContentTypeNotifications(Type handler, Type implementation)
    {
        _builder.AddBlockFarmEditor();

        Assert.Contains(_services, x => x.ServiceType == handler && x.ImplementationType == implementation);
    }

    [Fact]
    public void AddBlockFarmEditor_AddsItsPipelineFilter_WithEndpointsAndTheContextMiddleware()
    {
        _builder.AddBlockFarmEditor();

        var options = _services.BuildServiceProvider().GetRequiredService<IOptions<UmbracoPipelineOptions>>().Value;

        var filter = Assert.IsType<UmbracoPipelineFilter>(Assert.Single(options.PipelineFilters, x => x is UmbracoPipelineFilter { Name: "BlockFarmEditorController" }));
        Assert.NotNull(filter.Endpoints);
        Assert.NotNull(filter.PostPipeline);
    }

    [Theory]
    [InlineData("/umbraco/blockfarmeditor/{action}/{id?}", "BlockFarmEditor", "RenderBlock")]
    [InlineData("/umbraco/blockfarmeditor/definitions/{action}/{id?}", "BlockFarmEditorDefinition", "Index")]
    [InlineData("/umbraco/blockfarmeditor/layouts/{action}/{id?}", "BlockFarmEditorLayout", "Index")]
    public void PipelineFilter_MapsTheRoutesTheBackofficeClientCalls(string pattern, string controller, string defaultAction)
    {
        _builder.AddBlockFarmEditor();
        var filter = _services.BuildServiceProvider().GetRequiredService<IOptions<UmbracoPipelineOptions>>().Value
            .PipelineFilters.OfType<UmbracoPipelineFilter>().Single(x => x.Name == "BlockFarmEditorController");

        var appBuilder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory });
        appBuilder.Services.AddControllers().AddApplicationPart(typeof(BlockFarmEditorRegister).Assembly);
        using var app = appBuilder.Build();
        app.UseRouting();

        filter.OnEndpoints(app);

        var routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(x => x.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(x => Equals(x.RoutePattern.RequiredValues["controller"], controller))
            .Select(x => x.RoutePattern)
            .ToList();

        var route = Assert.Single(routes.DistinctBy(x => x.RawText));
        Assert.Equal(pattern.TrimStart('/'), route.RawText!.TrimStart('/'));
        Assert.Equal(defaultAction, route.Defaults["action"]);
    }

    [Fact]
    public async Task ContextMiddleware_LoadsThePageDefinition_BeforeTheRestOfThePipelineRuns()
    {
        var calls = new List<string>();
        var context = new Mock<IBlockFarmEditorContext>();
        context.Setup(x => x.SetPageDefinition()).Callback(() => calls.Add("SetPageDefinition")).Returns(Task.CompletedTask);
        var httpContext = new DefaultHttpContext { RequestServices = new ServiceCollection().AddSingleton(context.Object).BuildServiceProvider() };
        var middleware = new BlockFarmEditorContextMiddleware(_ =>
        {
            calls.Add("next");
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(["SetPageDefinition", "next"], calls);
    }
}
