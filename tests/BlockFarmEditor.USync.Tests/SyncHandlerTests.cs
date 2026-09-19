using System.Reflection;
using BlockFarmEditor.Umbraco.Core.DTO;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.USync.BlockFarmEditorLayouts;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Persistence;
using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;
using uSync.Core.Serialization;
using static Umbraco.Cms.Core.Constants;

namespace BlockFarmEditor.USync.Tests;

/// <summary>uSync's handler base class needs its collaborators to exist, but none of them matter to these tests.</summary>
internal sealed class HandlerDependencies<TObject>
{
    public Mock<IUmbracoDatabase> Database { get; } = new();
    public Mock<IUmbracoDatabaseFactory> DatabaseFactory { get; } = new();
    public Mock<ISyncItemFactory> ItemFactory { get; } = new() { DefaultValue = DefaultValue.Mock };
    public Mock<ISyncConfigService> Config { get; } = new() { DefaultValue = DefaultValue.Mock };
    public IShortStringHelper ShortStringHelper { get; } = Mock.Of<IShortStringHelper>();
    public ISyncFileService FileService { get; } = Mock.Of<ISyncFileService>();
    public ISyncEventService EventService { get; } = Mock.Of<ISyncEventService>();
    public NullLogger<SyncHandlerRoot<TObject, TObject>> Logger { get; } = NullLogger<SyncHandlerRoot<TObject, TObject>>.Instance;

    public HandlerDependencies()
    {
        DatabaseFactory.Setup(x => x.CreateDatabase()).Returns(Database.Object);
        ItemFactory.Setup(x => x.GetSerializers<TObject>()).Returns([Mock.Of<ISyncSerializer<TObject>>()]);
    }
}

public class BlockFarmEditorDefinitionUsyncHandlerTests
{
    // uSync reads [SyncHandler] from the concrete type (not inherited), so the test subclass repeats it.
    [SyncHandler("TestDefinitionHandler", "Test Definitions", "TestDefinitions", 5000, EntityType = UdiEntityType.Unknown)]
    private sealed class TestableHandler(HandlerDependencies<BlockFarmEditorDefinitionDTO> dependencies, IBlockFarmEditorDefinitionService definitionService)
        : BlockFarmEditorDefinitionUsyncHandler(dependencies.Logger, AppCaches.Disabled, dependencies.ShortStringHelper, dependencies.FileService, dependencies.EventService, dependencies.Config.Object, dependencies.ItemFactory.Object, definitionService, dependencies.DatabaseFactory.Object)
    {
        public Task<IEnumerable<BlockFarmEditorDefinitionDTO>> ChildItems(BlockFarmEditorDefinitionDTO? parent) => GetChildItemsAsync(parent);
        public Task<IEnumerable<BlockFarmEditorDefinitionDTO>> Folders(BlockFarmEditorDefinitionDTO? parent) => GetFoldersAsync(parent);
        public Task<BlockFarmEditorDefinitionDTO?> FromService(BlockFarmEditorDefinitionDTO? item) => GetFromServiceAsync(item);
        public string ItemName(BlockFarmEditorDefinitionDTO item) => GetItemName(item);
        public Task<IEnumerable<uSyncAction>> DeleteMissing(BlockFarmEditorDefinitionDTO parent, IEnumerable<Guid> keysToKeep) => DeleteMissingItemsAsync(parent, keysToKeep, reportOnly: false);
    }

    private readonly HandlerDependencies<BlockFarmEditorDefinitionDTO> _dependencies = new();
    private readonly Mock<IBlockFarmEditorDefinitionService> _definitionService = new();
    private readonly TestableHandler _handler;

    public BlockFarmEditorDefinitionUsyncHandlerTests()
    {
        _handler = new TestableHandler(_dependencies, _definitionService.Object);
    }

    [Fact]
    public void HandlerAttribute_RegistersTheDefinitionsFolder_AfterTheCoreUmbracoHandlers()
    {
        var attribute = typeof(BlockFarmEditorDefinitionUsyncHandler).GetCustomAttribute<SyncHandlerAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("BlockFarmEditorDefinitionUsyncHandler", attribute.Alias);
        Assert.Equal("BFE Definitions", attribute.Name);
        Assert.Equal(BlockFarmEditorDefinitionDTO.TableName, attribute.Folder);
        Assert.Equal(5000, attribute.Priority);
        Assert.Equal(UdiEntityType.Unknown, attribute.EntityType);
    }

    [Fact]
    public async Task ChildItems_AtTheRoot_AreAllDefinitions()
    {
        var definitions = new[] { Dtos.Definition("heroBlock"), Dtos.Definition("cardBlock") };
        _definitionService.Setup(x => x.GetAllAsync(_dependencies.Database.Object)).ReturnsAsync(definitions.ToAsyncEnumerable());

        Assert.Equal(definitions, await _handler.ChildItems(null));
    }

    [Fact]
    public async Task ChildItems_WhenTheTableIsUnavailable_AreEmpty()
    {
        _definitionService.Setup(x => x.GetAllAsync(_dependencies.Database.Object)).ReturnsAsync((IAsyncEnumerable<BlockFarmEditorDefinitionDTO>?)null);

        Assert.Empty(await _handler.ChildItems(null));
    }

    [Fact]
    public async Task ChildItems_OfADefinition_AreEmpty_BecauseDefinitionsAreFlat()
    {
        Assert.Empty(await _handler.ChildItems(Dtos.Definition()));

        _definitionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Folders_DoNotExistForDefinitions()
    {
        Assert.Empty(await _handler.Folders(null));
        Assert.Empty(await _handler.Folders(Dtos.Definition()));
    }

    [Fact]
    public async Task FromService_ReloadsTheItemByKey()
    {
        var stale = Dtos.Definition();
        var fresh = Dtos.Definition();
        _definitionService.Setup(x => x.GetByKeyAsync(_dependencies.Database.Object, stale.Key)).ReturnsAsync(fresh);

        Assert.Same(fresh, await _handler.FromService(stale));
    }

    [Fact]
    public async Task FromService_WithoutAnItem_ReturnsNull()
    {
        Assert.Null(await _handler.FromService(null));

        _definitionService.VerifyNoOtherCalls();
    }

    [Fact]
    public void ItemName_IsTheContentTypeAlias()
    {
        Assert.Equal("heroBlock", _handler.ItemName(Dtos.Definition("heroBlock")));
    }

    [Fact]
    public async Task DeleteMissingItems_NeverDeletesAnything()
    {
        Assert.Empty(await _handler.DeleteMissing(Dtos.Definition(), []));

        _definitionService.VerifyNoOtherCalls();
    }
}

public class BlockFarmEditorLayoutUsyncHandlerTests
{
    // uSync reads [SyncHandler] from the concrete type (not inherited), so the test subclass repeats it.
    [SyncHandler("TestLayoutHandler", "Test Layouts", "TestLayouts", 5000, EntityType = UdiEntityType.Unknown)]
    private sealed class TestableHandler(HandlerDependencies<BlockFarmEditorLayoutDTO> dependencies, IBlockFarmEditorLayoutService layoutService)
        : BlockFarmEditorLayoutUsyncHandler(dependencies.Logger, AppCaches.Disabled, dependencies.ShortStringHelper, dependencies.FileService, dependencies.EventService, dependencies.Config.Object, dependencies.ItemFactory.Object, layoutService, dependencies.DatabaseFactory.Object)
    {
        public Task<IEnumerable<BlockFarmEditorLayoutDTO>> ChildItems(BlockFarmEditorLayoutDTO? parent) => GetChildItemsAsync(parent);
        public Task<IEnumerable<BlockFarmEditorLayoutDTO>> Folders(BlockFarmEditorLayoutDTO? parent) => GetFoldersAsync(parent);
        public Task<BlockFarmEditorLayoutDTO?> FromService(BlockFarmEditorLayoutDTO? item) => GetFromServiceAsync(item);
        public string ItemName(BlockFarmEditorLayoutDTO item) => GetItemName(item);
        public Task<IEnumerable<uSyncAction>> DeleteMissing(BlockFarmEditorLayoutDTO parent, IEnumerable<Guid> keysToKeep) => DeleteMissingItemsAsync(parent, keysToKeep, reportOnly: false);
    }

    private readonly HandlerDependencies<BlockFarmEditorLayoutDTO> _dependencies = new();
    private readonly Mock<IBlockFarmEditorLayoutService> _layoutService = new();
    private readonly TestableHandler _handler;

    public BlockFarmEditorLayoutUsyncHandlerTests()
    {
        _handler = new TestableHandler(_dependencies, _layoutService.Object);
    }

    [Fact]
    public void HandlerAttribute_RegistersTheLayoutsFolder_AfterTheCoreUmbracoHandlers()
    {
        var attribute = typeof(BlockFarmEditorLayoutUsyncHandler).GetCustomAttribute<SyncHandlerAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("BlockFarmEditorLayoutUsyncHandler", attribute.Alias);
        Assert.Equal("BFE Layouts", attribute.Name);
        Assert.Equal(BlockFarmEditorLayoutDTO.TableName, attribute.Folder);
        Assert.Equal(5000, attribute.Priority);
        Assert.Equal(UdiEntityType.Unknown, attribute.EntityType);
    }

    [Fact]
    public void Handlers_UseDistinctAliasesAndFolders()
    {
        var definition = typeof(BlockFarmEditorDefinitionUsyncHandler).GetCustomAttribute<SyncHandlerAttribute>()!;
        var layout = typeof(BlockFarmEditorLayoutUsyncHandler).GetCustomAttribute<SyncHandlerAttribute>()!;

        Assert.NotEqual(definition.Alias, layout.Alias);
        Assert.NotEqual(definition.Folder, layout.Folder);
    }

    [Fact]
    public async Task ChildItems_AtTheRoot_AreAllLayouts()
    {
        var layouts = new[] { Dtos.Layout("First"), Dtos.Layout("Second") };
        _layoutService.Setup(x => x.GetAllAsync(_dependencies.Database.Object)).ReturnsAsync(layouts.ToAsyncEnumerable());

        Assert.Equal(layouts, await _handler.ChildItems(null));
    }

    [Fact]
    public async Task ChildItems_WhenTheTableIsUnavailable_AreEmpty()
    {
        _layoutService.Setup(x => x.GetAllAsync(_dependencies.Database.Object)).ReturnsAsync((IAsyncEnumerable<BlockFarmEditorLayoutDTO>?)null);

        Assert.Empty(await _handler.ChildItems(null));
    }

    [Fact]
    public async Task ChildItems_OfALayout_AreEmpty_BecauseLayoutsAreFlat()
    {
        Assert.Empty(await _handler.ChildItems(Dtos.Layout()));

        _layoutService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Folders_DoNotExistForLayouts()
    {
        Assert.Empty(await _handler.Folders(null));
        Assert.Empty(await _handler.Folders(Dtos.Layout()));
    }

    [Fact]
    public async Task FromService_ReloadsTheItemByKey()
    {
        var stale = Dtos.Layout();
        var fresh = Dtos.Layout();
        _layoutService.Setup(x => x.GetByKeyAsync(_dependencies.Database.Object, stale.Key)).ReturnsAsync(fresh);

        Assert.Same(fresh, await _handler.FromService(stale));
    }

    [Fact]
    public async Task FromService_WithoutAnItem_ReturnsNull()
    {
        Assert.Null(await _handler.FromService(null));

        _layoutService.VerifyNoOtherCalls();
    }

    [Fact]
    public void ItemName_IsTheLayoutName()
    {
        Assert.Equal("Two Column", _handler.ItemName(Dtos.Layout("Two Column")));
    }

    [Fact]
    public async Task DeleteMissingItems_NeverDeletesAnything()
    {
        Assert.Empty(await _handler.DeleteMissing(Dtos.Layout(), []));

        _layoutService.VerifyNoOtherCalls();
    }
}
