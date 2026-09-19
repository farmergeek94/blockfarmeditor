using System.Reflection;
using System.Xml.Linq;
using BlockFarmEditor.Umbraco.Core.DTO;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.USync.BlockFarmEditorLayouts;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using uSync.Core;
using uSync.Core.Serialization;

namespace BlockFarmEditor.USync.Tests;

public class BlockFarmEditorLayoutSerializerTests
{
    private static readonly Guid AdminKey = Guid.NewGuid();

    private readonly Mock<IBlockFarmEditorLayoutService> _layoutService = new();
    private readonly Mock<IUmbracoDatabase> _database = new();
    private readonly Mock<IUserService> _userService = new();
    private readonly BlockFarmEditorLayoutSerializer _serializer;

    public BlockFarmEditorLayoutSerializerTests()
    {
        var databaseFactory = new Mock<IUmbracoDatabaseFactory>();
        databaseFactory.Setup(x => x.CreateDatabase()).Returns(_database.Object);

        var admin = new Mock<IUser>();
        admin.SetupGet(x => x.Key).Returns(AdminKey);
        _userService.Setup(x => x.GetUserById(-1)).Returns(admin.Object);

        _serializer = new BlockFarmEditorLayoutSerializer(
            Mock.Of<IEntityService>(),
            NullLogger<SyncSerializerBase<BlockFarmEditorLayoutDTO>>.Instance,
            databaseFactory.Object,
            _layoutService.Object,
            _userService.Object);
    }

    private void Existing(BlockFarmEditorLayoutDTO layout) =>
        _layoutService.Setup(x => x.GetByKeyAsync(_database.Object, layout.Key)).ReturnsAsync(layout);

    private async Task<XElement> Serialize(BlockFarmEditorLayoutDTO layout)
    {
        var attempt = await _serializer.SerializeAsync(layout, new SyncSerializerOptions());
        Assert.True(attempt.Success);
        return Assert.IsType<XElement>(attempt.Item);
    }

    #region Metadata

    [Fact]
    public void SerializerAttribute_DescribesTheLayoutTable()
    {
        var attribute = typeof(BlockFarmEditorLayoutSerializer).GetCustomAttribute<SyncSerializerAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("BlockFarmEditor Layout Serializer", attribute.Name);
        Assert.Equal(BlockFarmEditorLayoutDTO.TableName, attribute.ItemType);
        Assert.False(attribute.IsTwoPass);
    }

    [Fact]
    public void ItemAlias_IsTheKey()
    {
        var layout = Dtos.Layout();

        Assert.Equal(layout.Key.ToString(), _serializer.ItemAlias(layout));
    }

    #endregion

    #region Find / Save / Delete

    [Fact]
    public async Task FindItem_ByKey_UsesTheLayoutService()
    {
        var layout = Dtos.Layout();
        Existing(layout);

        Assert.Same(layout, await _serializer.FindItemAsync(layout.Key));
        Assert.Null(await _serializer.FindItemAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task FindItem_ByAlias_TreatsTheAliasAsAKey()
    {
        var layout = Dtos.Layout();
        Existing(layout);

        Assert.Same(layout, await _serializer.FindItemAsync(layout.Key.ToString()));
    }

    [Fact]
    public async Task FindItem_ByAlias_ThatIsNotAGuid_FindsNothing_WithoutQuerying()
    {
        Assert.Null(await _serializer.FindItemAsync("Two Column"));

        _layoutService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveItem_WhenTheKeyIsNew_CreatesTheLayout_KeepingItsOriginalAuthor()
    {
        var layout = Dtos.Layout();

        await _serializer.SaveItemAsync(layout);

        _layoutService.Verify(x => x.CreateAsync(_database.Object, layout, layout.CreatedBy), Times.Once);
        _layoutService.Verify(x => x.UpdateAsync(It.IsAny<IUmbracoDatabase>(), It.IsAny<int>(), It.IsAny<BlockFarmEditorLayoutDTO>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task SaveItem_WhenTheKeyExists_UpdatesTheStoredRow_WithTheIncomingValues()
    {
        var stored = Dtos.Layout(id: 8);
        Existing(stored);
        var incoming = Dtos.Layout("Renamed");
        incoming.Key = stored.Key;

        await _serializer.SaveItemAsync(incoming);

        _layoutService.Verify(x => x.UpdateAsync(_database.Object, 8, incoming, incoming.UpdatedBy), Times.Once);
        _layoutService.Verify(x => x.CreateAsync(It.IsAny<IUmbracoDatabase>(), It.IsAny<BlockFarmEditorLayoutDTO>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteItem_DeletesByKey()
    {
        var layout = Dtos.Layout();

        await _serializer.DeleteItemAsync(layout);

        _layoutService.Verify(x => x.DeleteAsync(_database.Object, layout.Key), Times.Once);
    }

    #endregion

    #region Serialize

    [Fact]
    public async Task Serialize_WritesAnElementNamedAfterTheTable_IdentifiedByKey()
    {
        var layout = Dtos.Layout();

        var node = await Serialize(layout);

        Assert.Equal(BlockFarmEditorLayoutDTO.TableName, node.Name.LocalName);
        Assert.Equal(layout.Key, node.GetKey());
        Assert.Equal(layout.Key.ToString(), node.GetAlias());
    }

    [Fact]
    public async Task Serialize_WritesEveryPersistedValue()
    {
        var layout = Dtos.Layout();

        var node = await Serialize(layout);

        Assert.Equal("Two Column", node.Element("Name")!.Value);
        Assert.Equal("Two equal columns", node.Element("Description")!.Value);
        Assert.Equal(layout.Layout, node.Element("Layout")!.Value);
        Assert.Equal("Layouts", node.Element("Category")!.Value);
        Assert.Equal("layout", node.Element("Type")!.Value);
        Assert.Equal("icon-layout", node.Element("Icon")!.Value);
        Assert.True((bool)node.Element("Enabled")!);
        Assert.Equal(layout.CreatedBy, (Guid)node.Element("CreatedBy")!);
        Assert.Equal(layout.UpdatedBy, (Guid)node.Element("UpdatedBy")!);
        Assert.Equal(layout.CreateDate, ((DateTime)node.Element("CreateDate")!).ToUniversalTime());
        Assert.Null(node.Element("Id"));
    }

    [Fact]
    public async Task Serialize_KeepsTheLayoutJsonIntact_ThroughAnXmlTextRoundTrip()
    {
        var layout = Dtos.Layout();

        var reparsed = XElement.Parse((await Serialize(layout)).ToString());

        Assert.Equal(layout.Layout, reparsed.Element("Layout")!.Value);
    }

    #endregion

    #region Deserialize

    [Fact]
    public async Task Deserialize_ForANewItem_BuildsTheLayoutFromTheFile()
    {
        var source = Dtos.Layout();
        source.Enabled = false;
        var node = await Serialize(source);

        var attempt = await _serializer.DeserializeAsync(node, new SyncSerializerOptions());

        Assert.True(attempt.Success);
        var item = Assert.IsType<BlockFarmEditorLayoutDTO>(attempt.Item);
        Assert.Equal(source.Key, item.Key);
        Assert.Equal(source.Name, item.Name);
        Assert.Equal(source.Description, item.Description);
        Assert.Equal(source.Layout, item.Layout);
        Assert.Equal(source.Category, item.Category);
        Assert.Equal(source.Type, item.Type);
        Assert.Equal(source.Icon, item.Icon);
        Assert.False(item.Enabled);
        Assert.Equal(source.CreatedBy, item.CreatedBy);
        Assert.Equal(source.UpdatedBy, item.UpdatedBy);
        Assert.Equal(source.CreateDate, item.CreateDate.ToUniversalTime());
        Assert.Equal(ChangeType.Import, attempt.Change);
    }

    [Fact]
    public async Task Deserialize_ForAnExistingItem_AppliesTheFilesValues_AndReportsEachChange()
    {
        var source = Dtos.Layout("New name");
        var stored = Dtos.Layout("Old name", id: 8);
        stored.Key = source.Key;
        stored.Icon = "icon-old";
        stored.CreatedBy = source.CreatedBy;
        stored.UpdatedBy = source.UpdatedBy;
        stored.CreateDate = source.CreateDate;
        Existing(stored);
        var node = await Serialize(source);

        var attempt = await _serializer.DeserializeAsync(node, new SyncSerializerOptions());

        Assert.True(attempt.Success);
        Assert.Same(stored, attempt.Item);
        Assert.Equal(8, stored.Id);
        Assert.Equal("New name", stored.Name);
        Assert.Equal("icon-layout", stored.Icon);
        Assert.Equal(["Icon", "Name"], attempt.Details!.Select(x => x.Name).Order());
    }

    [Fact]
    public async Task Deserialize_ForAnExistingItem_DetectsAChangeInEveryField()
    {
        var source = Dtos.Layout("New name");
        var stored = new BlockFarmEditorLayoutDTO
        {
            Id = 8,
            Key = source.Key,
            Name = "Old name",
            Description = "Old description",
            Layout = "{}",
            Category = "Old",
            Type = "old-type",
            Icon = "icon-old",
            Enabled = false,
            CreatedBy = Guid.NewGuid(),
            UpdatedBy = Guid.NewGuid(),
            CreateDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DeleteDate = new DateTime(2020, 2, 2, 0, 0, 0, DateTimeKind.Utc)
        };
        Existing(stored);

        var attempt = await _serializer.DeserializeAsync(await Serialize(source), new SyncSerializerOptions());

        Assert.Equal(
            ["Category", "CreateDate", "CreatedBy", "DeleteDate", "Description", "Enabled", "Icon", "Layout", "Name", "Type", "UpdatedBy"],
            attempt.Details!.Select(x => x.Name).Order());
        Assert.Equal(source.Description, stored.Description);
        Assert.Equal(source.Layout, stored.Layout);
        Assert.Equal(source.Category, stored.Category);
        Assert.Equal(source.Type, stored.Type);
        Assert.True(stored.Enabled);
        Assert.Equal(source.CreatedBy, stored.CreatedBy);
        Assert.Equal(source.UpdatedBy, stored.UpdatedBy);
        Assert.Equal(source.CreateDate, stored.CreateDate.ToUniversalTime());
        Assert.Null(stored.DeleteDate);
    }

    [Fact]
    public async Task Deserialize_WithMissingOptionalElements_UsesDefaults()
    {
        var key = Guid.NewGuid();
        var node = new XElement(BlockFarmEditorLayoutDTO.TableName,
            new XAttribute("Key", key),
            new XAttribute("Alias", key.ToString()),
            new XAttribute("Level", 0),
            new XElement("Name", "Bare"));

        var attempt = await _serializer.DeserializeAsync(node, new SyncSerializerOptions());

        var item = Assert.IsType<BlockFarmEditorLayoutDTO>(attempt.Item);
        Assert.Equal(key, item.Key);
        Assert.Equal("Bare", item.Name);
        Assert.Equal(string.Empty, item.Description);
        Assert.Equal(string.Empty, item.Layout);
        Assert.Equal(string.Empty, item.Category);
        Assert.Equal(string.Empty, item.Type);
        Assert.Equal(string.Empty, item.Icon);
        Assert.True(item.Enabled);
    }

    #endregion
}
