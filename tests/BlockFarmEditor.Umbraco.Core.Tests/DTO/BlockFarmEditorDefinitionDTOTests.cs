using System.Reflection;
using BlockFarmEditor.Umbraco.Core.DTO;
using NPoco;

namespace BlockFarmEditor.Umbraco.Core.Tests.DTO;

public class BlockFarmEditorDefinitionDTOTests
{
    private static BlockFarmEditorDefinitionDTO Create() => new()
    {
        Id = 7,
        Key = Guid.NewGuid(),
        ContentTypeAlias = "heroBlock",
        Type = "partial",
        ViewPath = "~/Views/Partials/Hero.cshtml",
        Category = "Content",
        Enabled = true,
        CreatedBy = Guid.NewGuid(),
        UpdatedBy = Guid.NewGuid(),
        CreateDate = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc),
        UpdateDate = new DateTime(2025, 2, 3, 4, 5, 6, DateTimeKind.Utc),
        DeleteDate = new DateTime(2025, 3, 4, 5, 6, 7, DateTimeKind.Utc)
    };

    [Fact]
    public void Defaults_AreSensible()
    {
        var before = DateTime.UtcNow;
        var dto = new BlockFarmEditorDefinitionDTO
        {
            ContentTypeAlias = "a",
            Type = "b",
            ViewPath = "c",
            Category = "d",
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
        var after = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, dto.Key);
        Assert.False(dto.Enabled);
        Assert.Null(dto.DeleteDate);
        Assert.InRange(dto.CreateDate, before, after);
        Assert.InRange(dto.UpdateDate, before, after);
        Assert.True(dto.HasIdentity);
    }

    [Fact]
    public void NewInstances_GetDistinctKeys()
    {
        var a = Create();
        var b = Create();

        Assert.NotEqual(a.Key, b.Key);
    }

    [Fact]
    public void DeepClone_CopiesEveryValue_IntoNewInstance()
    {
        var original = Create();

        var clone = Assert.IsType<BlockFarmEditorDefinitionDTO>(original.DeepClone());

        Assert.NotSame(original, clone);
        Assert.Equal(original.Id, clone.Id);
        Assert.Equal(original.Key, clone.Key);
        Assert.Equal(original.ContentTypeAlias, clone.ContentTypeAlias);
        Assert.Equal(original.Type, clone.Type);
        Assert.Equal(original.ViewPath, clone.ViewPath);
        Assert.Equal(original.Category, clone.Category);
        Assert.Equal(original.Enabled, clone.Enabled);
        Assert.Equal(original.CreatedBy, clone.CreatedBy);
        Assert.Equal(original.UpdatedBy, clone.UpdatedBy);
        Assert.Equal(original.CreateDate, clone.CreateDate);
        Assert.Equal(original.UpdateDate, clone.UpdateDate);
        Assert.Equal(original.DeleteDate, clone.DeleteDate);
    }

    [Fact]
    public void DeepClone_IsIndependentOfOriginal()
    {
        var original = Create();
        var clone = (BlockFarmEditorDefinitionDTO)original.DeepClone();

        clone.Category = "Changed";

        Assert.Equal("Content", original.Category);
    }

    [Fact]
    public void ResetIdentity_AssignsNewKey_AndKeepsId()
    {
        var dto = Create();
        var originalKey = dto.Key;

        dto.ResetIdentity();

        Assert.NotEqual(originalKey, dto.Key);
        Assert.NotEqual(Guid.Empty, dto.Key);
        Assert.Equal(7, dto.Id);
    }

    [Fact]
    public void TableMapping_UsesExpectedTableAndPrimaryKey()
    {
        var type = typeof(BlockFarmEditorDefinitionDTO);

        Assert.Equal("BlockFarmEditorDefinition", BlockFarmEditorDefinitionDTO.TableName);
        Assert.Equal(BlockFarmEditorDefinitionDTO.TableName, type.GetCustomAttribute<TableNameAttribute>()!.Value);
        Assert.Equal("Id", type.GetCustomAttribute<PrimaryKeyAttribute>()!.Value);
        Assert.NotNull(type.GetCustomAttribute<ExplicitColumnsAttribute>());
    }

    // The column names are the database contract - a rename here needs a migration.
    [Theory]
    [InlineData(nameof(BlockFarmEditorDefinitionDTO.Id), "Id")]
    [InlineData(nameof(BlockFarmEditorDefinitionDTO.Key), "Key")]
    [InlineData(nameof(BlockFarmEditorDefinitionDTO.ContentTypeAlias), "ContentTypeAlias")]
    [InlineData(nameof(BlockFarmEditorDefinitionDTO.Type), "Type")]
    [InlineData(nameof(BlockFarmEditorDefinitionDTO.ViewPath), "ViewPath")]
    [InlineData(nameof(BlockFarmEditorDefinitionDTO.Category), "Category")]
    [InlineData(nameof(BlockFarmEditorDefinitionDTO.Enabled), "Enabled")]
    [InlineData(nameof(BlockFarmEditorDefinitionDTO.CreatedBy), "CreatedBy")]
    [InlineData(nameof(BlockFarmEditorDefinitionDTO.UpdatedBy), "UpdatedBy")]
    [InlineData(nameof(BlockFarmEditorDefinitionDTO.CreateDate), "CreatedAt")]
    [InlineData(nameof(BlockFarmEditorDefinitionDTO.UpdateDate), "UpdatedAt")]
    [InlineData(nameof(BlockFarmEditorDefinitionDTO.DeleteDate), "DeletedAt")]
    public void Property_MapsToExpectedColumn(string propertyName, string expectedColumn)
    {
        var column = typeof(BlockFarmEditorDefinitionDTO).GetProperty(propertyName)!.GetCustomAttribute<ColumnAttribute>();

        Assert.NotNull(column);
        Assert.Equal(expectedColumn, column.Name);
    }

    [Fact]
    public void HasIdentity_IsNotMappedToAColumn()
    {
        var property = typeof(BlockFarmEditorDefinitionDTO).GetProperty(nameof(BlockFarmEditorDefinitionDTO.HasIdentity))!;

        Assert.Null(property.GetCustomAttribute<ColumnAttribute>());
        Assert.NotNull(property.GetCustomAttribute<IgnoreAttribute>());
    }
}
