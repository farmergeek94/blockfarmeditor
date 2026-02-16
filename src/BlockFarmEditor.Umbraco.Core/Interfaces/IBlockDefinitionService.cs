using BlockFarmEditor.Umbraco.Core.Attributes;
using BlockFarmEditor.Umbraco.Core.Models;
using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using System.Text.Json;
using Umbraco.Cms.Core.Models;

namespace BlockFarmEditor.Umbraco.Core.Interfaces
{
    public interface IBlockDefinitionService
    {
        JsonSerializerOptions JsonSerializerReaderOptions { get; }
        JsonSerializerOptions JsonSerializerWriterOptions { get; }

        IDictionary<Guid, BlockFarmEditorDefinitionExpanded> RetrieveBlockFarmEditorDefinitions(bool force = false);
        Task<IEnumerable<BlockFarmEditorPropertyGroupModel>> RetrievePropertyEditors(Guid contentTypeKey);

        void ClearCache();
        Dictionary<string, IEnumerable<BlockFarmEditorConfigurationAttribute>> GetConfigMaps();
    }
}