using BlockFarmEditor.Umbraco.Core.DTO;

namespace BlockFarmEditor.USync.Tests;

internal static class Dtos
{
    public static BlockFarmEditorDefinitionDTO Definition(string alias = "heroBlock", int id = 0) => new()
    {
        Id = id,
        Key = Guid.NewGuid(),
        ContentTypeAlias = alias,
        Type = "partial",
        ViewPath = "~/Views/Partials/Hero.cshtml",
        Category = "Content",
        Enabled = true,
        CreatedBy = Guid.NewGuid(),
        UpdatedBy = Guid.NewGuid(),
        CreateDate = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc)
    };

    public static BlockFarmEditorLayoutDTO Layout(string name = "Two Column", int id = 0) => new()
    {
        Id = id,
        Key = Guid.NewGuid(),
        Name = name,
        Description = "Two equal columns",
        Layout = "{\"blocks\":[{\"properties\":{\"text\":\"<b>bold</b> & more\"}}]}",
        Category = "Layouts",
        Type = "layout",
        Icon = "icon-layout",
        Enabled = true,
        CreatedBy = Guid.NewGuid(),
        UpdatedBy = Guid.NewGuid(),
        CreateDate = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc)
    };
}
