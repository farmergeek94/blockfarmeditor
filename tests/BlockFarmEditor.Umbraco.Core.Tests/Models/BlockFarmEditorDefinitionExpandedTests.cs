using BlockFarmEditor.Umbraco.Core.DTO;
using BlockFarmEditor.Umbraco.Core.Models;

namespace BlockFarmEditor.Umbraco.Core.Tests.Models;

public class BlockFarmEditorDefinitionExpandedTests
{
    [Fact]
    public void ExpandDto_CopiesEveryDtoValue()
    {
        var dto = new BlockFarmEditorDefinitionDTO
        {
            Id = 12,
            Key = Guid.NewGuid(),
            ContentTypeAlias = "heroBlock",
            Type = "viewcomponent",
            ViewPath = "",
            Category = "Content",
            Enabled = true,
            CreatedBy = Guid.NewGuid(),
            UpdatedBy = Guid.NewGuid(),
            CreateDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdateDate = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            DeleteDate = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var expanded = BlockFarmEditorDefinitionExpanded.ExpandDto(dto);

        Assert.Equal(dto.Id, expanded.Id);
        Assert.Equal(dto.Key, expanded.Key);
        Assert.Equal(dto.ContentTypeAlias, expanded.ContentTypeAlias);
        Assert.Equal(dto.Type, expanded.Type);
        Assert.Equal(dto.ViewPath, expanded.ViewPath);
        Assert.Equal(dto.Category, expanded.Category);
        Assert.Equal(dto.Enabled, expanded.Enabled);
        Assert.Equal(dto.CreatedBy, expanded.CreatedBy);
        Assert.Equal(dto.UpdatedBy, expanded.UpdatedBy);
        Assert.Equal(dto.CreateDate, expanded.CreateDate);
        Assert.Equal(dto.UpdateDate, expanded.UpdateDate);
        Assert.Equal(dto.DeleteDate, expanded.DeleteDate);
    }

    [Fact]
    public void ExpandDto_LeavesExpandedOnlyMembersUnset()
    {
        var dto = new BlockFarmEditorDefinitionDTO
        {
            ContentTypeAlias = "a",
            Type = "b",
            ViewPath = "c",
            Category = "d",
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };

        var expanded = BlockFarmEditorDefinitionExpanded.ExpandDto(dto);

        Assert.Null(expanded.DefinitionAttribute);
        Assert.Null(expanded.ContentType);
        Assert.Empty(expanded.PropertyConfigs);
    }
}
