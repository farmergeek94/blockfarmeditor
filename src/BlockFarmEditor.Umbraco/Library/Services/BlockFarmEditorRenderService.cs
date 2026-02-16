using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace BlockFarmEditor.Umbraco.Library.Services
{
    internal class BlockFarmEditorRenderService(IBlockDefinitionService serializerService, ILogger<BlockFarmEditorRenderService> logger) : IBlockFarmEditorRenderService
    {
        public async Task<IHtmlContent?> RenderComponent<T>(IHtmlHelper htmlHelper, BlockDefinition<T> element) where T : IPublishedElement
        {
            var definitions = serializerService.RetrieveBlockFarmEditorDefinitions();

            IHtmlContent? renderedComponent = null;
            try
            {
                if (definitions.TryGetValue(element.ContentTypeKey!.Value, out var definition))
                {
                    if (definition.DefinitionAttribute?.ViewComponentType != null)
                    {
                        var viewComponentHelper = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<IViewComponentHelper>();

                        (viewComponentHelper as IViewContextAware)?.Contextualize(htmlHelper.ViewContext);

                        if (viewComponentHelper == null)
                        {
                            logger.LogError("ViewComponentHelper is null. Ensure that the service is registered correctly.");
                            return null;
                        }

                        // Grab the method to invoke
                        var invokeMethod = definition.DefinitionAttribute.ViewComponentType.GetMethod("InvokeAsync") ?? definition.DefinitionAttribute.ViewComponentType.GetMethod("Invoke");

                        // grab the parameters of the method
                        var parameters = invokeMethod?.GetParameters() ?? [];

                        if (parameters.Length == 1)
                        {
                            // If exactly one parameter, assume it's a model object
                            var paramName = parameters[0].Name ?? "properties";
                            // Create a dictionary to wrap the properties and pass in
                            var wrappedArgs = new Dictionary<string, object?>
                            {
                                [paramName] = element.Properties
                            };
                            renderedComponent = await viewComponentHelper.InvokeAsync(definition.DefinitionAttribute.ViewComponentType, wrappedArgs);
                        }
                        else if (parameters.Length > 1)
                        {
                            // Multiple parameters: assume properties match parameter names
                            renderedComponent = await viewComponentHelper.InvokeAsync(definition.DefinitionAttribute.ViewComponentType, element.Properties);
                        }
                        else
                        {
                            // No parameters: just invoke the component
                            renderedComponent = await viewComponentHelper.InvokeAsync(definition.DefinitionAttribute.ViewComponentType);
                        }
                    }
                    else if (definition.ViewPath != null)
                    {
                        // Render the view directly
                        renderedComponent = await htmlHelper.PartialAsync(definition.ViewPath, element.Properties);
                    }

                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error rendering component for block type {BlockType}", element.ContentTypeKey);
                
                var div = new TagBuilder("div");
                div.AddCssClass("block-render-error");
                div.Attributes["style"] = "display:none;";
                div.InnerHtml.Append($"Error rendering block of type {element.ContentTypeKey}: {ex.Message}");

                renderedComponent = div;
            }
            return renderedComponent;
        }
    }
}
