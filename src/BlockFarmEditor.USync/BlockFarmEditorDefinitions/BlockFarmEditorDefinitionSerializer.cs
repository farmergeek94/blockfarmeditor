using BlockFarmEditor.Umbraco.Core.DTO;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using uSync.Core;
using uSync.Core.Extensions;
using uSync.Core.Models;
using uSync.Core.Serialization;

namespace BlockFarmEditor.USync.BlockFarmEditorLayouts
{
    [SyncSerializer("9E50ED21-718F-4DA0-9307-1FC238A95ED7", "BlockFarmEditor Definition Serializer", BlockFarmEditorDefinitionDTO.TableName, IsTwoPass = false)]
    public class BlockFarmEditorDefinitionSerializer(IEntityService entityService, ILogger<SyncSerializerBase<BlockFarmEditorDefinitionDTO>> logger, IUmbracoDatabaseFactory umbracoDatabaseFactory, IBlockFarmEditorDefinitionService blockFarmEditorDefinitionService, IUserService userService) : SyncSerializerBase<BlockFarmEditorDefinitionDTO>(entityService, logger), ISyncSerializer<BlockFarmEditorDefinitionDTO>
    {
        public override async Task DeleteItemAsync(BlockFarmEditorDefinitionDTO item)
        {
            using var db = umbracoDatabaseFactory.CreateDatabase();
            await blockFarmEditorDefinitionService.DeleteAsync(db, item.ContentTypeAlias);
        }

        public override async Task<BlockFarmEditorDefinitionDTO?> FindItemAsync(Guid key)
        {
            using var db = umbracoDatabaseFactory.CreateDatabase();

            return await blockFarmEditorDefinitionService.GetByKeyAsync(db, key);
        }

        public override async Task<BlockFarmEditorDefinitionDTO?> FindItemAsync(string alias)
        {
            using var db = umbracoDatabaseFactory.CreateDatabase();

            return await blockFarmEditorDefinitionService.GetByAliasAsync(db, alias);
        }

        public override string ItemAlias(BlockFarmEditorDefinitionDTO item)
        {
            return item.Key.ToString();
        }

        public override async Task SaveItemAsync(BlockFarmEditorDefinitionDTO item)
        {
            using var db = umbracoDatabaseFactory.CreateDatabase();
            var layout = await blockFarmEditorDefinitionService.GetByKeyAsync(db, item.Key);
            if(layout == null)
            {
                await blockFarmEditorDefinitionService.CreateAsync(db, item, item.CreatedBy);
            }
            else
            {
                await blockFarmEditorDefinitionService.UpdateAsync(db, layout.Id, item.Type, item.ViewPath, item.Category, item.Enabled, item.UpdatedBy);
            }
        }

        protected override async Task<SyncAttempt<BlockFarmEditorDefinitionDTO>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
        {
            var key = node.GetKey();
            // var alias = node.GetAlias();

            var details = new List<uSyncChange>();

            var item = await FindItemAsync(key);

            if(item == null)
            {
                var alias = node.Element("ContentTypeAlias").ValueOrDefault(string.Empty);
                item = await FindItemAsync(alias);
            }

            var user = userService.GetUserById(-1); // admin user

            item ??= new BlockFarmEditorDefinitionDTO
            {
                Key = key,
                Type = node.Element("Type").ValueOrDefault(string.Empty),
                ContentTypeAlias = node.Element("ContentTypeAlias").ValueOrDefault(string.Empty),
                ViewPath = node.Element("ViewPath").ValueOrDefault(string.Empty),
                Category = node.Element("Category").ValueOrDefault(string.Empty),
                Enabled = node.Element("Enabled").ValueOrDefault(true),
                CreatedBy = user?.Key ?? Guid.Empty,
                UpdatedBy = user?.Key ?? Guid.Empty,
            };

            var type = node.Element("Type").ValueOrDefault(string.Empty);
            if (item.Type != type) {
                details.AddUpdate("Type", item.Type, type);
                item.Type = type;
            }

            var contentTypeAlias = node.Element("ContentTypeAlias").ValueOrDefault(string.Empty);
            if (item.ContentTypeAlias != contentTypeAlias) {
                details.AddUpdate("ContentTypeAlias", item.ContentTypeAlias, contentTypeAlias);
                item.ContentTypeAlias = contentTypeAlias;
            }

            var viewPath = node.Element("ViewPath").ValueOrDefault(string.Empty);
            if (item.ViewPath != viewPath)
            {
                details.AddUpdate("ViewPath", item.ViewPath, viewPath);
                item.ViewPath = viewPath;
            }

            var category = node.Element("Category").ValueOrDefault(string.Empty);
            if (item.Category != category) {
                details.AddUpdate("Category", item.Category, category);
                item.Category = category;
            }

            var enabled = node.Element("Enabled").ValueOrDefault(true);
            if (item.Enabled != enabled)
            {
                details.AddUpdate("Enabled", item.Enabled, enabled);
                item.Enabled = enabled;
            }

            var createDate = node.Element("CreateDate").ValueOrDefault(DateTime.UtcNow);
            if (item.CreateDate != createDate)
            {
                details.AddUpdate("CreateDate", item.CreateDate, createDate);
                item.CreateDate = createDate;
            }

            var deleteDate = node.Element("DeleteDate").ValueOrDefault<DateTime?>(null);
            if (item.DeleteDate != deleteDate)
            {
                details.AddUpdate("DeleteDate", item.DeleteDate, deleteDate);
                item.DeleteDate = deleteDate;
            }

            var createdBy = node.Element("CreatedBy").ValueOrDefault(Guid.Empty);
            if (item.CreatedBy != createdBy)
            {
                details.AddUpdate("CreatedBy", item.CreatedBy, createdBy);
                item.CreatedBy = createdBy;
            }

            var updatedBy = node.Element("UpdatedBy").ValueOrDefault(Guid.Empty);
            if (item.UpdatedBy != updatedBy)
            {
                details.AddUpdate("UpdatedBy", item.UpdatedBy, updatedBy);
                item.UpdatedBy = updatedBy;
            }

            return SyncAttempt<BlockFarmEditorDefinitionDTO>.Succeed(item.Key.ToString(), item, uSync.Core.ChangeType.Import, details);
        }

        protected override Task<SyncAttempt<XElement>> SerializeCoreAsync(BlockFarmEditorDefinitionDTO item, SyncSerializerOptions options)
        {
            return uSyncTaskHelper.FromResultOf(() =>
            {

                var node = InitializeBaseNode(item, item.ContentTypeAlias);
                
                node.Add(new XElement("Type", item.Type));
                node.Add(new XElement("ContentTypeAlias", item.ContentTypeAlias));
                node.Add(new XElement("ViewPath", item.ViewPath));
                node.Add(new XElement("Category", item.Category));
                node.Add(new XElement("Enabled", item.Enabled));
                node.Add(new XElement("CreatedBy", item.CreatedBy));
                node.Add(new XElement("UpdatedBy", item.UpdatedBy));
                node.Add(new XElement("CreateDate", item.CreateDate));
                node.Add(new XElement("DeleteDate", item.DeleteDate));

                return SyncAttempt<XElement>.Succeed(item.Key.ToString(), node, typeof(BlockFarmEditorDefinitionDTO), ChangeType.Export);
            });
        }
    }
}
