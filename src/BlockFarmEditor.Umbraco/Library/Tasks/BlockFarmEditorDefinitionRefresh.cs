using BlockFarmEditor.Umbraco.Core.Interfaces;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace BlockFarmEditor.Umbraco.Library.Tasks
{
    public class BlockFarmEditorDefinitionRefresh(IBlockDefinitionService blockDefinitionService) : INotificationAsyncHandler<ContentTypeChangedNotification>
    {
        public Task HandleAsync(ContentTypeChangedNotification notification, CancellationToken cancellationToken)
        {
            if (notification.Changes.Where(x => x.Item.IsElement == true).Any())
            {
                blockDefinitionService.ClearCache();
            }
            return Task.CompletedTask;
        }
    }
}
