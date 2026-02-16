using BlockFarmEditor.Umbraco.Core.DTO;
using Microsoft.Extensions.Logging;
using NPoco;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace BlockFarmEditor.Umbraco.Core.Database.BlockFarmEditorDefinitions
{
    public class BlockFarmEditorDefinitionTable(IMigrationContext context) : AsyncMigrationBase(context)
    {
        protected override Task MigrateAsync()
        {
            Logger.LogDebug("Running migration {MigrationStep}", BlockFarmEditorDefinitionDTO.TableName);

            // Lots of methods available in the MigrationBase class - discover with this.
            if (TableExists(BlockFarmEditorDefinitionDTO.TableName) == false)
            {
                Create.Table<BlockFarmEditorDefinitionDTO>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", BlockFarmEditorDefinitionDTO.TableName);
            }

            return Task.CompletedTask;
        }
    }
}
