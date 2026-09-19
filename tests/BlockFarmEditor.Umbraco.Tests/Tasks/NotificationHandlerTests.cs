using BlockFarmEditor.Umbraco.Core.DTO;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Library.Tasks;
using BlockFarmEditor.Umbraco.Tests.Helpers;
using NPoco;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Cms.Infrastructure.Persistence;

namespace BlockFarmEditor.Umbraco.Tests.Tasks;

public class BlockFarmEditorDefinitionRefreshTests
{
    private readonly Mock<IBlockDefinitionService> _blockDefinitionService = new();

    private static ContentTypeChange<IContentType> Change(bool isElement)
    {
        var contentType = new Mock<IContentType>();
        contentType.SetupGet(x => x.IsElement).Returns(isElement);
        return new ContentTypeChange<IContentType>(contentType.Object, ContentTypeChangeTypes.RefreshMain);
    }

    private Task Handle(params ContentTypeChange<IContentType>[] changes) =>
        new BlockFarmEditorDefinitionRefresh(_blockDefinitionService.Object)
            .HandleAsync(new ContentTypeChangedNotification(changes, new EventMessages()), CancellationToken.None);

    [Fact]
    public async Task WhenAnElementTypeChanged_ClearsTheDefinitionCache()
    {
        await Handle(Change(isElement: false), Change(isElement: true));

        _blockDefinitionService.Verify(x => x.ClearCache(), Times.Once);
    }

    [Fact]
    public async Task WhenOnlyDocumentTypesChanged_LeavesTheCacheAlone()
    {
        await Handle(Change(isElement: false));

        _blockDefinitionService.Verify(x => x.ClearCache(), Times.Never);
    }

    [Fact]
    public async Task WhenNothingChanged_LeavesTheCacheAlone()
    {
        await Handle();

        _blockDefinitionService.Verify(x => x.ClearCache(), Times.Never);
    }
}

public class BlockFarmEditorDefinitionCleanUpTests
{
    private readonly Mock<IUmbracoDatabase> _database = new();
    private readonly Mock<ICoreScopeProvider> _scopeProvider = new() { DefaultValue = DefaultValue.Mock };

    private static IContentType ContentType(string alias)
    {
        var contentType = new Mock<IContentType>();
        contentType.SetupGet(x => x.Alias).Returns(alias);
        return contentType.Object;
    }

    private void ExistingDefinition(BlockFarmEditorDefinitionDTO definition) =>
        _database
            .Setup(x => x.FirstOrDefaultAsync<BlockFarmEditorDefinitionDTO>(
                It.Is<string>(sql => sql.Contains("FROM BlockFarmEditorDefinition WHERE ContentTypeAlias = @0")),
                It.Is<object[]>(args => args.Length == 1 && Equals(args[0], definition.ContentTypeAlias)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

    private Task Handle(params IContentType[] deleted) =>
        new BlockFarmEditorDefinitionCleanUp(_scopeProvider.Object, _database.ToFactory().Object)
            .HandleAsync(new ContentTypeDeletedNotification(deleted, new EventMessages()), CancellationToken.None);

    [Fact]
    public async Task DeletesTheDefinitionLinkedToADeletedContentType()
    {
        var definition = Dtos.Definition("heroBlock");
        ExistingDefinition(definition);

        await Handle(ContentType("heroBlock"));

        _database.Verify(x => x.DeleteAsync(definition, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IgnoresDeletedContentTypesWithoutADefinition()
    {
        ExistingDefinition(Dtos.Definition("heroBlock"));

        await Handle(ContentType("somethingElse"));

        _database.Verify(x => x.DeleteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandlesEveryDeletedContentTypeInTheNotification()
    {
        var hero = Dtos.Definition("heroBlock");
        var card = Dtos.Definition("cardBlock");
        ExistingDefinition(hero);
        ExistingDefinition(card);

        await Handle(ContentType("heroBlock"), ContentType("noDefinition"), ContentType("cardBlock"));

        _database.Verify(x => x.DeleteAsync(hero, It.IsAny<CancellationToken>()), Times.Once);
        _database.Verify(x => x.DeleteAsync(card, It.IsAny<CancellationToken>()), Times.Once);
    }
}
