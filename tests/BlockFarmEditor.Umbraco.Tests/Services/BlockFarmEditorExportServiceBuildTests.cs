using BlockFarmEditor.Umbraco.Tests.Helpers;
using Umbraco.Cms.Core.Models;

namespace BlockFarmEditor.Umbraco.Tests.Services;

public class BlockFarmEditorExportServiceBuildTests : IDisposable
{
    private readonly ExportServiceFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    private Mock<IContentType> AddElementType(string alias, IEnumerable<PropertyGroup>? groups = null, IEnumerable<IPropertyType>? noGroupPropertyTypes = null, params IContentTypeComposition[] compositions)
    {
        var contentType = UmbracoModels.ContentType(alias, groups: groups, noGroupPropertyTypes: noGroupPropertyTypes, compositions: compositions);
        _fixture.ContentTypes.Add(contentType.Object);
        return contentType;
    }

    [Fact]
    public async Task WithNoKeys_ExportsEveryDefinition()
    {
        _fixture.Definitions.AddRange([Dtos.Definition("heroBlock"), Dtos.Definition("cardBlock")]);

        var before = DateTime.UtcNow;
        var package = await _fixture.Service.BuildExportPackageAsync([]);

        Assert.Equal(["heroBlock", "cardBlock"], package.Definitions.Select(x => x.ContentTypeAlias));
        Assert.InRange(package.ExportedAt, before, DateTime.UtcNow);
        Assert.Equal("1.0", package.Version);
    }

    [Fact]
    public async Task WithKeys_ExportsOnlyTheRequestedDefinitions()
    {
        var hero = Dtos.Definition("heroBlock");
        _fixture.Definitions.AddRange([hero, Dtos.Definition("cardBlock")]);

        var package = await _fixture.Service.BuildExportPackageAsync([hero.Key, Guid.NewGuid()]);

        Assert.Same(hero, Assert.Single(package.Definitions));
    }

    [Fact]
    public async Task WhenTheDefinitionTableIsUnavailable_ExportsAnEmptyPackage()
    {
        _fixture.DefinitionService.Setup(x => x.GetAllAsync(_fixture.Database.Object)).ReturnsAsync((IAsyncEnumerable<BlockFarmEditor.Umbraco.Core.DTO.BlockFarmEditorDefinitionDTO>?)null);

        var package = await _fixture.Service.BuildExportPackageAsync([]);

        Assert.Empty(package.Definitions);
        Assert.Empty(package.ElementTypes);
    }

    [Fact]
    public async Task ExportsOnlyElementTypesLinkedToTheExportedDefinitions()
    {
        _fixture.Definitions.Add(Dtos.Definition("heroBlock"));
        AddElementType("heroBlock");
        AddElementType("unrelatedBlock");

        var package = await _fixture.Service.BuildExportPackageAsync([]);

        Assert.Equal(["heroBlock"], package.ElementTypes.Select(x => x.Alias));
    }

    [Fact]
    public async Task MapsTheElementType_IncludingGroupsLoosePropertiesAndFolder()
    {
        var dataTypeKey = Guid.NewGuid();
        var title = UmbracoModels.PropertyType("title", dataTypeKey, name: "Title");
        title.Description = "Shown at the top";
        title.SortOrder = 3;
        title.Mandatory = true;
        title.MandatoryMessage = "Required";
        title.ValidationRegExp = "^.+$";
        title.ValidationRegExpMessage = "Invalid";
        title.LabelOnTop = true;
        title.Variations = ContentVariation.Culture;
        var loose = UmbracoModels.PropertyType("loose");
        var group = UmbracoModels.Tab("content", "Content", title);
        group.SortOrder = 5;

        _fixture.Definitions.Add(Dtos.Definition("heroBlock"));
        var contentType = AddElementType("heroBlock", groups: [group], noGroupPropertyTypes: [loose]);
        contentType.Object.Name = "Hero Block";
        contentType.Object.Description = "A hero";
        contentType.Object.Icon = "icon-star";
        contentType.Object.Variations = ContentVariation.CultureAndSegment;
        _fixture.FolderStructureService.Setup(x => x.BuildContentTypeFolderPathAsync(contentType.Object)).ReturnsAsync("Blocks/Content");

        var dto = Assert.Single((await _fixture.Service.BuildExportPackageAsync([])).ElementTypes);

        Assert.Equal(contentType.Object.Key, dto.Key);
        Assert.Equal("heroBlock", dto.Alias);
        Assert.Equal("Hero Block", dto.Name);
        Assert.Equal("A hero", dto.Description);
        Assert.Equal("icon-star", dto.Icon);
        Assert.True(dto.IsElement);
        Assert.False(dto.AllowedAsRoot);
        Assert.True(dto.VariesByCulture);
        Assert.True(dto.VariesBySegment);
        Assert.Equal("Blocks/Content", dto.FolderPath);
        Assert.Empty(dto.CompositionAliases);

        var groupDto = Assert.Single(dto.PropertyGroups);
        Assert.Equal(group.Key, groupDto.Key);
        Assert.Equal("content", groupDto.Alias);
        Assert.Equal("Content", groupDto.Name);
        Assert.Equal("Tab", groupDto.Type);
        Assert.Equal(5, groupDto.SortOrder);

        var titleDto = Assert.Single(groupDto.PropertyTypes);
        Assert.Equal(title.Key, titleDto.Key);
        Assert.Equal("title", titleDto.Alias);
        Assert.Equal("Title", titleDto.Name);
        Assert.Equal("Shown at the top", titleDto.Description);
        Assert.Equal(3, titleDto.SortOrder);
        Assert.Equal(dataTypeKey, titleDto.DataTypeKey);
        Assert.True(titleDto.Mandatory);
        Assert.Equal("Required", titleDto.MandatoryMessage);
        Assert.Equal("^.+$", titleDto.ValidationRegExp);
        Assert.Equal("Invalid", titleDto.ValidationRegExpMessage);
        Assert.True(titleDto.VariesByCulture);
        Assert.False(titleDto.VariesBySegment);
        Assert.Equal(1, titleDto.LabelOnTop);

        Assert.Equal("loose", Assert.Single(dto.NoGroupPropertyTypes).Alias);
    }

    [Fact]
    public async Task IncludesCompositions_Recursively_AndOrdersThemBeforeTheTypesThatUseThem()
    {
        var baseComposition = AddElementType("baseComposition");
        var seoComposition = AddElementType("seoComposition", compositions: baseComposition.Object);
        AddElementType("heroBlock", compositions: seoComposition.Object);
        _fixture.Definitions.Add(Dtos.Definition("heroBlock"));

        var package = await _fixture.Service.BuildExportPackageAsync([]);

        Assert.Equal(["baseComposition", "seoComposition", "heroBlock"], package.ElementTypes.Select(x => x.Alias));
        Assert.Equal(["seoComposition"], package.ElementTypes.Single(x => x.Alias == "heroBlock").CompositionAliases);
        Assert.Equal(["baseComposition"], package.ElementTypes.Single(x => x.Alias == "seoComposition").CompositionAliases);
    }

    [Fact]
    public async Task SharedCompositions_AreExportedOnce()
    {
        var seoComposition = AddElementType("seoComposition");
        AddElementType("heroBlock", compositions: seoComposition.Object);
        AddElementType("cardBlock", compositions: seoComposition.Object);
        _fixture.Definitions.AddRange([Dtos.Definition("heroBlock"), Dtos.Definition("cardBlock")]);

        var package = await _fixture.Service.BuildExportPackageAsync([]);

        Assert.Equal(["seoComposition", "heroBlock", "cardBlock"], package.ElementTypes.Select(x => x.Alias));
    }

    [Fact]
    public async Task ExportsEachUsedDataTypeOnce_IncludingThoseUsedByCompositions()
    {
        var shared = Guid.NewGuid();
        var compositionOnly = Guid.NewGuid();
        _fixture.AddExistingDataType(shared, "Textstring");
        _fixture.AddExistingDataType(compositionOnly, "Toggle");
        var seoComposition = AddElementType("seoComposition", noGroupPropertyTypes: [UmbracoModels.PropertyType("hide", compositionOnly), UmbracoModels.PropertyType("metaTitle", shared)]);
        AddElementType("heroBlock", noGroupPropertyTypes: [UmbracoModels.PropertyType("title", shared), UmbracoModels.PropertyType("missing", Guid.NewGuid())], compositions: seoComposition.Object);
        _fixture.Definitions.Add(Dtos.Definition("heroBlock"));

        var package = await _fixture.Service.BuildExportPackageAsync([]);

        Assert.Equal(["Textstring", "Toggle"], package.DataTypes.Select(x => x.Name).Order());
    }

    [Fact]
    public async Task MapsTheDataType_SerializingComplexConfigurationValuesAsJson()
    {
        var key = Guid.NewGuid();
        var dataType = _fixture.AddExistingDataType(key, "Theme picker", new Dictionary<string, object>
        {
            ["placeholder"] = "Pick one",
            ["maxItems"] = 3,
            ["multiple"] = true,
            ["items"] = new[] { "light", "dark" }
        });
        dataType.Object.EditorUiAlias = "Umb.PropertyEditorUi.Dropdown";
        _fixture.FolderStructureService.Setup(x => x.BuildDataTypeFolderPathAsync(dataType.Object)).ReturnsAsync("Custom/Pickers");
        AddElementType("heroBlock", noGroupPropertyTypes: [UmbracoModels.PropertyType("theme", key)]);
        _fixture.Definitions.Add(Dtos.Definition("heroBlock"));

        var dto = Assert.Single((await _fixture.Service.BuildExportPackageAsync([])).DataTypes);

        Assert.Equal(key, dto.Key);
        Assert.Equal("Theme picker", dto.Name);
        Assert.Equal(UmbracoModels.TextBoxEditorAlias, dto.EditorAlias);
        Assert.Equal("Umb.PropertyEditorUi.Dropdown", dto.EditorUiAlias);
        Assert.Equal("Custom/Pickers", dto.FolderPath);
        Assert.Equal(
            new Dictionary<string, string?> { ["placeholder"] = "Pick one", ["maxItems"] = "3", ["multiple"] = "true", ["items"] = "[\"light\",\"dark\"]" },
            dto.ConfigurationItems.ToDictionary(x => x.Key, x => x.Value));
    }

    [Theory]
    [InlineData("~/Views/Partials/BlockFarm/Hero.cshtml")]
    [InlineData("~/Partials/BlockFarm/Hero.cshtml")]
    [InlineData("Partials/BlockFarm/Hero.cshtml")]
    public async Task ExportsPartialViews_RelativeToTheViewsFolder(string viewPath)
    {
        _fixture.Site.WriteFile("Views/Partials/BlockFarm/Hero.cshtml", "<h1>@Model.Title</h1>");
        _fixture.Definitions.Add(Dtos.Definition("heroBlock", type: "partial", viewPath: viewPath));

        var partialView = Assert.Single((await _fixture.Service.BuildExportPackageAsync([])).PartialViews);

        Assert.Equal("Partials/BlockFarm/Hero.cshtml", partialView.Path);
        Assert.Equal("<h1>@Model.Title</h1>", partialView.Content);
    }

    [Fact]
    public async Task SkipsPartialViews_ThatAreMissingOnDisk_OrBelongToNonPartialDefinitions()
    {
        _fixture.Site.WriteFile("Views/Partials/Card.cshtml", "<div></div>");
        _fixture.Definitions.AddRange(
        [
            Dtos.Definition("heroBlock", type: "partial", viewPath: "~/Views/Partials/Missing.cshtml"),
            Dtos.Definition("cardBlock", type: "viewcomponent", viewPath: "~/Views/Partials/Card.cshtml"),
            Dtos.Definition("emptyBlock", type: "partial", viewPath: "")
        ]);

        var package = await _fixture.Service.BuildExportPackageAsync([]);

        Assert.Empty(package.PartialViews);
    }
}
