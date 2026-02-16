using BlockFarmEditor.Umbraco.Core.DTO;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Persistence;
using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;
using static Umbraco.Cms.Core.Constants;

namespace BlockFarmEditor.USync.BlockFarmEditorLayouts
{
    [SyncHandler("BlockFarmEditorLayoutUsyncHandler", "BFE Layouts", BlockFarmEditorLayoutDTO.TableName, 5000
    , Icon = "icon-layout", EntityType = UdiEntityType.Unknown)]
    public class BlockFarmEditorLayoutUsyncHandler(ILogger<SyncHandlerRoot<BlockFarmEditorLayoutDTO, BlockFarmEditorLayoutDTO>> logger, AppCaches appCaches, IShortStringHelper shortStringHelper, ISyncFileService syncFileService, ISyncEventService mutexService, ISyncConfigService uSyncConfig, ISyncItemFactory itemFactory, IBlockFarmEditorLayoutService blockFarmEditorLayoutService, IUmbracoDatabaseFactory umbracoDatabaseFactory) : SyncHandlerRoot<BlockFarmEditorLayoutDTO, BlockFarmEditorLayoutDTO>(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory), ISyncHandler,
    INotificationAsyncHandler<SavedNotification<BlockFarmEditorLayoutDTO>>,
    INotificationAsyncHandler<DeletedNotification<BlockFarmEditorLayoutDTO>>,
    INotificationAsyncHandler<SavingNotification<BlockFarmEditorLayoutDTO>>,
    INotificationAsyncHandler<DeletingNotification<BlockFarmEditorLayoutDTO>>
    {
        protected override Task<IEnumerable<uSyncAction>> DeleteMissingItemsAsync(BlockFarmEditorLayoutDTO parent, IEnumerable<Guid> keysToKeep, bool reportOnly)
        {
            return Task.FromResult(Enumerable.Empty<uSyncAction>());
        }

        protected override async Task<IEnumerable<BlockFarmEditorLayoutDTO>> GetChildItemsAsync(BlockFarmEditorLayoutDTO? parent)
        {
            // When parent is null, we want to return ALL items for root-level export
            if (parent == null)
            {
                using var umbracoDatabase = umbracoDatabaseFactory.CreateDatabase();
                var allItemsAsync = await blockFarmEditorLayoutService.GetAllAsync(umbracoDatabase);

                if (allItemsAsync == null)
                    return Enumerable.Empty<BlockFarmEditorLayoutDTO>();

                var allItems = new List<BlockFarmEditorLayoutDTO>();
                await foreach (var item in allItemsAsync)
                {
                    allItems.Add(item);
                }

                return allItems;
            }

            // If there's a parent, return empty since BlockFarmEditorDefinitions don't have hierarchical structure
            return Enumerable.Empty<BlockFarmEditorLayoutDTO>();
        }

        protected override Task<IEnumerable<BlockFarmEditorLayoutDTO>> GetFoldersAsync(BlockFarmEditorLayoutDTO? parent)
        {
            return Task.FromResult(Enumerable.Empty<BlockFarmEditorLayoutDTO>());
        }

        protected override async Task<BlockFarmEditorLayoutDTO?> GetFromServiceAsync(BlockFarmEditorLayoutDTO? item)
        {
            if(item == null)
                return null;
            using var UmbracoDatabase = umbracoDatabaseFactory.CreateDatabase();
            return await blockFarmEditorLayoutService.GetByKeyAsync(UmbracoDatabase, item!.Key);
        }

        protected override string GetItemName(BlockFarmEditorLayoutDTO item)
        {
            return item.Key.ToString();
        }
    }
}
