using BlockFarmEditor.Umbraco.Core.DTO;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Persistence;
using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;
using uSync.Core.Dependency;
using uSync.Core.Models;
using static Umbraco.Cms.Core.Constants;

namespace BlockFarmEditor.USync.BlockFarmEditorLayouts
{
    [SyncHandler("BlockFarmEditorDefinitionUsyncHandler", "BFE Definitions", BlockFarmEditorDefinitionDTO.TableName, 5000
    , Icon = "icon-info", EntityType = UdiEntityType.Unknown)]
    public class BlockFarmEditorDefinitionUsyncHandler(ILogger<SyncHandlerRoot<BlockFarmEditorDefinitionDTO, BlockFarmEditorDefinitionDTO>> logger, AppCaches appCaches, IShortStringHelper shortStringHelper, ISyncFileService syncFileService, ISyncEventService mutexService, ISyncConfigService uSyncConfig, ISyncItemFactory itemFactory, IBlockFarmEditorDefinitionService blockFarmEditorDefinitionService, IUmbracoDatabaseFactory umbracoDatabaseFactory) : SyncHandlerRoot<BlockFarmEditorDefinitionDTO, BlockFarmEditorDefinitionDTO>(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory), ISyncHandler,
    INotificationAsyncHandler<SavedNotification<BlockFarmEditorDefinitionDTO>>,
    INotificationAsyncHandler<DeletedNotification<BlockFarmEditorDefinitionDTO>>,
    INotificationAsyncHandler<SavingNotification<BlockFarmEditorDefinitionDTO>>,
    INotificationAsyncHandler<DeletingNotification<BlockFarmEditorDefinitionDTO>>
    {
        protected override Task<IEnumerable<uSyncAction>> DeleteMissingItemsAsync(BlockFarmEditorDefinitionDTO parent, IEnumerable<Guid> keysToKeep, bool reportOnly)
        {
            return Task.FromResult(Enumerable.Empty<uSyncAction>());
        }

        protected override async Task<IEnumerable<BlockFarmEditorDefinitionDTO>> GetChildItemsAsync(BlockFarmEditorDefinitionDTO? parent)
        {
            // When parent is null, we want to return ALL items for root-level export
            if (parent == null)
            {
                using var umbracoDatabase = umbracoDatabaseFactory.CreateDatabase();
                var allItemsAsync = await blockFarmEditorDefinitionService.GetAllAsync(umbracoDatabase);

                if (allItemsAsync == null)
                    return Enumerable.Empty<BlockFarmEditorDefinitionDTO>();

                var allItems = new List<BlockFarmEditorDefinitionDTO>();
                await foreach (var item in allItemsAsync)
                {
                    allItems.Add(item);
                }

                return allItems;
            }

            // If there's a parent, return empty since BlockFarmEditorDefinitions don't have hierarchical structure
            return Enumerable.Empty<BlockFarmEditorDefinitionDTO>();
        }

        protected override Task<IEnumerable<BlockFarmEditorDefinitionDTO>> GetFoldersAsync(BlockFarmEditorDefinitionDTO? parent)
        {
            return Task.FromResult(Enumerable.Empty<BlockFarmEditorDefinitionDTO>());
        }

        protected override async Task<BlockFarmEditorDefinitionDTO?> GetFromServiceAsync(BlockFarmEditorDefinitionDTO? item)
        {
            if(item == null)
                return null;
            using var UmbracoDatabase = umbracoDatabaseFactory.CreateDatabase();
            return await blockFarmEditorDefinitionService.GetByKeyAsync(UmbracoDatabase, item!.Key);
        }

        protected override string GetItemName(BlockFarmEditorDefinitionDTO item)
        {
            return item.Key.ToString();
        }
    }
}
