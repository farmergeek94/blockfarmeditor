using BlockFarmEditor.Umbraco.Core.DTO;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using Microsoft.Extensions.Logging;
using NPoco;
using Umbraco.Cms.Infrastructure.Persistence;

namespace BlockFarmEditor.Umbraco.Library.Services
{
    /// <summary>
    /// Service for managing BlockFarmEditor Layouts in the database
    /// </summary>
    internal class BlockFarmEditorLayoutService(
        ILogger<BlockFarmEditorLayoutService> logger) : IBlockFarmEditorLayoutService
    {
        public async Task<IAsyncEnumerable<BlockFarmEditorLayoutDTO>?> GetAllAsync(IUmbracoDatabase umbracoDatabase)
        {
            var result = umbracoDatabase.QueryAsync<BlockFarmEditorLayoutDTO>($"SELECT * FROM {BlockFarmEditorLayoutDTO.TableName}");
            return result;
        }

        public async Task<BlockFarmEditorLayoutDTO?> GetByKeyAsync(IUmbracoDatabase umbracoDatabase, Guid key)
        {
            var result = await umbracoDatabase.SingleOrDefaultAsync<BlockFarmEditorLayoutDTO>($"SELECT * FROM {BlockFarmEditorLayoutDTO.TableName} WHERE Key = @0", key);
            return result;
        }


        public async Task<IEnumerable<string>> GetCategories(IUmbracoDatabase umbracoDatabase)
        {
            var result = umbracoDatabase.QueryAsync<BlockFarmEditorLayoutDTO>($"SELECT DISTINCT {nameof(BlockFarmEditorLayoutDTO.Category)} FROM {BlockFarmEditorLayoutDTO.TableName}");
            return await result.Select(x => x.Category).ToListAsync();
        }

        public async Task<BlockFarmEditorLayoutDTO> CreateAsync(IUmbracoDatabase umbracoDatabase, BlockFarmEditorLayoutDTO dto, Guid createdBy)
        {
            var Layout = new BlockFarmEditorLayoutDTO
            {
                Key = dto.Key,
                Name = dto.Name,
                Description = dto.Description,
                Layout = dto.Layout,
                Category = dto.Category,
                Type = dto.Type,
                Icon = dto.Icon,
                Enabled = dto.Enabled,
                CreatedBy = createdBy,
                UpdatedBy = createdBy
            };

            var insertedId = await umbracoDatabase.InsertAsync(Layout);
            dto.Id = Convert.ToInt32(insertedId);

            logger.LogInformation("Created new BlockFarmEditor Layout with ID {Id} and Name {Name}", Layout.Id, Layout.Name);

            return dto;
        }

        public async Task<BlockFarmEditorLayoutDTO?> UpdateAsync(IUmbracoDatabase umbracoDatabase, int id, BlockFarmEditorLayoutDTO dto, Guid updatedBy)
        {
            var existing = await umbracoDatabase.SingleOrDefaultAsync<BlockFarmEditorLayoutDTO>($"SELECT * FROM {BlockFarmEditorLayoutDTO.TableName} WHERE Id = @0", id);
            if (existing == null)
            {
                return null;
            }

            dto.Id = id;

            await umbracoDatabase.UpdateAsync(dto);

            logger.LogInformation("Updated BlockFarmEditor Layout with ID {Id}", id);

            return new BlockFarmEditorLayoutDTO
            {
                Id = existing.Id,
                Key = existing.Key,
                Name = existing.Name,
                Description = existing.Description,
                Layout = existing.Layout,
                Category = existing.Category,
                Type = existing.Type,
                Icon = dto.Icon,
                Enabled = existing.Enabled,
                CreateDate = existing.CreateDate,
                UpdateDate = existing.UpdateDate,
                CreatedBy = existing.CreatedBy,
                UpdatedBy = existing.UpdatedBy
            };
        }

        public async Task DeleteAsync(IUmbracoDatabase umbracoDatabase, Guid key)
        {
            var layout = await GetByKeyAsync(umbracoDatabase, key);
            if (layout == null)
            {
                logger.LogWarning("Attempted to delete BlockFarmEditor Layout with Key {Key}, but it does not exist", key);
                return;
            }
            await umbracoDatabase.DeleteAsync(layout);
        }
    }
}
