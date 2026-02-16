namespace BlockFarmEditor.Umbraco.Core.Models.ConfigModels
{
    public interface IBlockFarmEditorConfig
    {
        Task<IEnumerable<BlockFarmEditorConfigItem>> GetItems();
    }
}