using BlockFarmEditor.Umbraco.Core.DTO;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Infrastructure.Persistence;

namespace BlockFarmEditor.Umbraco.Library.Tasks
{
    public class BlockFarmEditorDefinitionCleanUp(ICoreScopeProvider coreScopeProvider, IUmbracoDatabaseFactory umbracoDatabaseFactory) : INotificationAsyncHandler<ContentTypeDeletedNotification>
    {
        public async Task HandleAsync(ContentTypeDeletedNotification notification, CancellationToken cancellationToken)
        {
            using var scope = coreScopeProvider.CreateCoreScope();
            using var umbracoDatabase = umbracoDatabaseFactory.CreateDatabase();
            foreach (var contentType in notification.DeletedEntities)
            {
                // Delete the BlockFarmEditorDefinition associated with the deleted content type
                var definition = await umbracoDatabase.FirstOrDefaultAsync<BlockFarmEditorDefinitionDTO>($"SELECT * FROM {BlockFarmEditorDefinitionDTO.TableName} WHERE ContentTypeAlias = @0", [contentType.Alias], cancellationToken);

                if (definition != null)
                {
                    await umbracoDatabase.DeleteAsync(definition);
                }
            }
        }
    }
}
