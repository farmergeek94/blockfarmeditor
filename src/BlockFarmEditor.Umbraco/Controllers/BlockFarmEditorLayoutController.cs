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
    public class BlockFarmEditorLayoutController(
        IBlockFarmEditorLayoutService layoutService,
        ILogger<BlockFarmEditorLayoutController> logger,
        IUserService userService,
        IUmbracoDatabaseFactory umbracoDatabaseFactory,
        IBlockDefinitionService blockDefinitionService) : Controller
    {

        /// <summary>
        /// Get a specific BlockFarmEditor layout by Name
        /// </summary>
        /// <param name="key">The layouts key</param>
        /// <returns>The definition if found</returns>
        [HttpGet]
        public async Task<IActionResult> Index(Guid key)
        {
            try
            {
                using var umbracoDatabase = umbracoDatabaseFactory.CreateDatabase();
                var result = await layoutService.GetByKeyAsync(umbracoDatabase, key);
                if (result == null)
                {
                    return Ok(new { });
                }

                return Ok(new { data = result });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving BlockFarmEditor definition with key {Key}", key);
                return StatusCode(500, "Internal server error");
            }
        }


        [HttpPost]
        public async Task<IActionResult> Export()
        {
            var path = Path.Combine("BlockFarmEditor", "Definitions");
            Directory.CreateDirectory(path);

            using var umbracoDatabase = umbracoDatabaseFactory.CreateDatabase();

            var result = await layoutService.GetAllAsync(umbracoDatabase);
            if (result != null)
            {
                await foreach (var item in result)
                {
                    if (item != null)
                    {
                        // Export as XML, keep any JSON strings inside properties untouched
                        var filePath = Path.Combine(path, $"{item.Key}.xml");
                        try
                        {
                            var serializer = new XmlSerializer(typeof(BlockFarmEditorLayoutDTO));
                            using var fs = System.IO.File.Create(filePath);
                            serializer.Serialize(fs, item);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed to export layout {Key} to XML", item.Key);
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
                        // Read XML files; do not parse any JSON within Layout property
                        BlockFarmEditorLayoutDTO? dto = null;
                        var serializer = new XmlSerializer(typeof(BlockFarmEditorLayoutDTO));
                        using (var fs = System.IO.File.OpenRead(file))
                        {
                            dto = serializer.Deserialize(fs) as BlockFarmEditorLayoutDTO;
                        }

                        if(dto == null)
                        {
                            logger.LogWarning("Skipping import. Could not deserialize XML layout file: {File}", file);
                            continue;
                        }

                        var key = Path.GetFileNameWithoutExtension(file);

                        if(!Guid.TryParse(key, out Guid guidKey))
                        {
                            logger.LogError("Invalid GUID key in filename: {Filename}", file);
                            continue;
                        }

                        var layout = await layoutService.GetByKeyAsync(umbracoDatabase, guidKey);

                        var currentUser = await GetCurrentUserIdAsync();

                        if (layout != null)
                        {
                            layout.Category = dto.Category;
                            layout.Type = dto.Type;
                            layout.Description = dto.Description;
                            layout.Layout = dto.Layout; // JSON string preserved
                            layout.Icon = dto.Icon;
                            layout.Name = dto.Name;
                            layout.Enabled = dto.Enabled;
                            await layoutService.UpdateAsync(umbracoDatabase, layout.Id, layout, currentUser);

                        } else
                        {
                            await layoutService.CreateAsync(umbracoDatabase, dto, currentUser);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to import layout from XML file {File}", file);
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

                var result = await layoutService.GetCategories(umbracoDatabase);
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
        public async Task<IActionResult> Create([FromBody] CreateBlockFarmEditorLayoutRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                using var umbracoDatabase = umbracoDatabaseFactory.CreateDatabase();

                // Get current user ID for audit trail
                var currentUser = await GetCurrentUserIdAsync();

                var definition = new BlockFarmEditorLayoutDTO
                {
                    Name = request.Name,
                    Description = request.Description,
                    Layout = request.Layout,
                    Category = request.Category,
                    Type = request.Type,
                    Icon = request.Icon,
                    Enabled = request.Enabled,
                    CreatedBy = currentUser,
                    UpdatedBy = currentUser
                };

                var result = await layoutService.CreateAsync(umbracoDatabase, definition, currentUser);

                blockDefinitionService.ClearCache();

                return CreatedAtAction(nameof(Index), new { key = result.Key }, new { data = result });
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
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateBlockFarmEditorLayoutRequest request)
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

                var layout = await layoutService.GetByKeyAsync(umbracoDatabase, request.Key);

                if (layout == null)
                {
                    return NotFound($"Definition with Key {request.Key} not found");
                }

                var result = await layoutService.UpdateAsync(umbracoDatabase, id, layout, currentUser);
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

        [HttpDelete]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            try
            {
                using var umbracoDatabase = umbracoDatabaseFactory.CreateDatabase();
                await layoutService.DeleteAsync(umbracoDatabase, id);

                return Ok();
            } catch(Exception ex)
            {
                logger.LogError(ex, "Error deleting BlockFarmEditor definition with Key {Key}", id);
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
    public class CreateBlockFarmEditorLayoutRequest
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Layout { get; set; }
        public required string Category { get; set; }
        public required string Type { get; set; }
        public required string Icon { get; set; }
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Request model for updating an existing BlockFarmEditor definition
    /// </summary>
    public class UpdateBlockFarmEditorLayoutRequest
    {
        public required Guid Key { get; set; }
        public required string Description { get; set; }
        public required string Layout { get; set; }
        public required string Category { get; set; }
        public required string Type { get; set; }
        public required string Icon { get; set; }
        public bool Enabled { get; set; }
    }
}
