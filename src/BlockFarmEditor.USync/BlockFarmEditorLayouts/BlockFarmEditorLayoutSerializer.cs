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
    [SyncSerializer("9E50ED21-718F-4DA0-9307-1FC238A95ED7", "BlockFarmEditor Layout Serializer", BlockFarmEditorLayoutDTO.TableName, IsTwoPass = false)]
    public class BlockFarmEditorLayoutSerializer(IEntityService entityService, ILogger<SyncSerializerBase<BlockFarmEditorLayoutDTO>> logger, IUmbracoDatabaseFactory umbracoDatabaseFactory, IBlockFarmEditorLayoutService blockFarmEditorLayoutService, IUserService userService) : SyncSerializerBase<BlockFarmEditorLayoutDTO>(entityService, logger), ISyncSerializer<BlockFarmEditorLayoutDTO>
    {
        public override async Task DeleteItemAsync(BlockFarmEditorLayoutDTO item)
        {
            using var db = umbracoDatabaseFactory.CreateDatabase();
            await blockFarmEditorLayoutService.DeleteAsync(db, item.Key);
        }

        public override async Task<BlockFarmEditorLayoutDTO?> FindItemAsync(Guid key)
        {
            using var db = umbracoDatabaseFactory.CreateDatabase();

            return await blockFarmEditorLayoutService.GetByKeyAsync(db, key);
        }

        public override async Task<BlockFarmEditorLayoutDTO?> FindItemAsync(string alias)
        {
            using var db = umbracoDatabaseFactory.CreateDatabase();

            if(Guid.TryParse(alias, out var key))
            {
                return await blockFarmEditorLayoutService.GetByKeyAsync(db, key);
            }
            return null;
        }

        public override string ItemAlias(BlockFarmEditorLayoutDTO item)
        {
            return item.Key.ToString();
        }

        public override async Task SaveItemAsync(BlockFarmEditorLayoutDTO item)
        {
            using var db = umbracoDatabaseFactory.CreateDatabase();
            var layout = await blockFarmEditorLayoutService.GetByKeyAsync(db, item.Key);
            if(layout == null)
            {
                await blockFarmEditorLayoutService.CreateAsync(db, item, item.CreatedBy);
            }
            else
            {
                await blockFarmEditorLayoutService.UpdateAsync(db, layout.Id, item, item.UpdatedBy);
            }
        }

        protected override async Task<SyncAttempt<BlockFarmEditorLayoutDTO>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
        {
            var key = node.GetKey();
            // var alias = node.GetAlias();

            var details = new List<uSyncChange>();

            var item = await FindItemAsync(key);

            var isNew = false;

            if(item == null)
            {
                isNew = true;
            }

            var user = userService.GetUserById(-1); // admin user

            item ??= new BlockFarmEditorLayoutDTO
            {
                Key = key,
                Name = node.Element("Name").ValueOrDefault(string.Empty),
                Description = node.Element("Description").ValueOrDefault(string.Empty),
                Layout = node.Element("Layout").ValueOrDefault(string.Empty),
                Category = node.Element("Category").ValueOrDefault(string.Empty),
                Type = node.Element("Type").ValueOrDefault(string.Empty),
                Icon = node.Element("Icon").ValueOrDefault(string.Empty),
                Enabled = node.Element("Enabled").ValueOrDefault(true),
                CreatedBy = user?.Key ?? Guid.Empty,
                UpdatedBy = user?.Key ?? Guid.Empty,
            };

            var name = node.Element("Name").ValueOrDefault(string.Empty);
            if (item.Name != name)
            {
                details.AddUpdate("Name", item.Name, name);
                item.Name = name;
            }

            var description = node.Element("Description").ValueOrDefault(string.Empty);
            if (item.Description != description) {
                details.AddUpdate("Description", item.Description, description);
                item.Description = description;
            }

            var layout = node.Element("Layout").ValueOrDefault(string.Empty);
            if (item.Layout != layout)
            {
                details.AddUpdate("Layout", item.Layout, layout);
                item.Layout = layout;
            }

            var category = node.Element("Category").ValueOrDefault(string.Empty);
            if (item.Category != category) {
                details.AddUpdate("Category", item.Category, category);
                item.Category = category;
            }

            var type = node.Element("Type").ValueOrDefault(string.Empty);
            if (item.Type != type)
            {
                details.AddUpdate("Type", item.Type, type);
                item.Type = type;
            }

            var icon = node.Element("Icon").ValueOrDefault(string.Empty);
            if (item.Icon != icon)
            {
                details.AddUpdate("Icon", item.Icon, icon);
                item.Icon = icon;
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

            return SyncAttempt<BlockFarmEditorLayoutDTO>.Succeed(item.Key.ToString(), item, uSync.Core.ChangeType.Import, details);
        }

        protected override Task<SyncAttempt<XElement>> SerializeCoreAsync(BlockFarmEditorLayoutDTO item, SyncSerializerOptions options)
        {
            return uSyncTaskHelper.FromResultOf(() =>
            {

                var node = InitializeBaseNode(item, item.Key.ToString());

                node.Add(new XElement("Name", item.Name));
                node.Add(new XElement("Description", item.Description));
                node.Add(new XElement("Layout", item.Layout));
                node.Add(new XElement("Category", item.Category));
                node.Add(new XElement("Type", item.Type));
                node.Add(new XElement("Icon", item.Icon));
                node.Add(new XElement("Enabled", item.Enabled));
                node.Add(new XElement("CreatedBy", item.CreatedBy));
                node.Add(new XElement("UpdatedBy", item.UpdatedBy));
                node.Add(new XElement("CreateDate", item.CreateDate));

                return SyncAttempt<XElement>.Succeed(item.Key.ToString(), node, typeof(BlockFarmEditorLayoutDTO), ChangeType.Export);
            });
        }
    }
}
