
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NPoco;
using System.Runtime.Serialization;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace BlockFarmEditor.Umbraco.Core.DTO
{
    [TableName(TableName)]
    [ExplicitColumns]
    [PrimaryKey(nameof(Id))]
    [Serializable]
    [DataContract]
    public class BlockFarmEditorDefinitionDTO : IEntity
    {
        [Ignore]
        public const string TableName = "BlockFarmEditorDefinition";

        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
        [Column("Id")]
        [DataMember]
        public int Id { get; set; }

        [Column("Key")]
        [DataMember]
        public Guid Key { get; set; } = Guid.NewGuid();

        [Column("ContentTypeAlias")]
        [DataMember]
        [Length(255)]
        public required string ContentTypeAlias { get; set; }

        [Column("Type")]
        [DataMember]
        [Length(100)]
        public required string Type { get; set; }

        [Column("ViewPath")]
        [DataMember]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        public required string ViewPath { get; set; }

        [Column("Category")]
        [DataMember]
        [Length(255)]
        public required string Category { get; set; }

        [Column("Enabled")]
        [DataMember]
        public bool Enabled { get; set; } = false;

        [Column("CreatedBy")]
        [DataMember]
        public required Guid CreatedBy { get; set; } = Guid.Empty;

        [Column("UpdatedBy")]
        [DataMember]
        public required Guid UpdatedBy { get; set; } = Guid.Empty;

        [Column("CreatedAt")]
        [DataMember]
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;

        [Column("UpdatedAt")]
        [DataMember]
        public DateTime UpdateDate { get; set; } = DateTime.UtcNow;

        [Column("DeletedAt")]
        [NullSetting(NullSetting = NullSettings.Null)]
        [DataMember]
        public DateTime? DeleteDate { get; set; } = null;

        [Ignore]
        public bool HasIdentity => true;

        public object DeepClone()
        {
            return new BlockFarmEditorDefinitionDTO
            {
                Id = Id,
                Key = Key,
                ContentTypeAlias = ContentTypeAlias,
                Type = Type,
                ViewPath = ViewPath,
                Category = Category,
                Enabled = Enabled,
                CreatedBy = CreatedBy,
                UpdatedBy = UpdatedBy,
                CreateDate = CreateDate,
                UpdateDate = UpdateDate,
                DeleteDate = DeleteDate
            };
        }

        public void ResetIdentity()
        {
            Key = Guid.NewGuid();
        }
    }
}
