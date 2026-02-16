using BlockFarmEditor.Umbraco.Core.DTO;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Infrastructure.Migrations;

namespace BlockFarmEditor.Umbraco.Core.Database.BlockFarmEditorDefinitions
{
    public class BlockFarmEditorDefinitionTableDeletedAt(IMigrationContext context) : AsyncMigrationBase(context)
    {
        public const string ColumnName = "DeletedAt";
        protected override Task MigrateAsync()
        {
            Logger.LogDebug("Running migration {MigrationStep}", ColumnName);

            // Lots of methods available in the MigrationBase class - discover with this.
            if(ColumnExists(BlockFarmEditorDefinitionDTO.TableName, ColumnName) == false)
            {
                Logger.LogDebug("Altering database table {DbTable} to add {ColumnName} column", BlockFarmEditorDefinitionDTO.TableName, ColumnName);

                Execute.Sql($"ALTER TABLE {BlockFarmEditorDefinitionDTO.TableName} ADD COLUMN {ColumnName} DATETIME NULL;").Do();
            }

            return Task.CompletedTask;
        }
    }
}
