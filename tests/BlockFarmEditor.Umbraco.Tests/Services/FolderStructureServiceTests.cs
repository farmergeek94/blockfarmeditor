using BlockFarmEditor.Umbraco.Library.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace BlockFarmEditor.Umbraco.Tests.Services;

public class FolderStructureServiceTests
{
    private static readonly Guid AdminKey = Guid.NewGuid();

    private readonly Mock<IContentTypeContainerService> _contentTypeContainers = new();
    private readonly Mock<IDataTypeContainerService> _dataTypeContainers = new();
    private readonly Mock<IUserService> _userService = new();
    private readonly FolderStructureService _service;

    public FolderStructureServiceTests()
    {
        var admin = new Mock<IUser>();
        admin.SetupGet(x => x.Key).Returns(AdminKey);
        _userService.Setup(x => x.GetUserById(-1)).Returns(admin.Object);

        _contentTypeContainers.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync([]);
        _dataTypeContainers.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync([]);

        _service = new FolderStructureService(_contentTypeContainers.Object, _dataTypeContainers.Object, _userService.Object, NullLogger<FolderStructureService>.Instance);
    }

    private static EntityContainer Folder(string? name, int id = 0) => new(Constants.ObjectTypes.DocumentType)
    {
        Id = id,
        Key = Guid.NewGuid(),
        Name = name
    };

    private static Attempt<EntityContainer?, EntityContainerOperationStatus> Created(EntityContainer container) =>
        Attempt.SucceedWithStatus<EntityContainer?, EntityContainerOperationStatus>(EntityContainerOperationStatus.Success, container);

    private static Attempt<EntityContainer?, EntityContainerOperationStatus> Failed() =>
        Attempt.FailWithStatus<EntityContainer?, EntityContainerOperationStatus>(EntityContainerOperationStatus.ParentNotFound, null);

    #region BuildContentTypeFolderPathAsync

    [Fact]
    public async Task BuildContentTypeFolderPath_AtRoot_ReturnsNull()
    {
        var contentType = Mock.Of<IContentType>();

        Assert.Null(await _service.BuildContentTypeFolderPathAsync(contentType));
    }

    [Fact]
    public async Task BuildContentTypeFolderPath_WalksUpTheContainerHierarchy_RootFirst()
    {
        var contentType = Mock.Of<IContentType>();
        var root = Folder("Blocks");
        var middle = Folder("Content");
        var leaf = Folder("Heroes");
        _contentTypeContainers.Setup(x => x.GetParentAsync(contentType)).ReturnsAsync(leaf);
        _contentTypeContainers.Setup(x => x.GetParentAsync(leaf)).ReturnsAsync(middle);
        _contentTypeContainers.Setup(x => x.GetParentAsync(middle)).ReturnsAsync(root);

        Assert.Equal("Blocks/Content/Heroes", await _service.BuildContentTypeFolderPathAsync(contentType));
    }

    [Fact]
    public async Task BuildContentTypeFolderPath_UsesPlaceholderForUnnamedFolders()
    {
        var contentType = Mock.Of<IContentType>();
        _contentTypeContainers.Setup(x => x.GetParentAsync(contentType)).ReturnsAsync(Folder(null));

        Assert.Equal("Unnamed", await _service.BuildContentTypeFolderPathAsync(contentType));
    }

    #endregion

    #region BuildDataTypeFolderPathAsync

    [Fact]
    public async Task BuildDataTypeFolderPath_AtRoot_ReturnsNull()
    {
        var dataType = Mock.Of<IDataType>();

        Assert.Null(await _service.BuildDataTypeFolderPathAsync(dataType));
    }

    [Fact]
    public async Task BuildDataTypeFolderPath_WalksUpTheContainerHierarchy_RootFirst()
    {
        var dataType = Mock.Of<IDataType>();
        var root = Folder("Custom");
        var leaf = Folder("Pickers");
        _dataTypeContainers.Setup(x => x.GetParentAsync(dataType)).ReturnsAsync(leaf);
        _dataTypeContainers.Setup(x => x.GetParentAsync(leaf)).ReturnsAsync(root);

        Assert.Equal("Custom/Pickers", await _service.BuildDataTypeFolderPathAsync(dataType));
    }

    #endregion

    #region EnsureContentTypeFolderStructureAsync

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/")]
    [InlineData("///")]
    public async Task EnsureContentTypeFolderStructure_WithNoUsablePath_ReturnsRoot(string? path)
    {
        Assert.Equal(-1, await _service.EnsureContentTypeFolderStructureAsync(path));

        _contentTypeContainers.Verify(x => x.CreateAsync(It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task EnsureContentTypeFolderStructure_WhenAllFoldersExist_ReturnsDeepestId_WithoutCreating()
    {
        _contentTypeContainers.Setup(x => x.GetAsync("Blocks", 1)).ReturnsAsync([Folder("Blocks", 10)]);
        _contentTypeContainers.Setup(x => x.GetAsync("Content", 2)).ReturnsAsync([Folder("Content", 20)]);

        var result = await _service.EnsureContentTypeFolderStructureAsync("Blocks/Content");

        Assert.Equal(20, result);
        _contentTypeContainers.Verify(x => x.CreateAsync(It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task EnsureContentTypeFolderStructure_CreatesMissingFolders_UnderTheRightParent_AsAdmin()
    {
        var existing = Folder("Blocks", 10);
        var createdContent = Folder("Content", 20);
        var createdHeroes = Folder("Heroes", 30);
        _contentTypeContainers.Setup(x => x.GetAsync("Blocks", 1)).ReturnsAsync([existing]);
        _contentTypeContainers.Setup(x => x.CreateAsync(null, "Content", existing.Key, AdminKey)).ReturnsAsync(Created(createdContent));
        _contentTypeContainers.Setup(x => x.CreateAsync(null, "Heroes", createdContent.Key, AdminKey)).ReturnsAsync(Created(createdHeroes));

        var result = await _service.EnsureContentTypeFolderStructureAsync("Blocks/Content/Heroes");

        Assert.Equal(30, result);
        _contentTypeContainers.Verify(x => x.CreateAsync(null, "Content", existing.Key, AdminKey), Times.Once);
        _contentTypeContainers.Verify(x => x.CreateAsync(null, "Heroes", createdContent.Key, AdminKey), Times.Once);
    }

    [Fact]
    public async Task EnsureContentTypeFolderStructure_CreatesTopLevelFolderWithNoParent()
    {
        _contentTypeContainers.Setup(x => x.CreateAsync(null, "Blocks", null, AdminKey)).ReturnsAsync(Created(Folder("Blocks", 10)));

        Assert.Equal(10, await _service.EnsureContentTypeFolderStructureAsync("Blocks"));
    }

    [Fact]
    public async Task EnsureContentTypeFolderStructure_IgnoresEmptyPathSegments()
    {
        _contentTypeContainers.Setup(x => x.GetAsync("Blocks", 1)).ReturnsAsync([Folder("Blocks", 10)]);
        _contentTypeContainers.Setup(x => x.GetAsync("Content", 2)).ReturnsAsync([Folder("Content", 20)]);

        Assert.Equal(20, await _service.EnsureContentTypeFolderStructureAsync("/Blocks//Content/"));
    }

    [Fact]
    public async Task EnsureContentTypeFolderStructure_WhenCreationFails_ReturnsRoot_AndStops()
    {
        _contentTypeContainers.Setup(x => x.CreateAsync(null, "Blocks", null, AdminKey)).ReturnsAsync(Failed());

        var result = await _service.EnsureContentTypeFolderStructureAsync("Blocks/Content");

        Assert.Equal(-1, result);
        _contentTypeContainers.Verify(x => x.CreateAsync(null, "Content", It.IsAny<Guid?>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task EnsureContentTypeFolderStructure_WhenAdminUserIsMissing_UsesEmptyUserKey()
    {
        _userService.Setup(x => x.GetUserById(-1)).Returns((IUser?)null);
        _contentTypeContainers.Setup(x => x.CreateAsync(null, "Blocks", null, Guid.Empty)).ReturnsAsync(Created(Folder("Blocks", 10)));

        Assert.Equal(10, await _service.EnsureContentTypeFolderStructureAsync("Blocks"));
    }

    #endregion

    #region EnsureDataTypeFolderStructureAsync

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/")]
    public async Task EnsureDataTypeFolderStructure_WithNoUsablePath_ReturnsRoot(string? path)
    {
        Assert.Equal(-1, await _service.EnsureDataTypeFolderStructureAsync(path));

        _dataTypeContainers.Verify(x => x.CreateAsync(It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task EnsureDataTypeFolderStructure_MixesExistingAndCreatedFolders()
    {
        var existing = Folder("Custom", 10);
        _dataTypeContainers.Setup(x => x.GetAsync("Custom", 1)).ReturnsAsync([existing]);
        _dataTypeContainers.Setup(x => x.CreateAsync(null, "Pickers", existing.Key, AdminKey)).ReturnsAsync(Created(Folder("Pickers", 20)));

        var result = await _service.EnsureDataTypeFolderStructureAsync("Custom/Pickers");

        Assert.Equal(20, result);
        _dataTypeContainers.Verify(x => x.CreateAsync(null, "Custom", It.IsAny<Guid?>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task EnsureDataTypeFolderStructure_WhenCreationFails_ReturnsRoot()
    {
        _dataTypeContainers.Setup(x => x.CreateAsync(null, "Custom", null, AdminKey)).ReturnsAsync(Failed());

        Assert.Equal(-1, await _service.EnsureDataTypeFolderStructureAsync("Custom/Pickers"));
    }

    [Fact]
    public async Task EnsureDataTypeFolderStructure_DoesNotTouchContentTypeContainers()
    {
        _dataTypeContainers.Setup(x => x.GetAsync("Custom", 1)).ReturnsAsync([Folder("Custom", 10)]);

        await _service.EnsureDataTypeFolderStructureAsync("Custom");

        _contentTypeContainers.VerifyNoOtherCalls();
    }

    #endregion
}
