using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Authorization;

namespace BlockFarmEditor.Umbraco.Controllers
{
    public class BlockFarmEditorController(IUmbracoContextFactory umbracoContextFactory
        , IBlockFarmEditorContext blockFarmEditorContext
        , IBlockDefinitionService blockDefinitionService
        , IBlockFarmEditorLayoutService blockFarmEditorLayoutService
        , IUmbracoDatabaseFactory umbracoDatabaseFactory) : Controller
    {
        [Authorize]
        public async Task<IActionResult> RenderBlock([FromRoute] Guid id, [FromQuery] string? culture)
        {
            try
            {
                using var reader = new StreamReader(Request.Body, encoding: Encoding.UTF8, leaveOpen: true);
                string bodyString = await reader.ReadToEndAsync();

                using var context = umbracoContextFactory.EnsureUmbracoContext();
                var content = await context.UmbracoContext.Content.GetByIdAsync(id, true);
                if (content != null)
                {
                    await blockFarmEditorContext.SetPageDefinition(context.UmbracoContext, content, Request.Host.Host, culture: culture, true, true);

                    var jsonNode = JsonSerializer.Deserialize<JsonNode>(bodyString, blockDefinitionService.JsonSerializerReaderOptions);
                    if (jsonNode != null)
                    {
                        var jsonNodeSeriliazed = JsonSerializer.Serialize(jsonNode, blockDefinitionService.JsonSerializerWriterOptions);

                        BlockDefinition<IPublishedElement>? blockDefinition = JsonSerializer.Deserialize<BlockDefinition<IPublishedElement>>(jsonNodeSeriliazed, blockDefinitionService.JsonSerializerReaderOptions);
                        if (blockDefinition != null)
                        {
                            return RenderBlock(blockDefinition);

                        }
                    }
                    return Content("<div>Block definition not found in request body</div>");
                }
                return Content("<div>Block not found</div>");

            }
            catch (Exception ex)
            {
                return Content($"<div>Error: {ex.Message}<div>");
            }
        }

        private IActionResult RenderBlock<T>(BlockDefinition<T> element) where T : IPublishedElement
        {
            IActionResult? renderedComponent = Content($"Block type {element.ContentTypeKey} not found.  This will be hidden in the live view.");
            if (element.ContentTypeKey.HasValue)
            {
                var definitions = blockDefinitionService.RetrieveBlockFarmEditorDefinitions();
                var definition = definitions.TryGetValue(element.ContentTypeKey.Value, out var attr) ? attr : null;

                if (definition != null)
                {
                    if (definition.DefinitionAttribute?.ViewComponentType != null)
                    {
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
                            renderedComponent = ViewComponent(definition.DefinitionAttribute.ViewComponentType, wrappedArgs);
                        }
                        else if (parameters.Length > 1)
                        {
                            // Multiple parameters: assume properties match parameter names
                            renderedComponent = ViewComponent(definition.DefinitionAttribute.ViewComponentType, element.Properties);
                        }
                        else
                        {
                            // No parameters: just invoke the component
                            renderedComponent = ViewComponent(definition.DefinitionAttribute.ViewComponentType);
                        }
                    }
                    else if (definition.ViewPath != null)
                    {
                        // Render the view directly
                        renderedComponent = PartialView(definition.ViewPath, element.Properties);
                    }

                }
            }
            return renderedComponent;
        }

        [Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
        public async Task<IActionResult> GetPropertyEditors([FromQuery] Guid? contentTypeKey)
        {
            if (contentTypeKey.HasValue)
            {
                var propertyEditors = await blockDefinitionService.RetrievePropertyEditors(contentTypeKey.Value);
                return Json(propertyEditors);
            }
            return Json(Enumerable.Empty<string>());
        }

        public record AllowedBlocksRequest(string? AllowedBlocks);

        [HttpGet]
        [HttpPost]
        [Authorize]
        public IActionResult GetBlockDefinitions([FromBody] AllowedBlocksRequest allowedBlocksRequest)
        {
            var definitions = blockDefinitionService.RetrieveBlockFarmEditorDefinitions().Values.AsEnumerable();

            if (!string.IsNullOrEmpty(allowedBlocksRequest?.AllowedBlocks))
            {
                var allowedBlockList = allowedBlocksRequest.AllowedBlocks.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
                definitions = definitions.Where(x => allowedBlockList.Contains(x.ContentTypeAlias, StringComparer.OrdinalIgnoreCase));
            }

            var results = definitions.Select(x => new BlockFarmEditorBlockDefinition
            {
                ContentTypeKey = x.ContentType?.Key,
                Name = x.ContentType?.Name,
                Description = x.ContentType?.Description,
                Icon = x.ContentType?.Icon,
                Category = x.Category
            });

            return Json(results.GroupBy(x => x.Category).OrderBy(x => x.Key == "Container" || x.Key == "Containers" || x.Key == "Cont" ? 0 : 1).ThenBy(x => x.Key).ToDictionary(x => x.Key, x => x.ToList()));
        }

        [HttpGet]
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
        public async Task<IActionResult> GetLayouts()
        {
            using var db = umbracoDatabaseFactory.CreateDatabase();
            var layouts = await blockFarmEditorLayoutService.GetAllAsync(db);

            if (layouts == null)
            {
                return Json(new { });
            }

            return Json(await layouts.GroupBy(x => x.Category).OrderBy(x => x.Key).ToDictionaryAsync(x => x.Key, x => x.ToList()));
        }

        public class BlockFarmEditorBlockDefinition
        {
            public string? Name { get; set; }

            public string? Icon { get; set; }

            public string Category { get; set; } = string.Empty;
            public Guid? ContentTypeKey { get; set; }
            public string? Description { get; set; }
        }
    }
}
