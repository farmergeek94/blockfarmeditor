namespace BlockFarmEditor.Umbraco.Core.DTO;

/// <summary>
/// Result of an import operation with counts of created/updated items.
/// </summary>
public class ImportResultDTO
{
    public ImportItemCounts DataTypes { get; set; } = new();
    public ImportItemCounts ElementTypes { get; set; } = new();
    public ImportItemCounts Compositions { get; set; } = new();
    public ImportItemCounts Definitions { get; set; } = new();
    public ImportItemCounts PartialViews { get; set; } = new();
}

/// <summary>
/// Counts for a specific item type during import.
/// </summary>
public class ImportItemCounts
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }

    public int Total => Created + Updated + Skipped + Failed;
    public int Imported => Created + Updated;
}
