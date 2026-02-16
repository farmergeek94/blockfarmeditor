using NPoco;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;
using Microsoft.Extensions.Logging;
using BlockFarmEditor.Umbraco.Core.DTO;

namespace BlockFarmEditor.Umbraco.Core.Database.BlockFarmEditorLayouts
{
    public class BlockFarmEditorLayoutTable(IMigrationContext context) : AsyncMigrationBase(context)
    {
        protected override Task MigrateAsync()
        {
            Logger.LogDebug("Running migration {MigrationStep}", BlockFarmEditorLayoutDTO.TableName);

            // Lots of methods available in the MigrationBase class - discover with this.
            if (TableExists(BlockFarmEditorLayoutDTO.TableName) == false)
            {
                Create.Table<BlockFarmEditorLayoutDTO>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", BlockFarmEditorLayoutDTO.TableName);
            }

            return Task.CompletedTask;
        }
    }
}
