using BlockFarmEditor.Umbraco.Core.Attributes;
using BlockFarmEditor.Umbraco.Core.DTO;
using System;
using System.Collections.Generic;
using System.Text;
using Umbraco.Cms.Core.Models;

namespace BlockFarmEditor.Umbraco.Core.Models
{
    public class BlockFarmEditorDefinitionExpanded : BlockFarmEditorDefinitionDTO
    {
        public BlockFarmEditorDefinitionAttribute? DefinitionAttribute { get; set; }

        public IContentType? ContentType { get; set; }
        public IEnumerable<BlockFarmEditorConfigurationAttribute> PropertyConfigs { get; set; } = [];

        public static BlockFarmEditorDefinitionExpanded ExpandDto(BlockFarmEditorDefinitionDTO dto)
        {
            return new BlockFarmEditorDefinitionExpanded()
            {
                Id = dto.Id,
                Key = dto.Key,
                ContentTypeAlias = dto.ContentTypeAlias,
                Type = dto.Type,
                ViewPath = dto.ViewPath,
                Category = dto.Category,
                Enabled = dto.Enabled,
                CreateDate = dto.CreateDate,
                UpdateDate = dto.UpdateDate,
                DeleteDate = dto.DeleteDate,
                CreatedBy = dto.CreatedBy,
                UpdatedBy = dto.UpdatedBy
            };
        }
    }
}
