using BlockFarmEditor.Umbraco.Core.DTO;
using Umbraco.Cms.Infrastructure.Persistence;

namespace BlockFarmEditor.Umbraco.Core.Interfaces
{
    /// <summary>
    /// Service for managing BlockFarmEditor definitions in the database
    /// </summary>
    public interface IBlockFarmEditorDefinitionService
    {

        /// <summary>
        /// Get a definition by content type alias
        /// </summary>
        /// <param name="alias">The content type alias</param>
        /// <returns>The definition if found, null otherwise</returns>
        Task<BlockFarmEditorDefinitionDTO?> GetByAliasAsync(IUmbracoDatabase umbracoDatabase, string alias);

        /// <summary>
        /// Create a new definition
        /// </summary>
        /// <param name="definition">The definition to create</param>
        /// <param name="createdBy">The user creating the definition</param>
        /// <returns>The created definition with ID</returns>
        Task<BlockFarmEditorDefinitionDTO> CreateAsync(IUmbracoDatabase umbracoDatabase, BlockFarmEditorDefinitionDTO definition, Guid createdBy);

        /// <summary>
        /// Update an existing definition
        /// </summary>
        /// <param name="id">The definition ID to update</param>
        /// <param name="type">The block type</param>
        /// <param name="viewPath">The view path</param>
        /// <param name="category">The category</param>
        /// <param name="enabled">Whether the definition is enabled</param>
        /// <param name="updatedBy">The user updating the definition</param>
        /// <returns>The updated definition if successful, null if not found</returns>
        Task<BlockFarmEditorDefinitionDTO?> UpdateAsync(IUmbracoDatabase umbracoDatabase, int id, string type, string viewPath, string category, bool enabled, Guid updatedBy);
        
        /// <summary>
        /// Get the Definition Categories
        /// </summary>
        /// <param name="umbracoDatabase"></param>
        /// <returns></returns>
        Task<IEnumerable<string>> GetCategories(IUmbracoDatabase umbracoDatabase);
        Task<IAsyncEnumerable<BlockFarmEditorDefinitionDTO>?> GetAllAsync(IUmbracoDatabase umbracoDatabase);
        Task DeleteAsync(IUmbracoDatabase umbracoDatabase, string alias);
        Task<BlockFarmEditorDefinitionDTO?> GetByKeyAsync(IUmbracoDatabase umbracoDatabase, Guid key);
    }
}
