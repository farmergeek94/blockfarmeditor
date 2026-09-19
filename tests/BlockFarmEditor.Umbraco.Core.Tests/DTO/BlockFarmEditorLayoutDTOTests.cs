using System.Reflection;
using BlockFarmEditor.Umbraco.Core.DTO;
using NPoco;

namespace BlockFarmEditor.Umbraco.Core.Tests.DTO;

public class BlockFarmEditorLayoutDTOTests
{
    private static BlockFarmEditorLayoutDTO Create() => new()
    {
        Id = 3,
        Key = Guid.NewGuid(),
        Name = "Two Column",
        Description = "Two equal columns",
        Layout = "{\"blocks\":[]}",
        Category = "Layouts",
        Type = "layout",
        Icon = "icon-layout",
        Enabled = false,
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
        var dto = new BlockFarmEditorLayoutDTO
        {
            Name = "a",
            Description = "b",
            Layout = "c",
            Category = "d",
            Type = "e",
            Icon = "f",
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
        var after = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, dto.Key);
        Assert.True(dto.Enabled);
        Assert.Null(dto.DeleteDate);
        Assert.InRange(dto.CreateDate, before, after);
        Assert.InRange(dto.UpdateDate, before, after);
        Assert.True(dto.HasIdentity);
    }

    [Fact]
    public void DeepClone_CopiesContentValues_IntoNewInstance()
    {
        var original = Create();

        var clone = Assert.IsType<BlockFarmEditorLayoutDTO>(original.DeepClone());

        Assert.NotSame(original, clone);
        Assert.Equal(original.Id, clone.Id);
        Assert.Equal(original.Key, clone.Key);
        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(original.Description, clone.Description);
        Assert.Equal(original.Layout, clone.Layout);
        Assert.Equal(original.Category, clone.Category);
        Assert.Equal(original.Type, clone.Type);
        Assert.Equal(original.Icon, clone.Icon);
        Assert.Equal(original.Enabled, clone.Enabled);
        Assert.Equal(original.CreatedBy, clone.CreatedBy);
        Assert.Equal(original.UpdatedBy, clone.UpdatedBy);
    }

    [Fact]
    public void DeepClone_CopiesAuditDates()
    {
        var original = Create();

        var clone = (BlockFarmEditorLayoutDTO)original.DeepClone();

        Assert.Equal(original.CreateDate, clone.CreateDate);
        Assert.Equal(original.UpdateDate, clone.UpdateDate);
        Assert.Equal(original.DeleteDate, clone.DeleteDate);
    }

    [Fact]
    public void ResetIdentity_AssignsNewKey_AndKeepsId()
    {
        var dto = Create();
        var originalKey = dto.Key;

        dto.ResetIdentity();

        Assert.NotEqual(originalKey, dto.Key);
        Assert.NotEqual(Guid.Empty, dto.Key);
        Assert.Equal(3, dto.Id);
    }

    [Fact]
    public void TableMapping_UsesExpectedTableAndPrimaryKey()
    {
        var type = typeof(BlockFarmEditorLayoutDTO);

        Assert.Equal("BlockFarmEditorLayout", BlockFarmEditorLayoutDTO.TableName);
        Assert.Equal(BlockFarmEditorLayoutDTO.TableName, type.GetCustomAttribute<TableNameAttribute>()!.Value);
        Assert.Equal("Id", type.GetCustomAttribute<PrimaryKeyAttribute>()!.Value);
        Assert.NotNull(type.GetCustomAttribute<ExplicitColumnsAttribute>());
    }

    // The column names are the database contract - a rename here needs a migration.
    [Theory]
    [InlineData(nameof(BlockFarmEditorLayoutDTO.Id), "Id")]
    [InlineData(nameof(BlockFarmEditorLayoutDTO.Key), "Key")]
    [InlineData(nameof(BlockFarmEditorLayoutDTO.Name), "Name")]
    [InlineData(nameof(BlockFarmEditorLayoutDTO.Description), "Description")]
    [InlineData(nameof(BlockFarmEditorLayoutDTO.Layout), "Layout")]
    [InlineData(nameof(BlockFarmEditorLayoutDTO.Category), "Category")]
    [InlineData(nameof(BlockFarmEditorLayoutDTO.Type), "Type")]
    [InlineData(nameof(BlockFarmEditorLayoutDTO.Icon), "Icon")]
    [InlineData(nameof(BlockFarmEditorLayoutDTO.Enabled), "Enabled")]
    [InlineData(nameof(BlockFarmEditorLayoutDTO.CreateDate), "CreateDate")]
    [InlineData(nameof(BlockFarmEditorLayoutDTO.UpdateDate), "UpdateDate")]
    [InlineData(nameof(BlockFarmEditorLayoutDTO.DeleteDate), "DeleteDate")]
    [InlineData(nameof(BlockFarmEditorLayoutDTO.CreatedBy), "CreatedBy")]
    [InlineData(nameof(BlockFarmEditorLayoutDTO.UpdatedBy), "UpdatedBy")]
    public void Property_MapsToExpectedColumn(string propertyName, string expectedColumn)
    {
        var column = typeof(BlockFarmEditorLayoutDTO).GetProperty(propertyName)!.GetCustomAttribute<ColumnAttribute>();

        Assert.NotNull(column);
        Assert.Equal(expectedColumn, column.Name);
    }
}
