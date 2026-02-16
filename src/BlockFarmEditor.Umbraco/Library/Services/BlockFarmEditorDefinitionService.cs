using BlockFarmEditor.Umbraco.Core.DTO;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using Microsoft.Extensions.Logging;
using NPoco;
using Umbraco.Cms.Infrastructure.Persistence;

namespace BlockFarmEditor.Umbraco.Library.Services
{
    /// <summary>
    /// Service for managing BlockFarmEditor definitions in the database
    /// </summary>
    internal class BlockFarmEditorDefinitionService(
        ILogger<BlockFarmEditorDefinitionService> logger) : IBlockFarmEditorDefinitionService
    {
        public async Task<IAsyncEnumerable<BlockFarmEditorDefinitionDTO>?> GetAllAsync(IUmbracoDatabase umbracoDatabase)
        {
            var result = umbracoDatabase.QueryAsync<BlockFarmEditorDefinitionDTO>($"SELECT * FROM {BlockFarmEditorDefinitionDTO.TableName}");
            return result;
        }

        public async Task<BlockFarmEditorDefinitionDTO?> GetByAliasAsync(IUmbracoDatabase umbracoDatabase, string alias)
        { 
            var result = await umbracoDatabase.SingleOrDefaultAsync<BlockFarmEditorDefinitionDTO>($"SELECT * FROM {BlockFarmEditorDefinitionDTO.TableName} WHERE ContentTypeAlias = @0", alias);
            return result;
        }

        public async Task<BlockFarmEditorDefinitionDTO?> GetByKeyAsync(IUmbracoDatabase umbracoDatabase, Guid key)
        {
            var result = await umbracoDatabase.SingleOrDefaultAsync<BlockFarmEditorDefinitionDTO>($"SELECT * FROM {BlockFarmEditorDefinitionDTO.TableName} WHERE Key = @0", key);
            return result;
        }

        public async Task<IEnumerable<string>> GetCategories(IUmbracoDatabase umbracoDatabase)
        {
            var result = umbracoDatabase.QueryAsync<BlockFarmEditorDefinitionDTO>($"SELECT DISTINCT {nameof(BlockFarmEditorDefinitionDTO.Category)} FROM {BlockFarmEditorDefinitionDTO.TableName}");
            return await result.Select(x => x.Category).ToListAsync();
        }

        public async Task<BlockFarmEditorDefinitionDTO> CreateAsync(IUmbracoDatabase umbracoDatabase, BlockFarmEditorDefinitionDTO dto, Guid createdBy)
        {
            var definition = new BlockFarmEditorDefinitionDTO
            {
                Key = dto.Key,
                ContentTypeAlias = dto.ContentTypeAlias,
                Type = dto.Type,
                ViewPath = dto.ViewPath,
                Category = dto.Category,
                Enabled = dto.Enabled,
                CreatedBy = createdBy,
                UpdatedBy = createdBy
            };

            var insertedId = await umbracoDatabase.InsertAsync(definition);
            dto.Id = Convert.ToInt32(insertedId);

            logger.LogInformation("Created new BlockFarmEditor definition with ID {Id} for ContentTypeAlias {Alias}", definition.Id, definition.ContentTypeAlias);

            return dto;
        }

        public async Task<BlockFarmEditorDefinitionDTO?> UpdateAsync(IUmbracoDatabase umbracoDatabase, int id, string type, string viewPath, string category, bool enabled, Guid updatedBy)
        {
            var existing = await umbracoDatabase.SingleOrDefaultAsync<BlockFarmEditorDefinitionDTO>($"SELECT * FROM {BlockFarmEditorDefinitionDTO.TableName} WHERE Id = @0", id);
            if (existing == null)
            {
                return null;
            }

            existing.Type = type;
            existing.ViewPath = viewPath;
            existing.Category = category;
            existing.Enabled = enabled;
            existing.UpdateDate = DateTime.UtcNow;
            existing.UpdatedBy = updatedBy;

            await umbracoDatabase.UpdateAsync(existing);

            logger.LogInformation("Updated BlockFarmEditor definition with ID {Id}", id);

            return new BlockFarmEditorDefinitionDTO
            {
                Id = existing.Id,
                Key = existing.Key,
                ContentTypeAlias = existing.ContentTypeAlias,
                Type = existing.Type,
                ViewPath = existing.ViewPath,
                Category = existing.Category,
                Enabled = existing.Enabled,
                CreateDate = existing.CreateDate,
                UpdateDate = existing.UpdateDate,
                CreatedBy = existing.CreatedBy,
                UpdatedBy = existing.UpdatedBy,
                DeleteDate = existing.DeleteDate
            };
        }

        public async Task DeleteAsync(IUmbracoDatabase umbracoDatabase, string alias)
        {
            var layout = await GetByAliasAsync(umbracoDatabase, alias);
            if (layout == null)
            {
                logger.LogWarning("Attempted to delete BlockFarmEditor Definition with Alias {Alias}, but it does not exist", alias);
                return;
            }
            await umbracoDatabase.DeleteAsync(layout);
        }
    }
}
