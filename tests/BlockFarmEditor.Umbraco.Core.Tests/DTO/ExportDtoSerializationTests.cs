using System.Xml.Linq;
using System.Xml.Serialization;
using BlockFarmEditor.Umbraco.Core.DTO;

namespace BlockFarmEditor.Umbraco.Core.Tests.DTO;

/// <summary>
/// The export DTOs are persisted as XML (zip packages and the BlockFarmEditor folder), so the
/// XML shape is a compatibility contract with previously exported packages.
/// </summary>
public class ExportDtoSerializationTests
{
    private static string Serialize<T>(T value)
    {
        var serializer = new XmlSerializer(typeof(T));
        using var writer = new StringWriter();
        serializer.Serialize(writer, value);
        return writer.ToString();
    }

    private static T Deserialize<T>(string xml)
    {
        var serializer = new XmlSerializer(typeof(T));
        using var reader = new StringReader(xml);
        return (T)serializer.Deserialize(reader)!;
    }

    private static BlockFarmEditorExportPackageDTO CreatePackage() => new()
    {
        ExportedAt = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc),
        Definitions =
        [
            new BlockFarmEditorDefinitionDTO
            {
                Id = 4,
                Key = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ContentTypeAlias = "heroBlock",
                Type = "partial",
                ViewPath = "~/Views/Partials/Hero.cshtml",
                Category = "Content",
                Enabled = true,
                CreatedBy = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                UpdatedBy = Guid.Parse("33333333-3333-3333-3333-333333333333")
            }
        ],
        ElementTypes =
        [
            new ContentTypeExportDTO
            {
                Key = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Alias = "heroBlock",
                Name = "Hero Block",
                Description = null,
                Icon = "icon-star",
                FolderPath = "Blocks/Content",
                VariesByCulture = true,
                CompositionAliases = ["seoComposition"],
                PropertyGroups =
                [
                    new PropertyGroupExportDTO
                    {
                        Key = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                        Alias = "content",
                        Name = "Content",
                        SortOrder = 1,
                        Type = "Tab",
                        PropertyTypes =
                        [
                            new PropertyTypeExportDTO
                            {
                                Key = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                                Alias = "title",
                                Name = "Title",
                                DataTypeKey = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                                Mandatory = true,
                                MandatoryMessage = "Required",
                                ValidationRegExp = "^.+$",
                                LabelOnTop = 1,
                                SortOrder = 2
                            }
                        ]
                    }
                ],
                NoGroupPropertyTypes =
                [
                    new PropertyTypeExportDTO { Alias = "loose", Name = "Loose", DataTypeKey = Guid.Parse("77777777-7777-7777-7777-777777777777") }
                ]
            }
        ],
        DataTypes =
        [
            new DataTypeExportDTO
            {
                Key = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                Name = "Textstring",
                EditorAlias = "Umbraco.TextBox",
                EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
                FolderPath = null,
                ConfigurationItems =
                [
                    new DataTypeConfigurationItemDTO { Key = "maxChars", Value = "50" },
                    new DataTypeConfigurationItemDTO { Key = "empty", Value = null }
                ]
            }
        ],
        PartialViews =
        [
            new PartialViewExportDTO { Path = "Partials/Hero.cshtml", Content = "<h1>@Model.Title</h1>\n<p>&amp; more</p>" }
        ]
    };

    [Fact]
    public void Package_Defaults()
    {
        var before = DateTime.UtcNow;
        var package = new BlockFarmEditorExportPackageDTO();
        var after = DateTime.UtcNow;

        Assert.Equal("1.0", package.Version);
        Assert.InRange(package.ExportedAt, before, after);
        Assert.Empty(package.Definitions);
        Assert.Empty(package.ElementTypes);
        Assert.Empty(package.DataTypes);
        Assert.Empty(package.PartialViews);
    }

    [Fact]
    public void ContentType_Defaults()
    {
        var dto = new ContentTypeExportDTO { Alias = "a", Name = "A" };

        Assert.True(dto.IsElement);
        Assert.False(dto.AllowedAsRoot);
        Assert.False(dto.VariesByCulture);
        Assert.False(dto.VariesBySegment);
        Assert.Null(dto.FolderPath);
        Assert.Empty(dto.PropertyGroups);
        Assert.Empty(dto.NoGroupPropertyTypes);
        Assert.Empty(dto.CompositionAliases);
    }

    [Fact]
    public void Package_UsesExpectedXmlElementNames()
    {
        var root = XDocument.Parse(Serialize(CreatePackage())).Root!;

        Assert.Equal("BlockFarmEditorExportPackage", root.Name.LocalName);
        Assert.Equal("1.0", root.Element("Version")!.Value);
        Assert.Single(root.Element("Definitions")!.Elements("Definition"));
        Assert.Single(root.Element("DataTypes")!.Elements("DataType"));
        Assert.Single(root.Element("PartialViews")!.Elements("PartialView"));

        var elementType = Assert.Single(root.Element("ElementTypes")!.Elements("ElementType"));
        Assert.Equal("seoComposition", Assert.Single(elementType.Element("CompositionAliases")!.Elements("Alias")).Value);
        Assert.Single(elementType.Element("NoGroupPropertyTypes")!.Elements("PropertyType"));

        var group = Assert.Single(elementType.Element("PropertyGroups")!.Elements("PropertyGroup"));
        Assert.Single(group.Element("PropertyTypes")!.Elements("PropertyType"));

        var dataType = root.Element("DataTypes")!.Element("DataType")!;
        Assert.Equal(2, dataType.Element("ConfigurationItems")!.Elements("ConfigurationItem").Count());
    }

    [Fact]
    public void Package_RoundTripsThroughXml()
    {
        var original = CreatePackage();

        var result = Deserialize<BlockFarmEditorExportPackageDTO>(Serialize(original));

        Assert.Equal(original.Version, result.Version);
        Assert.Equal(original.ExportedAt, result.ExportedAt.ToUniversalTime());

        var definition = Assert.Single(result.Definitions);
        Assert.Equal(original.Definitions[0].Key, definition.Key);
        Assert.Equal("heroBlock", definition.ContentTypeAlias);
        Assert.Equal("partial", definition.Type);
        Assert.Equal("~/Views/Partials/Hero.cshtml", definition.ViewPath);
        Assert.Equal("Content", definition.Category);
        Assert.True(definition.Enabled);
        Assert.Equal(original.Definitions[0].CreatedBy, definition.CreatedBy);
        Assert.Equal(original.Definitions[0].UpdatedBy, definition.UpdatedBy);

        var elementType = Assert.Single(result.ElementTypes);
        Assert.Equal(original.ElementTypes[0].Key, elementType.Key);
        Assert.Equal("Hero Block", elementType.Name);
        Assert.Null(elementType.Description);
        Assert.Equal("icon-star", elementType.Icon);
        Assert.Equal("Blocks/Content", elementType.FolderPath);
        Assert.True(elementType.IsElement);
        Assert.True(elementType.VariesByCulture);
        Assert.Equal(["seoComposition"], elementType.CompositionAliases);
        Assert.Equal("loose", Assert.Single(elementType.NoGroupPropertyTypes).Alias);

        var group = Assert.Single(elementType.PropertyGroups);
        Assert.Equal("content", group.Alias);
        Assert.Equal("Tab", group.Type);
        Assert.Equal(1, group.SortOrder);

        var property = Assert.Single(group.PropertyTypes);
        Assert.Equal("title", property.Alias);
        Assert.Equal(original.DataTypes[0].Key, property.DataTypeKey);
        Assert.True(property.Mandatory);
        Assert.Equal("Required", property.MandatoryMessage);
        Assert.Equal("^.+$", property.ValidationRegExp);
        Assert.Null(property.ValidationRegExpMessage);
        Assert.Equal(1, property.LabelOnTop);
        Assert.Equal(2, property.SortOrder);

        var dataType = Assert.Single(result.DataTypes);
        Assert.Equal("Umbraco.TextBox", dataType.EditorAlias);
        Assert.Equal("Umb.PropertyEditorUi.TextBox", dataType.EditorUiAlias);
        Assert.Null(dataType.FolderPath);
        Assert.Equal("50", dataType.ConfigurationItems.Single(x => x.Key == "maxChars").Value);
        Assert.Null(dataType.ConfigurationItems.Single(x => x.Key == "empty").Value);

        var partialView = Assert.Single(result.PartialViews);
        Assert.Equal("Partials/Hero.cshtml", partialView.Path);
        Assert.Equal(original.PartialViews[0].Content, partialView.Content);
    }

    [Fact]
    public void Layout_RoundTripsThroughXml_PreservingEmbeddedJson()
    {
        var original = new BlockFarmEditorLayoutDTO
        {
            Key = Guid.NewGuid(),
            Name = "Two Column",
            Description = "desc",
            Layout = "{\"blocks\":[{\"unique\":\"abc\",\"properties\":{\"text\":\"<b>bold</b> & \\\"quoted\\\"\"}}]}",
            Category = "Layouts",
            Type = "layout",
            Icon = "icon-layout",
            Enabled = false,
            CreatedBy = Guid.NewGuid(),
            UpdatedBy = Guid.NewGuid()
        };

        var result = Deserialize<BlockFarmEditorLayoutDTO>(Serialize(original));

        Assert.Equal(original.Key, result.Key);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.Layout, result.Layout);
        Assert.False(result.Enabled);
        Assert.Equal(original.CreatedBy, result.CreatedBy);
    }
}
