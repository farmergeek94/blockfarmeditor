using BlockFarmEditor.Umbraco.Core.Database.BlockFarmEditorDefinitions;
using BlockFarmEditor.Umbraco.Core.Database.BlockFarmEditorLayouts;
using BlockFarmEditor.Umbraco.Core.DTO;
using Umbraco.Cms.Core.Packaging;

namespace BlockFarmEditor.Umbraco.Library.Migration;
/// <summary>
/// Migration plan for the BlockFarmEditor definitions package.
/// </summary>
/// <remarks>
/// This migration plan is used to define the migration steps for the BlockFarmEditor definitions package.
/// It is registered in the Umbraco CMS to ensure that the package can be migrated correctly.
/// </remarks>

internal class BlockFarmEditorMigration : PackageMigrationPlan
{
    public BlockFarmEditorMigration() : base("Block Farm Editor")
    {
    }

    protected override void DefinePlan()
    {
        // Defines the migration steps for the Block Farm Editor package
        From(string.Empty)
        .To<BlockFarmEditorDefinitionTable>($"{BlockFarmEditorDefinitionDTO.TableName}-db")
        .To<BlockFarmEditorDefinitionTableDeletedAt>($"{BlockFarmEditorDefinitionTableDeletedAt.ColumnName}-db-column")
        .To<BlockFarmEditorLayoutTable>($"{BlockFarmEditorLayoutDTO.TableName}-db")
        .To<BlockFarmEditorInstall>($"BlockFarmEditor-Composer-Install-Update");
    }

    public override bool IgnoreCurrentState => true;
}