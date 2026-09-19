using BlockFarmEditor.Umbraco.Core.DTO;
using BlockFarmEditor.Umbraco.Library.Services;
using BlockFarmEditor.Umbraco.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Infrastructure.Persistence;

namespace BlockFarmEditor.Umbraco.Tests.Services;

public class BlockFarmEditorLayoutServiceTests
{
    private readonly Mock<IUmbracoDatabase> _database = new();
    private readonly BlockFarmEditorLayoutService _service = new(NullLogger<BlockFarmEditorLayoutService>.Instance);

    [Fact]
    public async Task GetAllAsync_QueriesTheLayoutTable()
    {
        var rows = new[] { Dtos.Layout("a"), Dtos.Layout("b") };
        _database.SetupQuery("SELECT * FROM BlockFarmEditorLayout", rows);

        var result = await _service.GetAllAsync(_database.Object);

        Assert.NotNull(result);
        Assert.Equal(rows, await result.ToListAsync());
    }

    [Fact]
    public async Task GetByKeyAsync_FiltersOnKey()
    {
        var layout = Dtos.Layout();
        _database.SetupSingleOrDefault("FROM BlockFarmEditorLayout WHERE [Key] = @0", layout.Key, layout);

        Assert.Same(layout, await _service.GetByKeyAsync(_database.Object, layout.Key));
        Assert.Null(await _service.GetByKeyAsync(_database.Object, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetCategories_SelectsDistinctCategories()
    {
        _database.SetupQuery("SELECT DISTINCT Category FROM BlockFarmEditorLayout", Dtos.Layout(category: "Heroes"), Dtos.Layout(category: "Grids"));

        var result = await _service.GetCategories(_database.Object);

        Assert.Equal(["Heroes", "Grids"], result);
    }

    [Fact]
    public async Task CreateAsync_InsertsACopyStampedWithTheCreatingUser()
    {
        var user = Guid.NewGuid();
        var dto = Dtos.Layout();
        dto.Enabled = false;
        BlockFarmEditorLayoutDTO? inserted = null;
        _database
            .Setup(x => x.InsertAsync(It.IsAny<BlockFarmEditorLayoutDTO>(), It.IsAny<CancellationToken>()))
            .Callback((BlockFarmEditorLayoutDTO poco, CancellationToken _) => inserted = poco)
            .ReturnsAsync(17L);

        var result = await _service.CreateAsync(_database.Object, dto, user);

        Assert.NotNull(inserted);
        Assert.Equal(dto.Key, inserted.Key);
        Assert.Equal(dto.Name, inserted.Name);
        Assert.Equal(dto.Description, inserted.Description);
        Assert.Equal(dto.Layout, inserted.Layout);
        Assert.Equal(dto.Category, inserted.Category);
        Assert.Equal(dto.Type, inserted.Type);
        Assert.Equal(dto.Icon, inserted.Icon);
        Assert.False(inserted.Enabled);
        Assert.Equal(user, inserted.CreatedBy);
        Assert.Equal(user, inserted.UpdatedBy);

        Assert.Same(dto, result);
        Assert.Equal(17, result.Id);
    }

    [Fact]
    public async Task UpdateAsync_WhenLayoutDoesNotExist_ReturnsNull_AndWritesNothing()
    {
        var result = await _service.UpdateAsync(_database.Object, 5, Dtos.Layout(), Guid.NewGuid());

        Assert.Null(result);
        _database.Verify(x => x.UpdateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_PersistsTheSuppliedDto_UnderTheGivenId()
    {
        var existing = Dtos.Layout("Old name", id: 5);
        var update = Dtos.Layout("New name", id: 999, key: existing.Key);
        _database.SetupSingleOrDefault("FROM BlockFarmEditorLayout WHERE [Id] = @0", 5, existing);

        var result = await _service.UpdateAsync(_database.Object, 5, update, Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Equal(5, update.Id);
        _database.Verify(x => x.UpdateAsync(update, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(5, result.Id);
        Assert.Equal(existing.Key, result.Key);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsTheUpdatedValues()
    {
        var existing = Dtos.Layout("Old name", category: "Old", id: 5);
        var update = Dtos.Layout("New name", category: "New", key: existing.Key);
        update.Description = "New description";
        update.Layout = "{\"blocks\":[1]}";
        update.Enabled = false;
        _database.SetupSingleOrDefault("WHERE [Id] = @0", 5, existing);

        var result = await _service.UpdateAsync(_database.Object, 5, update, Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Equal("New name", result.Name);
        Assert.Equal("New", result.Category);
        Assert.Equal("New description", result.Description);
        Assert.Equal("{\"blocks\":[1]}", result.Layout);
        Assert.False(result.Enabled);
    }

    [Fact]
    public async Task UpdateAsync_StampsTheUpdatingUserAndDate()
    {
        var user = Guid.NewGuid();
        var stale = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var existing = Dtos.Layout(id: 5);
        var update = Dtos.Layout(key: existing.Key);
        update.UpdateDate = stale;
        _database.SetupSingleOrDefault("WHERE [Id] = @0", 5, existing);

        await _service.UpdateAsync(_database.Object, 5, update, user);

        Assert.Equal(user, update.UpdatedBy);
        Assert.True(update.UpdateDate > stale);
    }

    [Fact]
    public async Task DeleteAsync_WhenLayoutExists_DeletesIt()
    {
        var layout = Dtos.Layout();
        _database.SetupSingleOrDefault("WHERE [Key] = @0", layout.Key, layout);

        await _service.DeleteAsync(_database.Object, layout.Key);

        _database.Verify(x => x.DeleteAsync(layout, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenLayoutDoesNotExist_DoesNothing()
    {
        await _service.DeleteAsync(_database.Object, Guid.NewGuid());

        _database.Verify(x => x.DeleteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
