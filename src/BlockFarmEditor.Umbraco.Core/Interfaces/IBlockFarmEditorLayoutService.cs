using BlockFarmEditor.Umbraco.Core.DTO;
using Umbraco.Cms.Infrastructure.Persistence;

namespace BlockFarmEditor.Umbraco.Core.Interfaces
{
    public interface IBlockFarmEditorLayoutService
    {
        Task<BlockFarmEditorLayoutDTO> CreateAsync(IUmbracoDatabase umbracoDatabase, BlockFarmEditorLayoutDTO dto, Guid createdBy);
        Task DeleteAsync(IUmbracoDatabase umbracoDatabase, Guid key);
        Task<IAsyncEnumerable<BlockFarmEditorLayoutDTO>?> GetAllAsync(IUmbracoDatabase umbracoDatabase);
        Task<BlockFarmEditorLayoutDTO?> GetByKeyAsync(IUmbracoDatabase umbracoDatabase, Guid key);
        Task<IEnumerable<string>> GetCategories(IUmbracoDatabase umbracoDatabase);
        Task<BlockFarmEditorLayoutDTO?> UpdateAsync(IUmbracoDatabase umbracoDatabase, int id, BlockFarmEditorLayoutDTO dto, Guid updatedBy);
    }
}