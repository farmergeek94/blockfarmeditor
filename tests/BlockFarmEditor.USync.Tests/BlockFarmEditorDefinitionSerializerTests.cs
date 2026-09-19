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

public class BlockFarmEditorDefinitionSerializerTests
{
    private static readonly Guid AdminKey = Guid.NewGuid();

    private readonly Mock<IBlockFarmEditorDefinitionService> _definitionService = new();
    private readonly Mock<IUmbracoDatabase> _database = new();
    private readonly Mock<IUserService> _userService = new();
    private readonly BlockFarmEditorDefinitionSerializer _serializer;

    public BlockFarmEditorDefinitionSerializerTests()
    {
        var databaseFactory = new Mock<IUmbracoDatabaseFactory>();
        databaseFactory.Setup(x => x.CreateDatabase()).Returns(_database.Object);

        var admin = new Mock<IUser>();
        admin.SetupGet(x => x.Key).Returns(AdminKey);
        _userService.Setup(x => x.GetUserById(-1)).Returns(admin.Object);

        _serializer = new BlockFarmEditorDefinitionSerializer(
            Mock.Of<IEntityService>(),
            NullLogger<SyncSerializerBase<BlockFarmEditorDefinitionDTO>>.Instance,
            databaseFactory.Object,
            _definitionService.Object,
            _userService.Object);
    }

    private void Existing(BlockFarmEditorDefinitionDTO definition)
    {
        _definitionService.Setup(x => x.GetByKeyAsync(_database.Object, definition.Key)).ReturnsAsync(definition);
        _definitionService.Setup(x => x.GetByAliasAsync(_database.Object, definition.ContentTypeAlias)).ReturnsAsync(definition);
    }

    private async Task<XElement> Serialize(BlockFarmEditorDefinitionDTO definition)
    {
        var attempt = await _serializer.SerializeAsync(definition, new SyncSerializerOptions());
        Assert.True(attempt.Success);
        return Assert.IsType<XElement>(attempt.Item);
    }

    #region Metadata

    [Fact]
    public void SerializerAttribute_DescribesTheDefinitionTable()
    {
        var attribute = typeof(BlockFarmEditorDefinitionSerializer).GetCustomAttribute<SyncSerializerAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("BlockFarmEditor Definition Serializer", attribute.Name);
        Assert.Equal(BlockFarmEditorDefinitionDTO.TableName, attribute.ItemType);
        Assert.False(attribute.IsTwoPass);
    }

    [Fact]
    public void SerializerId_IsNotSharedWithTheLayoutSerializer()
    {
        var definitionId = typeof(BlockFarmEditorDefinitionSerializer).GetCustomAttribute<SyncSerializerAttribute>()!.Id;
        var layoutId = typeof(BlockFarmEditorLayoutSerializer).GetCustomAttribute<SyncSerializerAttribute>()!.Id;

        Assert.NotEqual(definitionId, layoutId);
    }

    [Fact]
    public void ItemAlias_IsTheKey()
    {
        var definition = Dtos.Definition();

        Assert.Equal(definition.Key.ToString(), _serializer.ItemAlias(definition));
    }

    #endregion

    #region Find / Save / Delete

    [Fact]
    public async Task FindItem_ByKey_UsesTheDefinitionService()
    {
        var definition = Dtos.Definition();
        Existing(definition);

        Assert.Same(definition, await _serializer.FindItemAsync(definition.Key));
        Assert.Null(await _serializer.FindItemAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task FindItem_ByAlias_LooksUpTheContentTypeAlias()
    {
        var definition = Dtos.Definition("heroBlock");
        Existing(definition);

        Assert.Same(definition, await _serializer.FindItemAsync("heroBlock"));
        Assert.Null(await _serializer.FindItemAsync("missing"));
    }

    [Fact]
    public async Task SaveItem_WhenTheKeyIsNew_CreatesTheDefinition_KeepingItsOriginalAuthor()
    {
        var definition = Dtos.Definition();

        await _serializer.SaveItemAsync(definition);

        _definitionService.Verify(x => x.CreateAsync(_database.Object, definition, definition.CreatedBy), Times.Once);
        _definitionService.Verify(x => x.UpdateAsync(It.IsAny<IUmbracoDatabase>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task SaveItem_WhenTheKeyExists_UpdatesTheStoredRow()
    {
        var stored = Dtos.Definition(id: 12);
        Existing(stored);
        var incoming = Dtos.Definition();
        incoming.Key = stored.Key;
        incoming.Type = "viewcomponent";
        incoming.ViewPath = "new.cshtml";
        incoming.Category = "New";
        incoming.Enabled = false;

        await _serializer.SaveItemAsync(incoming);

        _definitionService.Verify(x => x.UpdateAsync(_database.Object, 12, "viewcomponent", "new.cshtml", "New", false, incoming.UpdatedBy), Times.Once);
        _definitionService.Verify(x => x.CreateAsync(It.IsAny<IUmbracoDatabase>(), It.IsAny<BlockFarmEditorDefinitionDTO>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteItem_DeletesByContentTypeAlias()
    {
        await _serializer.DeleteItemAsync(Dtos.Definition("heroBlock"));

        _definitionService.Verify(x => x.DeleteAsync(_database.Object, "heroBlock"), Times.Once);
    }

    #endregion

    #region Serialize

    [Fact]
    public async Task Serialize_WritesAnElementNamedAfterTheTable_IdentifiedByKeyAndContentTypeAlias()
    {
        var definition = Dtos.Definition("heroBlock");

        var node = await Serialize(definition);

        Assert.Equal(BlockFarmEditorDefinitionDTO.TableName, node.Name.LocalName);
        Assert.Equal(definition.Key, node.GetKey());
        Assert.Equal("heroBlock", node.GetAlias());
    }

    [Fact]
    public async Task Serialize_WritesEveryPersistedValue()
    {
        var definition = Dtos.Definition("heroBlock");
        definition.DeleteDate = new DateTime(2025, 6, 7, 8, 9, 10, DateTimeKind.Utc);

        var node = await Serialize(definition);

        Assert.Equal("partial", node.Element("Type")!.Value);
        Assert.Equal("heroBlock", node.Element("ContentTypeAlias")!.Value);
        Assert.Equal("~/Views/Partials/Hero.cshtml", node.Element("ViewPath")!.Value);
        Assert.Equal("Content", node.Element("Category")!.Value);
        Assert.True((bool)node.Element("Enabled")!);
        Assert.Equal(definition.CreatedBy, (Guid)node.Element("CreatedBy")!);
        Assert.Equal(definition.UpdatedBy, (Guid)node.Element("UpdatedBy")!);
        Assert.Equal(definition.CreateDate, ((DateTime)node.Element("CreateDate")!).ToUniversalTime());
        Assert.Equal(definition.DeleteDate, ((DateTime)node.Element("DeleteDate")!).ToUniversalTime());
    }

    [Fact]
    public async Task Serialize_DoesNotIncludeTheDatabaseId_SoFilesArePortableBetweenEnvironments()
    {
        var node = await Serialize(Dtos.Definition(id: 12));

        Assert.Null(node.Element("Id"));
    }

    [Fact]
    public async Task Serialize_IsStable_ForAnUnchangedItem()
    {
        var definition = Dtos.Definition();

        Assert.Equal((await Serialize(definition)).ToString(), (await Serialize(definition)).ToString());
    }

    #endregion

    #region Deserialize

    [Fact]
    public async Task Deserialize_ForANewItem_BuildsTheDefinitionFromTheFile()
    {
        var source = Dtos.Definition("heroBlock");
        source.Enabled = false;
        var node = await Serialize(source);

        var attempt = await _serializer.DeserializeAsync(node, new SyncSerializerOptions());

        Assert.True(attempt.Success);
        var item = Assert.IsType<BlockFarmEditorDefinitionDTO>(attempt.Item);
        Assert.Equal(source.Key, item.Key);
        Assert.Equal("heroBlock", item.ContentTypeAlias);
        Assert.Equal("partial", item.Type);
        Assert.Equal("~/Views/Partials/Hero.cshtml", item.ViewPath);
        Assert.Equal("Content", item.Category);
        Assert.False(item.Enabled);
        Assert.Equal(source.CreatedBy, item.CreatedBy);
        Assert.Equal(source.UpdatedBy, item.UpdatedBy);
        Assert.Equal(source.CreateDate, item.CreateDate.ToUniversalTime());
        Assert.Null(item.DeleteDate);
        Assert.Equal(ChangeType.Import, attempt.Change);
    }

    [Fact]
    public async Task Deserialize_ForAnExistingItem_AppliesTheFilesValues_AndReportsEachChange()
    {
        var source = Dtos.Definition("heroBlock");
        var stored = Dtos.Definition("heroBlock", id: 12);
        stored.Key = source.Key;
        stored.Category = "Old";
        stored.ViewPath = "old.cshtml";
        stored.CreatedBy = source.CreatedBy;
        stored.UpdatedBy = source.UpdatedBy;
        stored.CreateDate = source.CreateDate;
        Existing(stored);
        var node = await Serialize(source);

        var attempt = await _serializer.DeserializeAsync(node, new SyncSerializerOptions());

        Assert.True(attempt.Success);
        Assert.Same(stored, attempt.Item);
        Assert.Equal(12, stored.Id);
        Assert.Equal("Content", stored.Category);
        Assert.Equal("~/Views/Partials/Hero.cshtml", stored.ViewPath);
        Assert.Equal(["Category", "ViewPath"], attempt.Details!.Select(x => x.Name).Order());
        var categoryChange = attempt.Details!.Single(x => x.Name == "Category");
        Assert.Equal("Old", categoryChange.OldValue);
        Assert.Equal("Content", categoryChange.NewValue);
    }

    [Fact]
    public async Task Deserialize_ForAnExistingItem_DetectsAChangeInEveryField()
    {
        var source = Dtos.Definition("heroBlock");
        source.DeleteDate = new DateTime(2025, 6, 7, 8, 9, 10, DateTimeKind.Utc);
        var stored = new BlockFarmEditorDefinitionDTO
        {
            Id = 12,
            Key = source.Key,
            ContentTypeAlias = "oldAlias",
            Type = "viewcomponent",
            ViewPath = "old.cshtml",
            Category = "Old",
            Enabled = false,
            CreatedBy = Guid.NewGuid(),
            UpdatedBy = Guid.NewGuid(),
            CreateDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        _definitionService.Setup(x => x.GetByKeyAsync(_database.Object, stored.Key)).ReturnsAsync(stored);

        var attempt = await _serializer.DeserializeAsync(await Serialize(source), new SyncSerializerOptions());

        Assert.Equal(
            ["Category", "ContentTypeAlias", "CreateDate", "CreatedBy", "DeleteDate", "Enabled", "Type", "UpdatedBy", "ViewPath"],
            attempt.Details!.Select(x => x.Name).Order());
        Assert.Equal("heroBlock", stored.ContentTypeAlias);
        Assert.Equal("partial", stored.Type);
        Assert.True(stored.Enabled);
        Assert.Equal(source.CreatedBy, stored.CreatedBy);
        Assert.Equal(source.UpdatedBy, stored.UpdatedBy);
        Assert.Equal(source.CreateDate, stored.CreateDate.ToUniversalTime());
        Assert.Equal(source.DeleteDate, stored.DeleteDate!.Value.ToUniversalTime());
    }

    [Fact]
    public async Task Deserialize_ForAnUnchangedItem_ReportsNoChanges()
    {
        var source = Dtos.Definition("heroBlock");
        var node = await Serialize(source);
        Existing((BlockFarmEditorDefinitionDTO)source.DeepClone());

        var attempt = await _serializer.DeserializeAsync(node, new SyncSerializerOptions());

        Assert.Empty(attempt.Details ?? []);
    }

    [Fact]
    public async Task Deserialize_WhenTheKeyIsUnknown_FallsBackToMatchingOnContentTypeAlias()
    {
        var source = Dtos.Definition("heroBlock");
        var storedUnderAnotherKey = Dtos.Definition("heroBlock", id: 12);
        _definitionService.Setup(x => x.GetByAliasAsync(_database.Object, "heroBlock")).ReturnsAsync(storedUnderAnotherKey);
        var node = await Serialize(source);

        var attempt = await _serializer.DeserializeAsync(node, new SyncSerializerOptions());

        Assert.Same(storedUnderAnotherKey, attempt.Item);
    }

    [Fact]
    public async Task Deserialize_WithMissingOptionalElements_UsesDefaults()
    {
        var key = Guid.NewGuid();
        var node = new XElement(BlockFarmEditorDefinitionDTO.TableName,
            new XAttribute("Key", key),
            new XAttribute("Alias", "heroBlock"),
            new XAttribute("Level", 0),
            new XElement("ContentTypeAlias", "heroBlock"));

        var attempt = await _serializer.DeserializeAsync(node, new SyncSerializerOptions());

        var item = Assert.IsType<BlockFarmEditorDefinitionDTO>(attempt.Item);
        Assert.Equal(key, item.Key);
        Assert.Equal("heroBlock", item.ContentTypeAlias);
        Assert.Equal(string.Empty, item.Type);
        Assert.Equal(string.Empty, item.ViewPath);
        Assert.Equal(string.Empty, item.Category);
        Assert.True(item.Enabled);
        Assert.Null(item.DeleteDate);
    }

    [Fact]
    public async Task SerializeThenDeserialize_RoundTripsADeletedDefinition()
    {
        var source = Dtos.Definition();
        source.DeleteDate = new DateTime(2025, 6, 7, 8, 9, 10, DateTimeKind.Utc);

        var attempt = await _serializer.DeserializeAsync(await Serialize(source), new SyncSerializerOptions());

        Assert.Equal(source.DeleteDate, attempt.Item!.DeleteDate!.Value.ToUniversalTime());
    }

    #endregion
}
