using BlockFarmEditor.Umbraco.Core.DTO;

namespace BlockFarmEditor.Umbraco.Tests.Helpers;

internal static class Packages
{
    public static PropertyTypeExportDTO PropertyType(string alias, Guid dataTypeKey, string? name = null) => new()
    {
        Key = Guid.NewGuid(),
        Alias = alias,
        Name = name ?? alias,
        DataTypeKey = dataTypeKey
    };

    public static PropertyGroupExportDTO Group(string alias, string type = "Group", params PropertyTypeExportDTO[] propertyTypes) => new()
    {
        Key = Guid.NewGuid(),
        Alias = alias,
        Name = alias,
        Type = type,
        PropertyTypes = [.. propertyTypes]
    };

    public static ContentTypeExportDTO ElementType(string alias, params string[] compositionAliases) => new()
    {
        Key = Guid.NewGuid(),
        Alias = alias,
        Name = alias,
        CompositionAliases = [.. compositionAliases]
    };

    public static DataTypeExportDTO DataType(Guid? key = null, string name = "Textstring", string editorAlias = UmbracoModels.TextBoxEditorAlias) => new()
    {
        Key = key ?? Guid.NewGuid(),
        Name = name,
        EditorAlias = editorAlias
    };

    /// <summary>One of everything, for round-trip tests.</summary>
    public static BlockFarmEditorExportPackageDTO Sample()
    {
        var dataType = DataType(name: "Textstring");
        dataType.ConfigurationItems.Add(new DataTypeConfigurationItemDTO { Key = "maxChars", Value = "50" });

        var elementType = ElementType("heroBlock", "seoComposition");
        elementType.PropertyGroups.Add(Group("content", "Tab", PropertyType("title", dataType.Key)));

        return new BlockFarmEditorExportPackageDTO
        {
            Definitions = [Dtos.Definition("heroBlock"), Dtos.Definition("cardBlock")],
            ElementTypes = [ElementType("seoComposition"), elementType],
            DataTypes = [dataType],
            PartialViews = [new PartialViewExportDTO { Path = "Partials/BlockFarm/Hero.cshtml", Content = "<h1>@Model.Title</h1>" }]
        };
    }
}
