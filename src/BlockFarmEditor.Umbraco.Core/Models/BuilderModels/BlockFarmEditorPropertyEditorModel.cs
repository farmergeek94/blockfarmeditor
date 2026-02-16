using BlockFarmEditor.Umbraco.Core.Models.ConfigModels;
using Umbraco.Cms.Core.Models.ContentTypeEditing;

namespace BlockFarmEditor.Umbraco.Core.Models.BuilderModels
{
    public class BlockFarmEditorPropertyEditorModel
    {
        public string Alias { get; set; } = string.Empty;

        public string? Label { get; set; }

        public IEnumerable<IBlockFarmEditorConfigItem> Configurations { get; set; } = [];
        public PropertyTypeValidation? Validation { get; set; }
        public string? Description { get; set; }
    }
}
