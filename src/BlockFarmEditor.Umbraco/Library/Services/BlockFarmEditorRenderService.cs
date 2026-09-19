using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace BlockFarmEditor.Umbraco.Library.Services
{
    internal class BlockFarmEditorRenderService(IBlockDefinitionService serializerService, IBlockFarmEditorContext blockFarmEditorContext, ILogger<BlockFarmEditorRenderService> logger) : IBlockFarmEditorRenderService
    {
        public async Task<IHtmlContent?> RenderComponent<T>(IHtmlHelper htmlHelper, BlockDefinition<T> element) where T : IPublishedElement
        {
            IHtmlContent? renderedComponent = null;
            try
            {
                var definitions = serializerService.RetrieveBlockFarmEditorDefinitions();

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
                var message = GetErrorMessage(ex);
                logger.LogError(ex, "Error rendering component for block type {BlockType}: {ErrorMessage}", element.ContentTypeKey, message);

                var div = new TagBuilder("div");
                div.AddCssClass("alert alert-danger");
                div.AddCssClass("m-2");
                if (blockFarmEditorContext.IsPreview)
                {
                    // Preserve the line breaks of multi-line (compilation) errors
                    div.Attributes["style"] = "white-space:pre-wrap;";                
                    div.InnerHtml.Append($"Error rendering block of type {element.ContentTypeKey}: {message}");
                }
                else
                {
                    // Never show error details on the public facing site
                    div.Attributes["style"] = "display:none;";
                    div.InnerHtml.Append($"Error rendering block of type {element.ContentTypeKey}");
                }

                renderedComponent = div;
            }
            return renderedComponent;
        }

        /// <summary>
        /// Builds the error message, expanding the individual compiler diagnostics for compilation exceptions
        /// (e.g. UmbracoCompilationException), whose own message doesn't say what actually failed.
        /// </summary>
        private static string GetErrorMessage(Exception ex)
        {
            // UmbracoCompilationException lives in Umbraco.Cms.DevelopmentMode.Backoffice, so match on the interface it implements instead.
            ICompilationException? compilationException = null;
            for (var current = ex; current != null && compilationException == null; current = current.InnerException)
            {
                compilationException = current as ICompilationException;
            }

            if (compilationException?.CompilationFailures == null)
            {
                return ex.Message;
            }

            var builder = new StringBuilder(ex.Message);
            foreach (var failure in compilationException.CompilationFailures)
            {
                if (failure == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(failure.FailureSummary))
                {
                    builder.AppendLine().Append(failure.FailureSummary);
                }

                foreach (var diagnostic in failure.Messages ?? [])
                {
                    if (diagnostic == null)
                    {
                        continue;
                    }

                    builder.AppendLine().Append(diagnostic.FormattedMessage ?? $"{diagnostic.SourceFilePath ?? failure.SourceFilePath}({diagnostic.StartLine},{diagnostic.StartColumn}): {diagnostic.Message}");
                }
            }
            return builder.ToString();
        }
    }
}
