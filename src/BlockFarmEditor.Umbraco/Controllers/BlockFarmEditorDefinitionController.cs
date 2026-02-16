using BlockFarmEditor.Umbraco.Core.DTO;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Xml.Serialization;
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
        IBlockDefinitionService blockDefinitionService) : Controller
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


        [HttpPost]
        public async Task<IActionResult> Export()
        {
            var path = Path.Combine("BlockFarmEditor", "Definitions");
            Directory.CreateDirectory(path);

            using var umbracoDatabase = umbracoDatabaseFactory.CreateDatabase();

            var result = await definitionService.GetAllAsync(umbracoDatabase);
            if (result != null)
            {
                await foreach (var item in result)
                {
                    if (item != null)
                    {
                        // Export as XML to allow embedding JSON in string fields without conflicts
                        var filePath = Path.Combine(path, $"{item.ContentTypeAlias}.xml");
                        try
                        {
                            var serializer = new XmlSerializer(typeof(BlockFarmEditorDefinitionDTO));
                            using var fs = System.IO.File.Create(filePath);
                            serializer.Serialize(fs, item);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed to export definition {Alias} to XML", item.ContentTypeAlias);
                        }
                    }
                }
            }
            return Ok();
        }


        [HttpPost]
        public async Task<IActionResult> Import()
        {
            var path = Path.Combine("BlockFarmEditor", "Definitions");
            if (Path.Exists(path))
            {
                using var umbracoDatabase = umbracoDatabaseFactory.CreateDatabase();

                var files = Directory.EnumerateFiles(path);
                foreach(var file in files)
                {
                    try
                    {
                        // Read XML file; keep any JSON strings in fields like ViewPath untouched
                        BlockFarmEditorDefinitionDTO? dto = null;
                        var serializer = new XmlSerializer(typeof(BlockFarmEditorDefinitionDTO));
                        using (var fs = System.IO.File.OpenRead(file))
                        {
                            dto = serializer.Deserialize(fs) as BlockFarmEditorDefinitionDTO;
                        }

                        if(dto == null)
                        {
                            logger.LogWarning("Skipping import. Could not deserialize XML definition file: {File}", file);
                            continue;
                        }

                        var alias = Path.GetFileNameWithoutExtension(file);

                        var definition = await definitionService.GetByAliasAsync(umbracoDatabase, alias);

                        var currentUser = await GetCurrentUserIdAsync();

                        if (definition != null)
                        {
                            await definitionService.UpdateAsync(umbracoDatabase, definition.Id, dto.Type, dto.ViewPath, dto.Category, dto.Enabled, currentUser);

                        } else
                        {
                            await definitionService.CreateAsync(umbracoDatabase, dto, currentUser);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to import definition from XML file {File}", file);
                    }
                }
            }
            return Ok();
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
}
