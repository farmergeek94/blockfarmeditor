using BlockFarmEditor.Umbraco.Core.DTO;

namespace BlockFarmEditor.Umbraco.Tests.Helpers;

internal static class Dtos
{
    public static BlockFarmEditorDefinitionDTO Definition(
        string alias = "heroBlock",
        string category = "Content",
        string type = "partial",
        string viewPath = "~/Views/Partials/Hero.cshtml",
        bool enabled = true,
        int id = 0,
        Guid? key = null) => new()
        {
            Id = id,
            Key = key ?? Guid.NewGuid(),
            ContentTypeAlias = alias,
            Type = type,
            ViewPath = viewPath,
            Category = category,
            Enabled = enabled,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };

    public static BlockFarmEditorLayoutDTO Layout(
        string name = "Two Column",
        string category = "Layouts",
        int id = 0,
        Guid? key = null) => new()
        {
            Id = id,
            Key = key ?? Guid.NewGuid(),
            Name = name,
            Description = $"{name} description",
            Layout = "{\"blocks\":[]}",
            Category = category,
            Type = "layout",
            Icon = "icon-layout",
            Enabled = true,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
}
