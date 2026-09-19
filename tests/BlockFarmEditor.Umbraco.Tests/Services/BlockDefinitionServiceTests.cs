using System.Linq.Expressions;
using BlockFarmEditor.Umbraco.Core.DTO;
using BlockFarmEditor.Umbraco.Core.Models.ConfigModels;
using BlockFarmEditor.Umbraco.Library.Services;
using BlockFarmEditor.Umbraco.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NPoco.Linq;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Infrastructure.Persistence;

namespace BlockFarmEditor.Umbraco.Tests.Services;

public class BlockDefinitionServiceTests
{
    public const string HeroAlias = "bfeTestHeroBlock";
    public const string CardAlias = "bfeTestCardBlock";
    public const string PlainAlias = "bfeTestPlainBlock";

    public class HeroViewComponent { }
    public class DuplicateHeroViewComponent { }

    public interface IThemeSource { IEnumerable<string> Themes { get; } }

    public class ThemeConfig(IThemeSource source) : IBlockFarmEditorConfig
    {
        public Task<IEnumerable<BlockFarmEditorConfigItem>> GetItems() =>
            Task.FromResult<IEnumerable<BlockFarmEditorConfigItem>>([new BlockFarmEditorConfigItem { Alias = "items", Value = source.Themes }]);
    }

    public class SizeConfig : IBlockFarmEditorConfig
    {
        public Task<IEnumerable<BlockFarmEditorConfigItem>> GetItems() => Task.FromResult<IEnumerable<BlockFarmEditorConfigItem>>([]);
    }

    private readonly BlockFarmServiceProvider _services;
    private readonly Mock<IUmbracoDatabase> _database = new();
    private readonly Mock<ICoreScope> _scope = new();
    private readonly List<BlockFarmEditorDefinitionDTO> _rows = [];
    private readonly List<IContentType> _contentTypes = [];
    private readonly BlockDefinitionService _service;
    private int _queryCount;

    public BlockDefinitionServiceTests()
    {
        var themes = new Mock<IThemeSource>();
        themes.SetupGet(x => x.Themes).Returns(["light", "dark"]);
        _services = new BlockFarmServiceProvider(services => services.AddSingleton(themes.Object));
        // GetAllElementTypes() is an extension method over GetAll() that keeps the element types
        _services.ContentTypeService.Setup(x => x.GetAll()).Returns(() => _contentTypes);

        // Stand-in for NPoco's LINQ provider: applies the service's filter to the in-memory rows.
        var filtered = new Mock<IQueryProvider<BlockFarmEditorDefinitionDTO>>();
        var query = new Mock<IQueryProviderWithIncludes<BlockFarmEditorDefinitionDTO>>();
        query
            .Setup(x => x.Where(It.IsAny<Expression<Func<BlockFarmEditorDefinitionDTO, bool>>>()))
            .Returns((Expression<Func<BlockFarmEditorDefinitionDTO, bool>> predicate) =>
            {
                _queryCount++;
                filtered.Setup(x => x.ToList()).Returns(_rows.Where(predicate.Compile()).ToList());
                return filtered.Object;
            });
        _database.Setup(x => x.Query<BlockFarmEditorDefinitionDTO>()).Returns(query.Object);

        var scopeProvider = new Mock<ICoreScopeProvider> { DefaultValue = DefaultValue.Mock };
        scopeProvider.SetReturnsDefault(_scope.Object);

        var typeFinder = new Mock<ITypeFinder>();
        typeFinder.SetupGet(x => x.AssembliesToScan).Returns([typeof(BlockDefinitionServiceTests).Assembly, typeof(BlockDefinitionService).Assembly]);

        _service = new BlockDefinitionService(
            _services.Provider,
            _services.DataTypeService.Object,
            NullLogger<BlockDefinitionService>.Instance,
            scopeProvider.Object,
            _database.ToFactory().Object,
            _services.ContentTypeService.Object,
            typeFinder.Object);
    }

    /// <summary>Adds an enabled definition row plus the element type it points at.</summary>
    private Mock<IContentType> AddBlock(string alias, IEnumerable<PropertyGroup>? groups = null, IEnumerable<IPropertyType>? noGroupPropertyTypes = null, bool enabled = true)
    {
        var contentType = UmbracoModels.ContentType(alias, groups: groups, noGroupPropertyTypes: noGroupPropertyTypes);
        _contentTypes.Add(contentType.Object);
        _rows.Add(Dtos.Definition(alias, enabled: enabled));
        return contentType;
    }

    private IPropertyType Property(string alias, IDictionary<string, object>? configuration = null)
    {
        var dataTypeKey = Guid.NewGuid();
        _services.DataTypeService.Setup(x => x.GetAsync(dataTypeKey)).ReturnsAsync(UmbracoModels.DataType(dataTypeKey, configuration: configuration).Object);
        return UmbracoModels.PropertyType(alias, dataTypeKey);
    }

    #region RetrieveBlockFarmEditorDefinitions

    [Fact]
    public void Retrieve_ReturnsEnabledDefinitions_KeyedByElementTypeKey()
    {
        var hero = AddBlock(HeroAlias);
        var plain = AddBlock(PlainAlias);

        var definitions = _service.RetrieveBlockFarmEditorDefinitions();

        Assert.Equal(2, definitions.Count);
        Assert.Equal(HeroAlias, definitions[hero.Object.Key].ContentTypeAlias);
        Assert.Same(hero.Object, definitions[hero.Object.Key].ContentType);
        Assert.Same(plain.Object, definitions[plain.Object.Key].ContentType);
    }

    [Fact]
    public void Retrieve_ExcludesDisabledDefinitions()
    {
        AddBlock(HeroAlias, enabled: false);
        var plain = AddBlock(PlainAlias);

        var definitions = _service.RetrieveBlockFarmEditorDefinitions();

        Assert.Equal([plain.Object.Key], definitions.Keys);
    }

    [Fact]
    public void Retrieve_ExcludesDefinitionsWhoseElementTypeNoLongerExists()
    {
        _rows.Add(Dtos.Definition("deletedElementType"));
        var plain = AddBlock(PlainAlias);

        var definitions = _service.RetrieveBlockFarmEditorDefinitions();

        Assert.Equal([plain.Object.Key], definitions.Keys);
    }

    [Fact]
    public void Retrieve_ExcludesDefinitionsPointingAtADocumentType()
    {
        var document = AddBlock(HeroAlias);
        document.Object.IsElement = false;
        var plain = AddBlock(PlainAlias);

        var definitions = _service.RetrieveBlockFarmEditorDefinitions();

        Assert.Equal([plain.Object.Key], definitions.Keys);
    }

    [Fact]
    public void Retrieve_AttachesTheDefinitionAttribute_FirstDeclarationWinsOnDuplicates()
    {
        var hero = AddBlock(HeroAlias);
        var plain = AddBlock(PlainAlias);

        var definitions = _service.RetrieveBlockFarmEditorDefinitions();

        Assert.Equal(typeof(HeroViewComponent), definitions[hero.Object.Key].DefinitionAttribute?.ViewComponentType);
        Assert.Null(definitions[plain.Object.Key].DefinitionAttribute);
    }

    [Fact]
    public void Retrieve_AttachesPropertyConfigsForTheAlias()
    {
        var hero = AddBlock(HeroAlias);
        var plain = AddBlock(PlainAlias);

        var definitions = _service.RetrieveBlockFarmEditorDefinitions();

        Assert.Equal(["size", "theme"], definitions[hero.Object.Key].PropertyConfigs.Select(x => x.PropertyAlias).Order());
        Assert.Empty(definitions[plain.Object.Key].PropertyConfigs);
    }

    [Fact]
    public void Retrieve_CompletesItsDatabaseScope()
    {
        AddBlock(PlainAlias);

        _service.RetrieveBlockFarmEditorDefinitions();

        _scope.Verify(x => x.Complete(), Times.Once);
    }

    [Fact]
    public void Retrieve_CachesTheResult()
    {
        AddBlock(PlainAlias);

        var first = _service.RetrieveBlockFarmEditorDefinitions();
        AddBlock(CardAlias);
        var second = _service.RetrieveBlockFarmEditorDefinitions();

        Assert.Same(first, second);
        Assert.Single(second);
        Assert.Equal(1, _queryCount);
    }

    [Fact]
    public void Retrieve_WithForce_ReloadsFromTheDatabase()
    {
        AddBlock(PlainAlias);
        _service.RetrieveBlockFarmEditorDefinitions();
        AddBlock(CardAlias);

        var reloaded = _service.RetrieveBlockFarmEditorDefinitions(force: true);

        Assert.Equal(2, reloaded.Count);
        Assert.Equal(2, _queryCount);
    }

    [Fact]
    public void ClearCache_MakesTheNextRetrieveReload()
    {
        AddBlock(PlainAlias);
        _service.RetrieveBlockFarmEditorDefinitions();
        AddBlock(CardAlias);

        _service.ClearCache();

        Assert.Equal(2, _service.RetrieveBlockFarmEditorDefinitions().Count);
    }

    #endregion

    #region GetConfigMaps

    [Fact]
    public void GetConfigMaps_GroupsConfigurationAttributesByBlockAlias()
    {
        var maps = _service.GetConfigMaps();

        Assert.Equal(["size", "theme"], maps[HeroAlias].Select(x => x.PropertyAlias).Order());
        Assert.Equal(typeof(ThemeConfig), Assert.Single(maps[CardAlias]).GetConfigType);
        Assert.False(maps.ContainsKey(PlainAlias));
    }

    [Fact]
    public void GetConfigMaps_ReturnsACopy_SoCallersCannotCorruptTheCache()
    {
        _service.GetConfigMaps().Clear();

        Assert.NotEmpty(_service.GetConfigMaps());
    }

    #endregion

    #region RetrievePropertyEditors

    [Fact]
    public async Task PropertyEditors_ForAnUnknownContentType_AreEmpty()
    {
        AddBlock(PlainAlias);

        Assert.Empty(await _service.RetrievePropertyEditors(Guid.NewGuid()));
    }

    [Fact]
    public async Task PropertyEditors_ForATypeWithoutProperties_AreEmpty()
    {
        var block = AddBlock(PlainAlias);

        Assert.Empty(await _service.RetrievePropertyEditors(block.Object.Key));
    }

    [Fact]
    public async Task PropertyEditors_WithoutAnyGroups_AreCollectedIntoASyntheticPropertiesGroup()
    {
        var block = AddBlock(PlainAlias, noGroupPropertyTypes: [Property("title"), Property("subtitle")]);

        var group = Assert.Single(await _service.RetrievePropertyEditors(block.Object.Key));

        Assert.Equal("properties", group.Alias);
        Assert.Equal("Properties", group.Label);
        Assert.Equal(PropertyGroupType.Group, group.Type);
        Assert.Equal(["title", "subtitle"], group.Editors!.Keys);
        Assert.Null(group.Groups);
    }

    [Fact]
    public async Task PropertyEditors_WithGroupsOnly_ReturnsEachGroupWithItsEditors()
    {
        var block = AddBlock(PlainAlias, groups:
        [
            UmbracoModels.Group("content", "Content", Property("title")),
            UmbracoModels.Group("settings", "Settings", Property("anchor"), Property("hidden"))
        ]);

        var groups = (await _service.RetrievePropertyEditors(block.Object.Key)).ToList();

        Assert.Equal(["content", "settings"], groups.Select(x => x.Alias));
        Assert.Equal(["Content", "Settings"], groups.Select(x => x.Label));
        Assert.All(groups, x => Assert.Equal(PropertyGroupType.Group, x.Type));
        Assert.Equal(["title"], groups[0].Editors!.Keys);
        Assert.Equal(["anchor", "hidden"], groups[1].Editors!.Keys);
    }

    [Fact]
    public async Task PropertyEditors_GroupsWithoutProperties_AreSkipped()
    {
        var block = AddBlock(PlainAlias, groups:
        [
            UmbracoModels.Group("content", "Content", Property("title")),
            UmbracoModels.Group("empty", "Empty")
        ]);

        var groups = await _service.RetrievePropertyEditors(block.Object.Key);

        Assert.Equal(["content"], groups.Select(x => x.Alias));
    }

    [Fact]
    public async Task PropertyEditors_WithTabs_NestsEachTabsGroups_AndKeepsTabLevelProperties()
    {
        var block = AddBlock(PlainAlias, groups:
        [
            UmbracoModels.Tab("content", "Content", Property("intro")),
            UmbracoModels.Group("content/text", "Text", Property("title")),
            UmbracoModels.Group("content/media", "Media", Property("image")),
            UmbracoModels.Tab("settings", "Settings"),
            UmbracoModels.Group("settings/advanced", "Advanced", Property("anchor"))
        ]);

        var tabs = (await _service.RetrievePropertyEditors(block.Object.Key)).ToList();

        Assert.Equal(["content", "settings"], tabs.Select(x => x.Alias));
        Assert.All(tabs, x => Assert.Equal(PropertyGroupType.Tab, x.Type));

        Assert.Equal(["intro"], tabs[0].Editors!.Keys);
        Assert.Equal(["content/text", "content/media"], tabs[0].Groups!.Select(x => x.Alias));
        Assert.Equal(["title"], tabs[0].Groups!.First().Editors!.Keys);

        Assert.Null(tabs[1].Editors);
        var advanced = Assert.Single(tabs[1].Groups!);
        Assert.Equal("Advanced", advanced.Label);
        Assert.Equal(["anchor"], advanced.Editors!.Keys);
    }

    [Fact]
    public async Task PropertyEditors_WithTabsAndRootLevelGroups_PutsThoseGroupsInALeadingGenericTab()
    {
        var block = AddBlock(PlainAlias, groups:
        [
            UmbracoModels.Tab("content", "Content"),
            UmbracoModels.Group("content/text", "Text", Property("title")),
            UmbracoModels.Group("seo", "SEO", Property("metaTitle"))
        ]);

        var tabs = (await _service.RetrievePropertyEditors(block.Object.Key)).ToList();

        Assert.Equal(["orphaned", "content"], tabs.Select(x => x.Alias));
        Assert.Equal("Generic", tabs[0].Label);
        Assert.Equal(PropertyGroupType.Tab, tabs[0].Type);
        Assert.Equal(["seo"], tabs[0].Groups!.Select(x => x.Alias));
        Assert.Equal(["content/text"], tabs[1].Groups!.Select(x => x.Alias));
    }

    [Fact]
    public async Task PropertyEditors_IncludeGroupsInheritedFromCompositions()
    {
        var composition = UmbracoModels.ContentType("seoComposition", groups: [UmbracoModels.Group("seo", "SEO", Property("metaTitle"))]);
        var contentType = UmbracoModels.ContentType(PlainAlias, groups: [UmbracoModels.Group("content", "Content", Property("title"))], compositions: [composition.Object]);
        _contentTypes.Add(contentType.Object);
        _rows.Add(Dtos.Definition(PlainAlias));

        var groups = await _service.RetrievePropertyEditors(contentType.Object.Key);

        Assert.Equal(["content", "seo"], groups.Select(x => x.Alias));
    }

    [Fact]
    public async Task PropertyEditors_MapEditorAliasLabelDescriptionAndValidation()
    {
        var property = Property("title");
        property.Name = "Title";
        property.Description = "Shown at the top";
        property.Mandatory = true;
        property.MandatoryMessage = "Please add a title";
        property.ValidationRegExp = "^.{0,50}$";
        property.ValidationRegExpMessage = "Too long";
        var block = AddBlock(PlainAlias, noGroupPropertyTypes: [property]);

        var editor = (await _service.RetrievePropertyEditors(block.Object.Key)).Single().Editors!["title"];

        Assert.Equal(UmbracoModels.TextBoxEditorAlias, editor.Alias);
        Assert.Equal("Title", editor.Label);
        Assert.Equal("Shown at the top", editor.Description);
        Assert.NotNull(editor.Validation);
        Assert.True(editor.Validation.Mandatory);
        Assert.Equal("Please add a title", editor.Validation.MandatoryMessage);
        Assert.Equal("^.{0,50}$", editor.Validation.RegularExpression);
        Assert.Equal("Too long", editor.Validation.RegularExpressionMessage);
    }

    [Fact]
    public async Task PropertyEditors_UseTheDataTypesConfiguration_ByDefault()
    {
        var block = AddBlock(PlainAlias, noGroupPropertyTypes: [Property("title", new Dictionary<string, object> { ["maxChars"] = 50, ["placeholder"] = "Type here" })]);

        var editor = (await _service.RetrievePropertyEditors(block.Object.Key)).Single().Editors!["title"];

        Assert.Equal(new Dictionary<string, object?> { ["maxChars"] = 50, ["placeholder"] = "Type here" }, editor.Configurations.ToDictionary(x => x.Alias, x => x.Value));
    }

    [Fact]
    public async Task PropertyEditors_WithARegisteredBlockFarmEditorConfig_UseItsItems_ResolvedThroughDependencyInjection()
    {
        var block = AddBlock(HeroAlias, noGroupPropertyTypes:
        [
            Property("theme", new Dictionary<string, object> { ["fromDataType"] = true }),
            Property("title", new Dictionary<string, object> { ["fromDataType"] = true })
        ]);

        var editors = (await _service.RetrievePropertyEditors(block.Object.Key)).Single().Editors!;

        var themeConfiguration = Assert.Single(editors["theme"].Configurations);
        Assert.Equal("items", themeConfiguration.Alias);
        Assert.Equal(["light", "dark"], Assert.IsAssignableFrom<IEnumerable<string>>(themeConfiguration.Value));
        Assert.Equal("fromDataType", Assert.Single(editors["title"].Configurations).Alias);
    }

    [Fact]
    public async Task PropertyEditors_SkipPropertiesWhoseDataTypeIsMissing()
    {
        var block = AddBlock(PlainAlias, noGroupPropertyTypes: [Property("title"), UmbracoModels.PropertyType("orphan")]);

        var editors = (await _service.RetrievePropertyEditors(block.Object.Key)).Single().Editors!;

        Assert.Equal(["title"], editors.Keys);
    }

    #endregion
}
