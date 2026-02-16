using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NPoco;
using System.Runtime.Serialization;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace BlockFarmEditor.Umbraco.Core.DTO
{
    [TableName(TableName)]
    [ExplicitColumns]
    [PrimaryKey(nameof(Id))]
    [Serializable]
    [DataContract]
    public class BlockFarmEditorLayoutDTO : IEntity
    {
        [Ignore]
        public const string TableName = "BlockFarmEditorLayout";

        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
        [Column("Id")]
        [DataMember]
        public int Id { get; set; }

        [Column("Key")]
        [DataMember]
        public Guid Key { get; set; } = Guid.NewGuid();

        [Column("Name")]
        [DataMember]
        [Length(255)]
        public required string Name { get; set; }

        [Column("Description")]
        [DataMember]
        [Length(1000)]

        public required string Description { get; set; }

        [Column("Layout")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        [DataMember]
        public required string Layout { get; set; }

        [Column("Category")]
        [DataMember]
        [Length(255)]
        public required string Category { get; set; }


        [Column("Type")]
        [DataMember]
        [Length(255)]
        public required string Type { get; set; }

        [Column("Icon")]
        [DataMember]
        public required string Icon { get; set; }

        [Column("Enabled")]
        [DataMember]
        public bool Enabled { get; set; } = true;


        [Column("CreateDate")]
        [DataMember]
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        [Column("UpdateDate")]
        [DataMember]
        public DateTime UpdateDate { get; set; } = DateTime.UtcNow;
        [Column("DeleteDate")]
        [NullSetting(NullSetting = NullSettings.Null)]
        [DataMember]
        public DateTime? DeleteDate { get; set; } = null;

        [Column("CreatedBy")]
        [DataMember]
        public required Guid CreatedBy { get; set; } = Guid.Empty;

        [Column("UpdatedBy")]
        [DataMember]
        public required Guid UpdatedBy { get; set; } = Guid.Empty;

        [Ignore]
        public bool HasIdentity => true;

        public object DeepClone()
        {
            return new BlockFarmEditorLayoutDTO
            {
                Id = this.Id,
                Key = this.Key,
                Name = this.Name,
                Description = this.Description,
                Layout = this.Layout,
                Category = this.Category,
                Type = Type,
                Icon = this.Icon,
                Enabled = this.Enabled,
                CreatedBy = this.CreatedBy,
                UpdatedBy = this.UpdatedBy
            };
        }

        public void ResetIdentity()
        {
            Key = Guid.NewGuid();
        }
    }
}
