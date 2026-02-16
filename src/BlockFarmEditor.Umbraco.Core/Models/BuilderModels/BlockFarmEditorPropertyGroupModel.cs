using Umbraco.Cms.Core.Models;

namespace BlockFarmEditor.Umbraco.Core.Models.BuilderModels;

public class BlockFarmEditorPropertyGroupModel(string alias, string? label, PropertyGroupType type)
{
    public string Alias { get; set; } = alias;

    public string? Label { get; set; } = label;

    public PropertyGroupType Type { get; set; } = type;

    public IDictionary<string, BlockFarmEditorPropertyEditorModel>? Editors { get; set; }

    public IEnumerable<BlockFarmEditorPropertyGroupModel>? Groups { get; set; }
}
