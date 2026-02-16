using BlockFarmEditor.Umbraco.Controllers;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Library.Services;
using BlockFarmEditor.Umbraco.Library.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace BlockFarmEditor.Umbraco
{
    public static class BlockFarmEditorRegister
    {
        public static IUmbracoBuilder AddBlockFarmEditor(this IUmbracoBuilder builder)
        {
            builder.Services.AddSingleton<IBlockDefinitionService, BlockDefinitionService>();
            builder.Services.AddSingleton<IBlockFarmEditorDefinitionService, BlockFarmEditorDefinitionService>();
            builder.Services.AddSingleton<IBlockFarmEditorLayoutService, BlockFarmEditorLayoutService>();
            builder.Services.AddScoped<IBlockFarmEditorRenderService, BlockFarmEditorRenderService>();
            builder.Services.AddScoped<IBlockFarmEditorContext, BlockFarmEditorContext>();

            builder.AddNotificationAsyncHandler<ContentTypeDeletedNotification, BlockFarmEditorDefinitionCleanUp>();

            builder.AddNotificationAsyncHandler<ContentTypeChangedNotification, BlockFarmEditorDefinitionRefresh>();

            builder.Services.Configure<UmbracoPipelineOptions>(options =>
            {

                options.AddFilter(new UmbracoPipelineFilter(nameof(BlockFarmEditorController))
                {
                    Endpoints = app => app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllerRoute(
                            "Block Farm Controller",
                            "/umbraco/blockfarmeditor/{action}/{id?}",
                            new { controller = "BlockFarmEditor", action = "RenderBlock" });

                        endpoints.MapControllerRoute(
                            "Block Farm Controller Definitions",
                            "/umbraco/blockfarmeditor/definitions/{action}/{id?}",
                            new { controller = "BlockFarmEditorDefinition", action = "Index" });

                        endpoints.MapControllerRoute(
                            "Block Farm Controller Layout",
                            "/umbraco/blockfarmeditor/layouts/{action}/{id?}",
                            new { controller = "BlockFarmEditorLayout", action = "Index" });
                    }),

                    PostPipeline = app =>
                    {
                        app.UseMiddleware<BlockFarmEditorContextMiddleware>();
                    }
                });
            });
            return builder;
        }
    }
    internal class BlockFarmEditorContextMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            var blockFarmEditorContext = context.RequestServices.GetRequiredService<IBlockFarmEditorContext>();
            await blockFarmEditorContext.SetPageDefinition();
            await next(context);
        }
    }
}
