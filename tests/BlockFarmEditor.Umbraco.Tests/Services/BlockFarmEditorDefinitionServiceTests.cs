using BlockFarmEditor.Umbraco.Core.DTO;
using BlockFarmEditor.Umbraco.Library.Services;
using BlockFarmEditor.Umbraco.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Infrastructure.Persistence;

namespace BlockFarmEditor.Umbraco.Tests.Services;

public class BlockFarmEditorDefinitionServiceTests
{
    private readonly Mock<IUmbracoDatabase> _database = new();
    private readonly BlockFarmEditorDefinitionService _service = new(NullLogger<BlockFarmEditorDefinitionService>.Instance);

    [Fact]
    public async Task GetAllAsync_QueriesTheDefinitionTable()
    {
        var rows = new[] { Dtos.Definition("a"), Dtos.Definition("b") };
        _database.SetupQuery("SELECT * FROM BlockFarmEditorDefinition", rows);

        var result = await _service.GetAllAsync(_database.Object);

        Assert.NotNull(result);
        Assert.Equal(rows, await result.ToListAsync());
    }

    [Fact]
    public async Task GetByAliasAsync_FiltersOnContentTypeAlias()
    {
        var definition = Dtos.Definition("heroBlock");
        _database.SetupSingleOrDefault("FROM BlockFarmEditorDefinition WHERE ContentTypeAlias = @0", "heroBlock", definition);

        Assert.Same(definition, await _service.GetByAliasAsync(_database.Object, "heroBlock"));
        Assert.Null(await _service.GetByAliasAsync(_database.Object, "missing"));
    }

    [Fact]
    public async Task GetByKeyAsync_FiltersOnKey()
    {
        var definition = Dtos.Definition();
        _database.SetupSingleOrDefault("FROM BlockFarmEditorDefinition WHERE [Key] = @0", definition.Key, definition);

        Assert.Same(definition, await _service.GetByKeyAsync(_database.Object, definition.Key));
        Assert.Null(await _service.GetByKeyAsync(_database.Object, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetCategories_SelectsDistinctCategories()
    {
        _database.SetupQuery("SELECT DISTINCT Category FROM BlockFarmEditorDefinition", Dtos.Definition(category: "Content"), Dtos.Definition(category: "Containers"));

        var result = await _service.GetCategories(_database.Object);

        Assert.Equal(["Content", "Containers"], result);
    }

    [Fact]
    public async Task CreateAsync_InsertsACopyStampedWithTheCreatingUser()
    {
        var user = Guid.NewGuid();
        var dto = Dtos.Definition();
        BlockFarmEditorDefinitionDTO? inserted = null;
        _database
            .Setup(x => x.InsertAsync(It.IsAny<BlockFarmEditorDefinitionDTO>(), It.IsAny<CancellationToken>()))
            .Callback((BlockFarmEditorDefinitionDTO poco, CancellationToken _) => inserted = poco)
            .ReturnsAsync(42);

        var result = await _service.CreateAsync(_database.Object, dto, user);

        Assert.NotNull(inserted);
        Assert.Equal(dto.Key, inserted.Key);
        Assert.Equal(dto.ContentTypeAlias, inserted.ContentTypeAlias);
        Assert.Equal(dto.Type, inserted.Type);
        Assert.Equal(dto.ViewPath, inserted.ViewPath);
        Assert.Equal(dto.Category, inserted.Category);
        Assert.Equal(dto.Enabled, inserted.Enabled);
        Assert.Equal(user, inserted.CreatedBy);
        Assert.Equal(user, inserted.UpdatedBy);

        Assert.Same(dto, result);
        Assert.Equal(42, result.Id);
    }

    [Fact]
    public async Task CreateAsync_AcceptsNonIntIdentityValues()
    {
        // SQL Server returns the identity as a decimal, SQLite as a long.
        _database
            .Setup(x => x.InsertAsync(It.IsAny<BlockFarmEditorDefinitionDTO>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(9m);

        var result = await _service.CreateAsync(_database.Object, Dtos.Definition(), Guid.NewGuid());

        Assert.Equal(9, result.Id);
    }

    [Fact]
    public async Task UpdateAsync_WhenDefinitionDoesNotExist_ReturnsNull_AndWritesNothing()
    {
        var result = await _service.UpdateAsync(_database.Object, 5, "partial", "view", "cat", true, Guid.NewGuid());

        Assert.Null(result);
        _database.Verify(x => x.UpdateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_AppliesChanges_AndReturnsTheUpdatedValues()
    {
        var user = Guid.NewGuid();
        var originalUpdateDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var existing = Dtos.Definition(alias: "heroBlock", category: "Old", type: "partial", viewPath: "old.cshtml", enabled: false, id: 5);
        existing.UpdateDate = originalUpdateDate;
        existing.CreateDate = originalUpdateDate;
        _database.SetupSingleOrDefault("FROM BlockFarmEditorDefinition WHERE [Id] = @0", 5, existing);

        var result = await _service.UpdateAsync(_database.Object, 5, "viewcomponent", "new.cshtml", "New", true, user);

        _database.Verify(x => x.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(result);
        Assert.NotSame(existing, result);
        Assert.Equal(5, result.Id);
        Assert.Equal(existing.Key, result.Key);
        Assert.Equal("heroBlock", result.ContentTypeAlias);
        Assert.Equal("viewcomponent", result.Type);
        Assert.Equal("new.cshtml", result.ViewPath);
        Assert.Equal("New", result.Category);
        Assert.True(result.Enabled);
        Assert.Equal(user, result.UpdatedBy);
        Assert.Equal(originalUpdateDate, result.CreateDate);
        Assert.True(result.UpdateDate > originalUpdateDate);

        // the persisted row carries the same changes
        Assert.Equal("viewcomponent", existing.Type);
        Assert.Equal(user, existing.UpdatedBy);
    }

    [Fact]
    public async Task DeleteAsync_WhenDefinitionExists_DeletesIt()
    {
        var definition = Dtos.Definition("heroBlock");
        _database.SetupSingleOrDefault("WHERE ContentTypeAlias = @0", "heroBlock", definition);

        await _service.DeleteAsync(_database.Object, "heroBlock");

        _database.Verify(x => x.DeleteAsync(definition, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenDefinitionDoesNotExist_DoesNothing()
    {
        await _service.DeleteAsync(_database.Object, "missing");

        _database.Verify(x => x.DeleteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
