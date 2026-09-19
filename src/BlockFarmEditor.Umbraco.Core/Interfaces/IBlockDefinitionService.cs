using BlockFarmEditor.Umbraco.Core.Attributes;
using BlockFarmEditor.Umbraco.Core.Models;
using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using Umbraco.Cms.Core.Models;

namespace BlockFarmEditor.Umbraco.Core.Interfaces
{
    public interface IBlockDefinitionService
    {
        IDictionary<Guid, BlockFarmEditorDefinitionExpanded> RetrieveBlockFarmEditorDefinitions(bool force = false);
        Task<IEnumerable<BlockFarmEditorPropertyGroupModel>> RetrievePropertyEditors(Guid contentTypeKey);

        void ClearCache();
        Dictionary<string, IEnumerable<BlockFarmEditorConfigurationAttribute>> GetConfigMaps();
    }
}