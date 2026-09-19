using BlockFarmEditor.Umbraco.Core.DTO;
using BlockFarmEditor.Umbraco.Tests.Helpers;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Infrastructure.Persistence;
using File = System.IO.File;

namespace BlockFarmEditor.Umbraco.Tests.Services;

public class BlockFarmEditorExportServiceImportTests : IDisposable
{
    private readonly ExportServiceFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    private static void AssertCounts(ImportItemCounts counts, int created = 0, int updated = 0, int skipped = 0, int failed = 0)
    {
        Assert.Equal((created, updated, skipped, failed), (counts.Created, counts.Updated, counts.Skipped, counts.Failed));
    }

    [Fact]
    public async Task EmptyPackage_ImportsNothing()
    {
        var result = await _fixture.Service.ImportPackageAsync(new BlockFarmEditorExportPackageDTO());

        foreach (var counts in new[] { result.DataTypes, result.ElementTypes, result.Compositions, result.Definitions, result.PartialViews })
        {
            AssertCounts(counts);
        }
    }

    #region Data types

    [Fact]
    public async Task NewDataType_IsCreated_InItsFolder_AsTheAdminUser()
    {
        _fixture.AddDataEditor(UmbracoModels.TextBoxEditorAlias);
        _fixture.FolderStructureService.Setup(x => x.EnsureDataTypeFolderStructureAsync("Custom/Pickers")).ReturnsAsync(77);
        var dto = Packages.DataType(name: "Theme picker");
        dto.EditorUiAlias = "Umb.PropertyEditorUi.Dropdown";
        dto.FolderPath = "Custom/Pickers";
        dto.ConfigurationItems.AddRange(
        [
            new DataTypeConfigurationItemDTO { Key = "placeholder", Value = "Pick one" },
            new DataTypeConfigurationItemDTO { Key = "maxItems", Value = "3" },
            new DataTypeConfigurationItemDTO { Key = "items", Value = "[\"light\",\"dark\"]" },
            new DataTypeConfigurationItemDTO { Key = "unset", Value = null }
        ]);
        IDataType? created = null;
        _fixture.DataTypeService
            .Setup(x => x.CreateAsync(It.IsAny<IDataType>(), ExportServiceFixture.AdminKey))
            .ReturnsAsync((IDataType dataType, Guid _) =>
            {
                created = dataType;
                return Attempt.SucceedWithStatus(DataTypeOperationStatus.Success, dataType);
            });

        var result = await _fixture.Service.ImportPackageAsync(new BlockFarmEditorExportPackageDTO { DataTypes = [dto] });

        AssertCounts(result.DataTypes, created: 1);
        Assert.NotNull(created);
        Assert.Equal(dto.Key, created.Key);
        Assert.Equal("Theme picker", created.Name);
        Assert.Equal("Umb.PropertyEditorUi.Dropdown", created.EditorUiAlias);
        Assert.Equal(77, created.ParentId);
        Assert.Equal(["placeholder", "maxItems", "items"], created.ConfigurationData.Keys);
        Assert.Equal("Pick one", created.ConfigurationData["placeholder"].ToString());
        Assert.Equal("3", created.ConfigurationData["maxItems"].ToString());
        Assert.Equal("[\"light\",\"dark\"]", created.ConfigurationData["items"].ToString());
    }

    [Fact]
    public async Task NewDataType_WhosePropertyEditorIsNotInstalled_Fails()
    {
        var result = await _fixture.Service.ImportPackageAsync(new BlockFarmEditorExportPackageDTO { DataTypes = [Packages.DataType(editorAlias: "Missing.Editor")] });

        AssertCounts(result.DataTypes, failed: 1);
        _fixture.DataTypeService.Verify(x => x.CreateAsync(It.IsAny<IDataType>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task NewDataType_RejectedByUmbraco_Fails()
    {
        _fixture.AddDataEditor(UmbracoModels.TextBoxEditorAlias);
        _fixture.DataTypeService
            .Setup(x => x.CreateAsync(It.IsAny<IDataType>(), It.IsAny<Guid>()))
            .ReturnsAsync((IDataType dataType, Guid _) => Attempt.FailWithStatus(DataTypeOperationStatus.DuplicateKey, dataType));

        var result = await _fixture.Service.ImportPackageAsync(new BlockFarmEditorExportPackageDTO { DataTypes = [Packages.DataType()] });

        AssertCounts(result.DataTypes, failed: 1);
    }

    [Fact]
    public async Task ExistingDataType_IsSkippedByDefault()
    {
        var dto = Packages.DataType(name: "New name");
        var existing = _fixture.AddExistingDataType(dto.Key, "Old name");

        var result = await _fixture.Service.ImportPackageAsync(new BlockFarmEditorExportPackageDTO { DataTypes = [dto] });

        AssertCounts(result.DataTypes, skipped: 1);
        existing.VerifySet(x => x.Name = It.IsAny<string>(), Times.Never);
        _fixture.DataTypeService.Verify(x => x.UpdateAsync(It.IsAny<IDataType>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ExistingDataType_WithOverwrite_IsUpdated()
    {
        var dto = Packages.DataType(name: "New name");
        dto.EditorUiAlias = "New.Ui";
        dto.ConfigurationItems.Add(new DataTypeConfigurationItemDTO { Key = "maxChars", Value = "50" });
        var existing = _fixture.AddExistingDataType(dto.Key, "Old name", new Dictionary<string, object> { ["stale"] = true });
        existing.SetupProperty(x => x.Name, "Old name");

        var result = await _fixture.Service.ImportPackageAsync(new BlockFarmEditorExportPackageDTO { DataTypes = [dto] }, overwriteDataTypes: true);

        AssertCounts(result.DataTypes, updated: 1);
        Assert.Equal("New name", existing.Object.Name);
        Assert.Equal("New.Ui", existing.Object.EditorUiAlias);
        Assert.Equal(["maxChars"], existing.Object.ConfigurationData.Keys);
        _fixture.DataTypeService.Verify(x => x.UpdateAsync(existing.Object, ExportServiceFixture.AdminKey), Times.Once);
    }

    [Fact]
    public async Task ADataTypeThatThrows_IsCountedAsFailed_AndTheImportContinues()
    {
        _fixture.AddDataEditor(UmbracoModels.TextBoxEditorAlias);
        var broken = Packages.DataType(name: "Broken");
        var fine = Packages.DataType(name: "Fine");
        _fixture.DataTypeService.Setup(x => x.GetAsync(broken.Key)).ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _fixture.Service.ImportPackageAsync(new BlockFarmEditorExportPackageDTO { DataTypes = [broken, fine] });

        AssertCounts(result.DataTypes, created: 1, failed: 1);
    }

    #endregion

    #region Element types

    [Fact]
    public async Task NewElementType_IsCreated_WithItsGroupsAndProperties()
    {
        var dataTypeKey = Guid.NewGuid();
        var dataType = _fixture.AddExistingDataType(dataTypeKey);
        _fixture.FolderStructureService.Setup(x => x.EnsureContentTypeFolderStructureAsync("Blocks/Content")).ReturnsAsync(55);

        var title = Packages.PropertyType("title", dataTypeKey, "Title");
        title.Description = "Shown at the top";
        title.SortOrder = 3;
        title.Mandatory = true;
        title.MandatoryMessage = "Required";
        title.ValidationRegExp = "^.+$";
        title.ValidationRegExpMessage = "Invalid";
        title.VariesByCulture = true;
        title.LabelOnTop = 1;
        var tab = Packages.Group("content", "Tab", title);
        tab.SortOrder = 4;
        var dto = Packages.ElementType("heroBlock");
        dto.Name = "Hero Block";
        dto.Description = "A hero";
        dto.Icon = "icon-star";
        dto.FolderPath = "Blocks/Content";
        dto.VariesByCulture = true;
        dto.PropertyGroups.Add(tab);
        dto.PropertyGroups.Add(Packages.Group("unknownType", "NotARealGroupType"));
        dto.NoGroupPropertyTypes.Add(Packages.PropertyType("loose", dataTypeKey));

        var result = await _fixture.Service.ImportPackageAsync(new BlockFarmEditorExportPackageDTO { ElementTypes = [dto] });

        AssertCounts(result.ElementTypes, created: 1);
        AssertCounts(result.Compositions);
        _fixture.ContentTypeService.Verify(x => x.CreateAsync(It.IsAny<IContentType>(), ExportServiceFixture.AdminKey), Times.Once);

        var created = Assert.Single(_fixture.ContentTypes);
        Assert.Equal(dto.Key, created.Key);
        Assert.Equal("heroBlock", created.Alias);
        Assert.Equal("Hero Block", created.Name);
        Assert.Equal("A hero", created.Description);
        Assert.Equal("icon-star", created.Icon);
        Assert.Equal(55, created.ParentId);
        Assert.True(created.IsElement);
        Assert.False(created.AllowedAsRoot);
        Assert.Equal(ContentVariation.Culture, created.Variations);

        var createdTab = created.PropertyGroups.Single(x => x.Alias == "content");
        Assert.Equal(tab.Key, createdTab.Key);
        Assert.Equal(PropertyGroupType.Tab, createdTab.Type);
        Assert.Equal(4, createdTab.SortOrder);
        Assert.Equal(PropertyGroupType.Group, created.PropertyGroups.Single(x => x.Alias == "unknownType").Type);

        var createdTitle = Assert.Single(createdTab.PropertyTypes!);
        Assert.Equal(title.Key, createdTitle.Key);
        Assert.Equal("title", createdTitle.Alias);
        Assert.Equal("Title", createdTitle.Name);
        Assert.Equal("Shown at the top", createdTitle.Description);
        Assert.Equal(3, createdTitle.SortOrder);
        Assert.Equal(dataType.Object.Id, createdTitle.DataTypeId);
        Assert.Equal(UmbracoModels.TextBoxEditorAlias, createdTitle.PropertyEditorAlias);
        Assert.True(createdTitle.Mandatory);
        Assert.Equal("Required", createdTitle.MandatoryMessage);
        Assert.Equal("^.+$", createdTitle.ValidationRegExp);
        Assert.Equal("Invalid", createdTitle.ValidationRegExpMessage);
        Assert.Equal(ContentVariation.Culture, createdTitle.Variations);
        Assert.True(createdTitle.LabelOnTop);

        Assert.Equal("loose", Assert.Single(created.NoGroupPropertyTypes).Alias);
    }

    [Fact]
    public async Task NewElementType_UsesDataTypesImportedFromTheSamePackage()
    {
        _fixture.AddDataEditor(UmbracoModels.TextBoxEditorAlias);
        var dataType = Packages.DataType();
        var dto = Packages.ElementType("heroBlock");
        dto.NoGroupPropertyTypes.Add(Packages.PropertyType("title", dataType.Key));

        var result = await _fixture.Service.ImportPackageAsync(new BlockFarmEditorExportPackageDTO { DataTypes = [dataType], ElementTypes = [dto] });

        AssertCounts(result.ElementTypes, created: 1);
        Assert.Equal(ExportServiceFixture.NewDataTypeId, Assert.Single(_fixture.ContentTypes.Single().PropertyTypes).DataTypeId);
        _fixture.DataTypeService.Verify(x => x.GetAsync(dataType.Key), Times.Once); // only the existence check - not looked up again per property
    }

    [Fact]
    public async Task NewElementType_DropsPropertiesWhoseDataTypeCannotBeFound()
    {
        var known = Guid.NewGuid();
        _fixture.AddExistingDataType(known);
        var dto = Packages.ElementType("heroBlock");
        dto.NoGroupPropertyTypes.AddRange([Packages.PropertyType("title", known), Packages.PropertyType("orphan", Guid.NewGuid())]);

        await _fixture.Service.ImportPackageAsync(new BlockFarmEditorExportPackageDTO { ElementTypes = [dto] });

        Assert.Equal(["title"], _fixture.ContentTypes.Single().PropertyTypes.Select(x => x.Alias));
    }

    [Fact]
    public async Task ExistingElementType_IsUpdatedByDefault_MergingGroupsAndProperties()
    {
        var dataTypeKey = Guid.NewGuid();
        _fixture.AddExistingDataType(dataTypeKey);
        var existingTitle = UmbracoModels.PropertyType("title", dataTypeKey, name: "Old title");
        var existingGroup = UmbracoModels.Group("content", "Old content", existingTitle);
        var existing = _fixture.AddExistingContentType("heroBlock", existingGroup);

        var title = Packages.PropertyType("title", dataTypeKey, "New title");
        title.Mandatory = true;
        title.LabelOnTop = 1;
        title.SortOrder = 9;
        var contentGroup = Packages.Group("content", "Group", title, Packages.PropertyType("subtitle", dataTypeKey));
        contentGroup.Name = "New content";
        contentGroup.SortOrder = 2;
        var dto = Packages.ElementType("heroBlock");
        dto.Name = "New hero";
        dto.Description = "New description";
        dto.Icon = "icon-new";
        dto.VariesBySegment = true;
        dto.PropertyGroups.AddRange([contentGroup, Packages.Group("settings", "Tab", Packages.PropertyType("anchor", dataTypeKey))]);
        dto.NoGroupPropertyTypes.Add(Packages.PropertyType("loose", dataTypeKey));

        var result = await _fixture.Service.ImportPackageAsync(new BlockFarmEditorExportPackageDTO { ElementTypes = [dto] });

        AssertCounts(result.ElementTypes, updated: 1);
        _fixture.ContentTypeService.Verify(x => x.UpdateAsync(existing, ExportServiceFixture.AdminKey), Times.Once);
        _fixture.ContentTypeService.Verify(x => x.CreateAsync(It.IsAny<IContentType>(), It.IsAny<Guid>()), Times.Never);

        Assert.Equal("New hero", existing.Name);
        Assert.Equal("New description", existing.Description);
        Assert.Equal("icon-new", existing.Icon);
        Assert.Equal(ContentVariation.Segment, existing.Variations);

        Assert.Same(existingGroup, existing.PropertyGroups.Single(x => x.Alias == "content"));
        Assert.Equal("New content", existingGroup.Name);
        Assert.Equal(2, existingGroup.SortOrder);
        Assert.Equal(["title", "subtitle"], existingGroup.PropertyTypes!.Select(x => x.Alias));

        Assert.Equal("New title", existingTitle.Name);
        Assert.True(existingTitle.Mandatory);
        Assert.True(existingTitle.LabelOnTop);
        Assert.Equal(9, existingTitle.SortOrder);

        var settings = existing.PropertyGroups.Single(x => x.Alias == "settings");
        Assert.Equal(PropertyGroupType.Tab, settings.Type);
        Assert.Equal("anchor", Assert.Single(settings.PropertyTypes!).Alias);
        Assert.Equal("loose", Assert.Single(existing.NoGroupPropertyTypes).Alias);
    }

    [Fact]
    public async Task ExistingElementType_WithOverwriteDisabled_IsSkipped()
    {
        var existing = _fixture.AddExistingContentType("heroBlock");
        var dto = Packages.ElementType("heroBlock");
        dto.Name = "New hero";

        var result = await _fixture.Service.ImportPackageAsync(new BlockFarmEditorExportPackageDTO { ElementTypes = [dto] }, overwriteElementTypes: false);

        AssertCounts(result.ElementTypes, skipped: 1);
        Assert.Equal("Old heroBlock", existing.Name);
        _fixture.ContentTypeService.Verify(x => x.UpdateAsync(It.IsAny<IContentType>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task AnElementTypeThatThrows_IsCountedAsFailed_AndTheImportContinues()
    {
        _fixture.FolderStructureService.Setup(x => x.EnsureContentTypeFolderStructureAsync("Broken")).ThrowsAsync(new InvalidOperationException("boom"));
        var broken = Packages.ElementType("brokenBlock");
        broken.FolderPath = "Broken";

        var result = await _fixture.Service.ImportPackageAsync(new BlockFarmEditorExportPackageDTO { ElementTypes = [broken, Packages.ElementType("heroBlock")] });

        AssertCounts(result.ElementTypes, created: 1, failed: 1);
    }

    #endregion

    #region Compositions

    [Fact]
    public async Task Compositions_AreCreatedBeforeTheirDependents_CountedSeparately_AndLinked()
    {
        var package = new BlockFarmEditorExportPackageDTO
        {
            // deliberately listed dependents-first
            ElementTypes = [Packages.ElementType("heroBlock", "seoComposition"), Packages.ElementType("seoComposition", "baseComposition"), Packages.ElementType("baseComposition")]
        };

        var result = await _fixture.Service.ImportPackageAsync(package);

        AssertCounts(result.ElementTypes, created: 1);
        AssertCounts(result.Compositions, created: 2);
        Assert.Equal(["baseComposition", "seoComposition", "heroBlock"], _fixture.ContentTypes.Select(x => x.Alias));

        var hero = _fixture.ContentTypes.Single(x => x.Alias == "heroBlock");
        var seo = _fixture.ContentTypes.Single(x => x.Alias == "seoComposition");
        Assert.Equal(["seoComposition"], hero.ContentTypeComposition.Select(x => x.Alias));
        Assert.Equal(["baseComposition"], seo.ContentTypeComposition.Select(x => x.Alias));
        _fixture.ContentTypeService.Verify(x => x.UpdateAsync(hero, ExportServiceFixture.AdminKey), Times.Once);
        _fixture.ContentTypeService.Verify(x => x.UpdateAsync(seo, ExportServiceFixture.AdminKey), Times.Once);
    }

    [Fact]
    public async Task Compositions_AlreadyLinked_AreNotLinkedAgain()
    {
        var seo = _fixture.AddExistingContentType("seoComposition");
        var hero = _fixture.AddExistingContentType("heroBlock");
        hero.AddContentType(seo);

        await _fixture.Service.ImportPackageAsync(
            new BlockFarmEditorExportPackageDTO { ElementTypes = [Packages.ElementType("seoComposition"), Packages.ElementType("heroBlock", "seoComposition")] },
            overwriteElementTypes: false,
            overwriteCompositions: false);

        Assert.Single(hero.ContentTypeComposition);
        _fixture.ContentTypeService.Verify(x => x.UpdateAsync(It.IsAny<IContentType>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Compositions_MissingFromBothPackageAndSite_AreIgnored()
    {
        var result = await _fixture.Service.ImportPackageAsync(new BlockFarmEditorExportPackageDTO { ElementTypes = [Packages.ElementType("heroBlock", "missingComposition")] });

        AssertCounts(result.ElementTypes, created: 1);
        Assert.Empty(_fixture.ContentTypes.Single().ContentTypeComposition);
    }

    [Theory]
    [InlineData(true, false, 1, 0, 0, 1)]
    [InlineData(false, true, 0, 1, 1, 0)]
    public async Task OverwriteFlags_ApplyToElementTypesAndCompositionsIndependently(
        bool overwriteElementTypes, bool overwriteCompositions, int elementTypesUpdated, int elementTypesSkipped, int compositionsUpdated, int compositionsSkipped)
    {
        _fixture.AddExistingContentType("seoComposition");
        _fixture.AddExistingContentType("heroBlock");
        var package = new BlockFarmEditorExportPackageDTO { ElementTypes = [Packages.ElementType("seoComposition"), Packages.ElementType("heroBlock", "seoComposition")] };

        var result = await _fixture.Service.ImportPackageAsync(package, overwriteElementTypes: overwriteElementTypes, overwriteCompositions: overwriteCompositions);

        AssertCounts(result.ElementTypes, updated: elementTypesUpdated, skipped: elementTypesSkipped);
        AssertCounts(result.Compositions, updated: compositionsUpdated, skipped: compositionsSkipped);
    }

    #endregion

    #region Definitions

    [Fact]
    public async Task NewDefinition_IsCreated_AsTheAdminUser()
    {
        var definition = Dtos.Definition("heroBlock");

        var result = await _fixture.Service.ImportPackageAsync(new BlockFarmEditorExportPackageDTO { Definitions = [definition] });

        AssertCounts(result.Definitions, created: 1);
        _fixture.DefinitionService.Verify(x => x.CreateAsync(_fixture.Database.Object, definition, ExportServiceFixture.AdminKey), Times.Once);
    }

    [Fact]
    public async Task ExistingDefinition_IsUpdatedByDefault_MatchedOnAlias()
    {
        _fixture.Definitions.Add(Dtos.Definition("heroBlock", id: 12));
        var incoming = Dtos.Definition("heroBlock", category: "New", type: "viewcomponent", viewPath: "new.cshtml", enabled: false);

        var result = await _fixture.Service.ImportPackageAsync(new BlockFarmEditorExportPackageDTO { Definitions = [incoming] });

        AssertCounts(result.Definitions, updated: 1);
        _fixture.DefinitionService.Verify(x => x.UpdateAsync(_fixture.Database.Object, 12, "viewcomponent", "new.cshtml", "New", false, ExportServiceFixture.AdminKey), Times.Once);
        _fixture.DefinitionService.Verify(x => x.CreateAsync(It.IsAny<IUmbracoDatabase>(), It.IsAny<BlockFarmEditorDefinitionDTO>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ExistingDefinition_WithOverwriteDisabled_IsSkipped()
    {
        _fixture.Definitions.Add(Dtos.Definition("heroBlock", id: 12));

        var result = await _fixture.Service.ImportPackageAsync(new BlockFarmEditorExportPackageDTO { Definitions = [Dtos.Definition("heroBlock")] }, overwriteBlockDefinitions: false);

        AssertCounts(result.Definitions, skipped: 1);
        _fixture.DefinitionService.Verify(x => x.UpdateAsync(It.IsAny<IUmbracoDatabase>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ADefinitionThatThrows_IsCountedAsFailed_AndTheImportContinues()
    {
        var broken = Dtos.Definition("brokenBlock");
        _fixture.DefinitionService.Setup(x => x.CreateAsync(_fixture.Database.Object, broken, It.IsAny<Guid>())).ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _fixture.Service.ImportPackageAsync(new BlockFarmEditorExportPackageDTO { Definitions = [broken, Dtos.Definition("heroBlock")] });

        AssertCounts(result.Definitions, created: 1, failed: 1);
    }

    [Fact]
    public async Task WhenTheAdminUserIsMissing_ImportsAsTheEmptyUser()
    {
        _fixture.UserService.Setup(x => x.GetUserById(-1)).Returns((IUser?)null);
        var definition = Dtos.Definition("heroBlock");

        await _fixture.Service.ImportPackageAsync(new BlockFarmEditorExportPackageDTO { Definitions = [definition] });

        _fixture.DefinitionService.Verify(x => x.CreateAsync(_fixture.Database.Object, definition, Guid.Empty), Times.Once);
    }

    #endregion

    #region Partial views

    private static BlockFarmEditorExportPackageDTO PartialViewPackage(string path, string content) =>
        new() { PartialViews = [new PartialViewExportDTO { Path = path, Content = content }] };

    [Fact]
    public async Task NewPartialView_IsWrittenUnderTheViewsFolder_CreatingDirectories()
    {
        var result = await _fixture.Service.ImportPackageAsync(PartialViewPackage("Partials/BlockFarm/Hero.cshtml", "<h1>new</h1>"));

        AssertCounts(result.PartialViews, created: 1);
        Assert.Equal("<h1>new</h1>", File.ReadAllText(_fixture.Site.Combine("Views", "Partials", "BlockFarm", "Hero.cshtml")));
    }

    [Fact]
    public async Task ExistingPartialView_IsOverwrittenByDefault()
    {
        var path = _fixture.Site.WriteFile("Views/Partials/Hero.cshtml", "<h1>old</h1>");

        var result = await _fixture.Service.ImportPackageAsync(PartialViewPackage("Partials/Hero.cshtml", "<h1>new</h1>"));

        AssertCounts(result.PartialViews, updated: 1);
        Assert.Equal("<h1>new</h1>", File.ReadAllText(path));
    }

    [Fact]
    public async Task ExistingPartialView_WithOverwriteDisabled_IsLeftAlone()
    {
        var path = _fixture.Site.WriteFile("Views/Partials/Hero.cshtml", "<h1>old</h1>");

        var result = await _fixture.Service.ImportPackageAsync(PartialViewPackage("Partials/Hero.cshtml", "<h1>new</h1>"), overwritePartialViews: false);

        AssertCounts(result.PartialViews, skipped: 1);
        Assert.Equal("<h1>old</h1>", File.ReadAllText(path));
    }

    [Fact]
    public async Task APartialViewThatCannotBeWritten_IsCountedAsFailed_AndTheImportContinues()
    {
        // a file where a directory is needed makes the write fail
        _fixture.Site.WriteFile("Views/Partials", "i am a file");
        var package = PartialViewPackage("Partials/Hero.cshtml", "x");
        package.PartialViews.Add(new PartialViewExportDTO { Path = "Other/Card.cshtml", Content = "y" });

        var result = await _fixture.Service.ImportPackageAsync(package);

        AssertCounts(result.PartialViews, created: 1, failed: 1);
    }

    #endregion

    [Fact]
    public async Task ExportedPackage_CanBeImportedIntoAnEmptySite()
    {
        _fixture.AddDataEditor(UmbracoModels.TextBoxEditorAlias);

        var result = await _fixture.Service.ImportPackageAsync(Packages.Sample());

        AssertCounts(result.DataTypes, created: 1);
        AssertCounts(result.Compositions, created: 1);
        AssertCounts(result.ElementTypes, created: 1);
        AssertCounts(result.Definitions, created: 2);
        AssertCounts(result.PartialViews, created: 1);
    }
}
