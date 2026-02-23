using BlockFarmEditor.Umbraco.Core.DTO;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Authorization;

namespace BlockFarmEditor.Umbraco.Controllers
{
    [Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
    public class BlockFarmEditorDefinitionController(
        IBlockFarmEditorDefinitionService definitionService,
        ILogger<BlockFarmEditorDefinitionController> logger,
        IUserService userService,
        IUmbracoDatabaseFactory umbracoDatabaseFactory,
        IBlockDefinitionService blockDefinitionService,
        IBlockFarmEditorExportService exportService) : Controller
    {

        /// <summary>
        /// Get a specific BlockFarmEditor definition by ContentTypeAlias
        /// </summary>
        /// <param name="alias">The content type alias</param>
        /// <returns>The definition if found</returns>
        [HttpGet]
        public async Task<IActionResult> Index(string alias)
        {
            try
            {
                using var umbracoDatabase = umbracoDatabaseFactory.CreateDatabase();
                var result = await definitionService.GetByAliasAsync(umbracoDatabase, alias);
                if (result == null)
                {
                    return Ok(new { });
                }

                return Ok(new { data = result });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving BlockFarmEditor definition with alias {Alias}", alias);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Export comprehensive package with definitions, element types, data types, and partial views.
        /// </summary>
        /// <param name="request">Export options including definition keys and download flag</param>
        /// <returns>OK or ZIP file depending on download flag</returns>
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
        public async Task<IActionResult> ExportPackage([FromBody] ExportPackageRequest request)
        {
            try
            {
                var definitionKeys = request.DefinitionKeys?.Select(Guid.Parse).ToList() ?? [];
                var package = await exportService.BuildExportPackageAsync(definitionKeys);

                // Always write to folder
                await exportService.ExportToFolderAsync(package);

                if (request.Download)
                {
                    var zipBytes = await exportService.ExportToZipAsync(package);
                    return File(zipBytes, "application/zip", $"blockfarmeditor-export-{DateTime.UtcNow:yyyyMMddHHmmss}.zip");
                }

                return Ok(new { message = "Package exported to folder successfully", definitionCount = package.Definitions.Count });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error exporting package");
                return StatusCode(500, "Failed to export package");
            }
        }

        /// <summary>
        /// Import comprehensive package from ZIP file or server folder.
        /// </summary>
        /// <param name="file">Optional ZIP file to import</param>
        /// <param name="overwriteElementTypes">Whether to overwrite existing element types</param>
        /// <param name="overwriteBlockDefinitions">Whether to overwrite existing block definitions</param>
        /// <param name="overwritePartialViews">Whether to overwrite existing partial views</param>
        /// <param name="overwriteDataTypes">Whether to overwrite existing data types</param>
        /// <returns>Import result</returns>
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
        public async Task<IActionResult> ImportPackage(
            IFormFile? file, 
            [FromQuery] bool overwriteElementTypes = true,
            [FromQuery] bool overwriteBlockDefinitions = true,
            [FromQuery] bool overwritePartialViews = true,
            [FromQuery] bool overwriteDataTypes = false)
        {
            try
            {
                BlockFarmEditorExportPackageDTO package;

                if (file != null && file.Length > 0)
                {
                    // Import from uploaded ZIP
                    using var stream = file.OpenReadStream();
                    package = await exportService.ReadFromZipAsync(stream);
                }
                else
                {
                    // Import from server folder
                    package = await exportService.ReadFromFolderAsync();
                }

                var importResult = await exportService.ImportPackageAsync(package, overwriteElementTypes, overwriteBlockDefinitions, overwritePartialViews, overwriteDataTypes);
                blockDefinitionService.ClearCache();

                return Ok(new
                {
                    message = "Package imported successfully",
                    definitions = importResult.Definitions.Imported,
                    elementTypes = importResult.ElementTypes.Imported,
                    dataTypes = importResult.DataTypes.Imported,
                    partialViews = importResult.PartialViews.Imported
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error importing package");
                return StatusCode(500, "Failed to import package");
            }
        }

        /// <summary>
        /// Get all definitions available for export selection.
        /// </summary>
        /// <returns>List of definitions with key, alias, category</returns>
        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
        public async Task<IActionResult> Exportable()
        {
            try
            {
                using var db = umbracoDatabaseFactory.CreateDatabase();
                var definitions = blockDefinitionService.RetrieveBlockFarmEditorDefinitions().Values.AsEnumerable();

                var result = new List<object>();

                foreach (var def in definitions)
                {
                    if (def != null)
                    {
                        result.Add(new
                        {
                            key = def.Key.ToString(),
                            name = def.ContentType?.Name,
                            alias = def.ContentTypeAlias,
                            category = def.Category,
                            enabled = def.Enabled,
                            icon = def.ContentType?.Icon,
                            description = def.ContentType?.Description
                        });
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving exportable definitions");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            try
            {
                using var umbracoDatabase = umbracoDatabaseFactory.CreateDatabase();

                var result = await definitionService.GetCategories(umbracoDatabase);
                if (result == null)
                {
                    return Ok(new { });
                }

                return Ok(new { data = result });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving BlockFarmEditor categories");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Create a new BlockFarmEditor definition
        /// </summary>
        /// <param name="request">The definition to create</param>
        /// <returns>The created definition</returns>
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
        public async Task<IActionResult> Create([FromBody] CreateBlockFarmEditorDefinitionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                using var umbracoDatabase = umbracoDatabaseFactory.CreateDatabase();

                // Check if definition already exists
                var exists = await definitionService.GetByAliasAsync(umbracoDatabase, request.ContentTypeAlias);
                if (exists != null)
                {
                    return Conflict($"Definition with ContentTypeAlias '{request.ContentTypeAlias}' already exists");
                }

                // Get current user ID for audit trail
                var currentUser = await GetCurrentUserIdAsync();

                var definition = new BlockFarmEditorDefinitionDTO
                {
                    Key = Guid.NewGuid(),
                    ContentTypeAlias = request.ContentTypeAlias,
                    Type = request.Type,
                    ViewPath = request.ViewPath,
                    Category = request.Category,
                    Enabled = request.Enabled,
                    CreatedBy = currentUser,
                    UpdatedBy = currentUser
                };

                var result = await definitionService.CreateAsync(umbracoDatabase, definition, currentUser);

                blockDefinitionService.ClearCache();

                return CreatedAtAction(nameof(Index), new { alias = result.ContentTypeAlias }, new { data = result });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating BlockFarmEditor definition");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Update an existing BlockFarmEditor definition
        /// </summary>
        /// <param name="id">The definition ID to update</param>
        /// <param name="request">The updated definition data</param>
        /// <returns>The updated definition</returns>
        [HttpPut]
        [Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateBlockFarmEditorDefinitionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Get current user ID for audit trail
                var currentUser = await GetCurrentUserIdAsync();

                using var umbracoDatabase = umbracoDatabaseFactory.CreateDatabase();
                var result = await definitionService.UpdateAsync(umbracoDatabase, id, request.Type, request.ViewPath, request.Category, request.Enabled, currentUser);
                if (result == null)
                {
                    return NotFound($"Definition with ID {id} not found");
                }
                blockDefinitionService.ClearCache();
                return Ok(new { data = result });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating BlockFarmEditor definition with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get the current user's ID for audit purposes
        /// </summary>
        /// <returns>Current user's ID or empty Guid if not found</returns>
        private Task<Guid> GetCurrentUserIdAsync()
        {
            try
            {
                if (User?.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(User.Identity.Name))
                {
                    var user = userService.GetByUsername(User.Identity.Name);
                    if (user == null)
                    {
                        user = userService.GetUserById(-1);
                    }
                    if (user != null)
                    {
                        return Task.FromResult(user.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not retrieve current user ID");
            }
            
            return Task.FromResult(Guid.Empty);
        }
    }

    /// <summary>
    /// Request model for creating a new BlockFarmEditor definition
    /// </summary>
    public class CreateBlockFarmEditorDefinitionRequest
    {
        public required string ContentTypeAlias { get; set; }
        public required string Type { get; set; }
        public required string ViewPath { get; set; }
        public required string Category { get; set; }
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Request model for updating an existing BlockFarmEditor definition
    /// </summary>
    public class UpdateBlockFarmEditorDefinitionRequest
    {
        public required string Type { get; set; }
        public required string ViewPath { get; set; }
        public required string Category { get; set; }
        public bool Enabled { get; set; }
    }

    /// <summary>
    /// Request model for exporting a package
    /// </summary>
    public class ExportPackageRequest
    {
        /// <summary>
        /// Definition keys to export. If empty or null, exports all definitions.
        /// </summary>
        public List<string>? DefinitionKeys { get; set; }

        /// <summary>
        /// Whether to return a ZIP file download.
        /// </summary>
        public bool Download { get; set; }
    }
}
