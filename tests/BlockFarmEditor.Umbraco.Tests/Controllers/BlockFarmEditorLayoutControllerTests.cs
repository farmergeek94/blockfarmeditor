using System.Xml.Serialization;
using BlockFarmEditor.Umbraco.Controllers;
using BlockFarmEditor.Umbraco.Core.DTO;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Tests.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Authorization;

namespace BlockFarmEditor.Umbraco.Tests.Controllers;

public abstract class BlockFarmEditorLayoutControllerTestBase
{
    private protected readonly Mock<IBlockFarmEditorLayoutService> LayoutService = new();
    private protected readonly Mock<IUserService> UserService = new();
    private protected readonly Mock<IUmbracoDatabase> Database = new();
    private protected readonly Mock<IBlockDefinitionService> BlockDefinitionService = new();
    private protected readonly BlockFarmEditorLayoutController Controller;

    protected BlockFarmEditorLayoutControllerTestBase()
    {
        Controller = new BlockFarmEditorLayoutController(
            LayoutService.Object,
            NullLogger<BlockFarmEditorLayoutController>.Instance,
            UserService.Object,
            Database.ToFactory().Object,
            BlockDefinitionService.Object).WithHttpContext();
    }
}

public class BlockFarmEditorLayoutControllerTests : BlockFarmEditorLayoutControllerTestBase
{
    private static CreateBlockFarmEditorLayoutRequest CreateRequest() => new()
    {
        Name = "Two Column",
        Description = "Two equal columns",
        Layout = "{\"blocks\":[]}",
        Category = "Layouts",
        Type = "layout",
        Icon = "icon-layout",
        Enabled = false
    };

    private static UpdateBlockFarmEditorLayoutRequest UpdateRequest(Guid key) => new()
    {
        Key = key,
        Description = "New description",
        Layout = "{\"blocks\":[1]}",
        Category = "New category",
        Type = "new-type",
        Icon = "icon-new",
        Enabled = false
    };

    [Fact]
    public void Controller_RequiresBackOfficeAccess()
    {
        var attribute = Assert.Single(typeof(BlockFarmEditorLayoutController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());

        Assert.Equal(AuthorizationPolicies.BackOfficeAccess, attribute.Policy);
    }

    #region Index / Categories / Delete

    [Fact]
    public async Task Index_WhenFound_ReturnsTheLayout()
    {
        var layout = Dtos.Layout("Two Column", id: 3);
        LayoutService.Setup(x => x.GetByKeyAsync(Database.Object, layout.Key)).ReturnsAsync(layout);

        var result = Assert.IsType<OkObjectResult>(await Controller.Index(layout.Key));

        Assert.Equal("Two Column", result.Json().GetProperty("data").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Index_WhenNotFound_ReturnsAnEmptyObject()
    {
        var result = Assert.IsType<OkObjectResult>(await Controller.Index(Guid.NewGuid()));

        Assert.Equal("{}", result.Json().GetRawText());
    }

    [Fact]
    public async Task Index_WhenTheLookupFails_Returns500()
    {
        LayoutService.Setup(x => x.GetByKeyAsync(Database.Object, It.IsAny<Guid>())).ThrowsAsync(new InvalidOperationException("db down"));

        (await Controller.Index(Guid.NewGuid())).AssertServerError("Internal server error");
    }

    [Fact]
    public async Task Categories_ReturnsTheDistinctCategories()
    {
        LayoutService.Setup(x => x.GetCategories(Database.Object)).ReturnsAsync(["Heroes", "Grids"]);

        var result = Assert.IsType<OkObjectResult>(await Controller.Categories());

        Assert.Equal(["Heroes", "Grids"], result.Json().GetProperty("data").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public async Task Categories_WhenTheLookupFails_Returns500()
    {
        LayoutService.Setup(x => x.GetCategories(Database.Object)).ThrowsAsync(new InvalidOperationException("db down"));

        (await Controller.Categories()).AssertServerError("Internal server error");
    }

    [Fact]
    public async Task Delete_RemovesTheLayout()
    {
        var key = Guid.NewGuid();

        Assert.IsType<OkResult>(await Controller.Delete(key));

        LayoutService.Verify(x => x.DeleteAsync(Database.Object, key), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenDeletingFails_Returns500()
    {
        LayoutService.Setup(x => x.DeleteAsync(Database.Object, It.IsAny<Guid>())).ThrowsAsync(new InvalidOperationException("db down"));

        (await Controller.Delete(Guid.NewGuid())).AssertServerError("Internal server error");
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_WithInvalidModel_ReturnsBadRequest()
    {
        Controller.ModelState.AddModelError("Name", "Required");

        Assert.IsType<BadRequestObjectResult>(await Controller.Create(CreateRequest()));
        LayoutService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_PersistsTheRequest_AsTheSignedInUser()
    {
        var userKey = UserService.AddUser("editor@example.test");
        Controller.SignedInAs("editor@example.test");
        BlockFarmEditorLayoutDTO? created = null;
        LayoutService
            .Setup(x => x.CreateAsync(Database.Object, It.IsAny<BlockFarmEditorLayoutDTO>(), userKey))
            .Callback((IUmbracoDatabase _, BlockFarmEditorLayoutDTO dto, Guid _) => created = dto)
            .ReturnsAsync((IUmbracoDatabase _, BlockFarmEditorLayoutDTO dto, Guid _) => dto);

        var result = Assert.IsType<CreatedAtActionResult>(await Controller.Create(CreateRequest()));

        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Key);
        Assert.Equal("Two Column", created.Name);
        Assert.Equal("Two equal columns", created.Description);
        Assert.Equal("{\"blocks\":[]}", created.Layout);
        Assert.Equal("Layouts", created.Category);
        Assert.Equal("layout", created.Type);
        Assert.Equal("icon-layout", created.Icon);
        Assert.False(created.Enabled);
        Assert.Equal(userKey, created.CreatedBy);
        Assert.Equal(userKey, created.UpdatedBy);

        Assert.Equal(nameof(BlockFarmEditorLayoutController.Index), result.ActionName);
        Assert.Equal(created.Key, result.RouteValues!["key"]);
    }

    [Fact]
    public async Task Create_ForAnUnknownUsername_FallsBackToTheAdminUser()
    {
        var adminKey = UserService.AddAdminUser();
        Controller.SignedInAs("ghost@example.test");
        LayoutService
            .Setup(x => x.CreateAsync(Database.Object, It.IsAny<BlockFarmEditorLayoutDTO>(), It.IsAny<Guid>()))
            .ReturnsAsync((IUmbracoDatabase _, BlockFarmEditorLayoutDTO dto, Guid _) => dto);

        await Controller.Create(CreateRequest());

        LayoutService.Verify(x => x.CreateAsync(Database.Object, It.IsAny<BlockFarmEditorLayoutDTO>(), adminKey), Times.Once);
    }

    [Fact]
    public async Task Create_WhenTheUserLookupFails_StillSaves_AsTheEmptyUser()
    {
        UserService.Setup(x => x.GetByUsername(It.IsAny<string>())).Throws(new InvalidOperationException("db down"));
        Controller.SignedInAs("editor@example.test");
        LayoutService
            .Setup(x => x.CreateAsync(Database.Object, It.IsAny<BlockFarmEditorLayoutDTO>(), It.IsAny<Guid>()))
            .ReturnsAsync((IUmbracoDatabase _, BlockFarmEditorLayoutDTO dto, Guid _) => dto);

        Assert.IsType<CreatedAtActionResult>(await Controller.Create(CreateRequest()));

        LayoutService.Verify(x => x.CreateAsync(Database.Object, It.IsAny<BlockFarmEditorLayoutDTO>(), Guid.Empty), Times.Once);
    }

    [Fact]
    public async Task Create_RefreshesTheDefinitionCache()
    {
        LayoutService
            .Setup(x => x.CreateAsync(Database.Object, It.IsAny<BlockFarmEditorLayoutDTO>(), It.IsAny<Guid>()))
            .ReturnsAsync((IUmbracoDatabase _, BlockFarmEditorLayoutDTO dto, Guid _) => dto);

        await Controller.Create(CreateRequest());

        BlockDefinitionService.Verify(x => x.ClearCache(), Times.Once);
    }

    [Fact]
    public async Task Create_WhenPersistingFails_Returns500()
    {
        LayoutService
            .Setup(x => x.CreateAsync(Database.Object, It.IsAny<BlockFarmEditorLayoutDTO>(), It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        (await Controller.Create(CreateRequest())).AssertServerError("Internal server error");
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_WithInvalidModel_ReturnsBadRequest()
    {
        Controller.ModelState.AddModelError("Layout", "Required");

        Assert.IsType<BadRequestObjectResult>(await Controller.Update(1, UpdateRequest(Guid.NewGuid())));
        LayoutService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_WhenNoLayoutHasTheRequestedKey_ReturnsNotFound()
    {
        var key = Guid.NewGuid();

        var result = Assert.IsType<NotFoundObjectResult>(await Controller.Update(1, UpdateRequest(key)));

        Assert.Contains(key.ToString(), Assert.IsType<string>(result.Value));
        LayoutService.Verify(x => x.UpdateAsync(It.IsAny<IUmbracoDatabase>(), It.IsAny<int>(), It.IsAny<BlockFarmEditorLayoutDTO>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenNoLayoutHasTheRequestedId_ReturnsNotFound()
    {
        var layout = Dtos.Layout(id: 5);
        LayoutService.Setup(x => x.GetByKeyAsync(Database.Object, layout.Key)).ReturnsAsync(layout);

        var result = Assert.IsType<NotFoundObjectResult>(await Controller.Update(99, UpdateRequest(layout.Key)));

        Assert.Contains("99", Assert.IsType<string>(result.Value));
        BlockDefinitionService.Verify(x => x.ClearCache(), Times.Never);
    }

    [Fact]
    public async Task Update_SavesTheLayout_AsTheSignedInUser()
    {
        var userKey = UserService.AddUser("editor@example.test");
        Controller.SignedInAs("editor@example.test");
        var layout = Dtos.Layout(id: 5);
        LayoutService.Setup(x => x.GetByKeyAsync(Database.Object, layout.Key)).ReturnsAsync(layout);
        LayoutService.Setup(x => x.UpdateAsync(Database.Object, 5, layout, userKey)).ReturnsAsync(layout);

        var result = Assert.IsType<OkObjectResult>(await Controller.Update(5, UpdateRequest(layout.Key)));

        Assert.Equal(5, result.Json().GetProperty("data").GetProperty("id").GetInt32());
        LayoutService.Verify(x => x.UpdateAsync(Database.Object, 5, layout, userKey), Times.Once);
    }

    [Fact(Skip = "Known bug: BlockFarmEditorLayoutController.Update loads the layout by key and saves it back untouched - none of the request's fields (Description, Layout, Category, Type, Icon, Enabled) are applied.")]
    public async Task Update_AppliesTheRequestedChanges()
    {
        var layout = Dtos.Layout(id: 5);
        BlockFarmEditorLayoutDTO? saved = null;
        LayoutService.Setup(x => x.GetByKeyAsync(Database.Object, layout.Key)).ReturnsAsync(layout);
        LayoutService
            .Setup(x => x.UpdateAsync(Database.Object, 5, It.IsAny<BlockFarmEditorLayoutDTO>(), It.IsAny<Guid>()))
            .Callback((IUmbracoDatabase _, int _, BlockFarmEditorLayoutDTO dto, Guid _) => saved = dto)
            .ReturnsAsync(layout);

        await Controller.Update(5, UpdateRequest(layout.Key));

        Assert.NotNull(saved);
        Assert.Equal("New description", saved.Description);
        Assert.Equal("{\"blocks\":[1]}", saved.Layout);
        Assert.Equal("New category", saved.Category);
        Assert.Equal("new-type", saved.Type);
        Assert.Equal("icon-new", saved.Icon);
        Assert.False(saved.Enabled);
    }

    [Fact]
    public async Task Update_WhenPersistingFails_Returns500()
    {
        LayoutService.Setup(x => x.GetByKeyAsync(Database.Object, It.IsAny<Guid>())).ThrowsAsync(new InvalidOperationException("db down"));

        (await Controller.Update(5, UpdateRequest(Guid.NewGuid()))).AssertServerError("Internal server error");
    }

    #endregion
}

[Collection(WorkingDirectoryCollection.Name)]
public class BlockFarmEditorLayoutControllerFileTests : BlockFarmEditorLayoutControllerTestBase, IDisposable
{
    private readonly TempWorkingDirectory _workingDirectory = new();

    public void Dispose() => _workingDirectory.Dispose();

    private string ExportFolder => _workingDirectory.Combine("BlockFarmEditor", "Definitions");

    private void WriteLayoutFile(string fileName, BlockFarmEditorLayoutDTO layout)
    {
        Directory.CreateDirectory(ExportFolder);
        using var stream = System.IO.File.Create(Path.Combine(ExportFolder, fileName));
        new XmlSerializer(typeof(BlockFarmEditorLayoutDTO)).Serialize(stream, layout);
    }

    private static BlockFarmEditorLayoutDTO ReadLayoutFile(string path)
    {
        using var stream = System.IO.File.OpenRead(path);
        return (BlockFarmEditorLayoutDTO)new XmlSerializer(typeof(BlockFarmEditorLayoutDTO)).Deserialize(stream)!;
    }

    [Fact]
    public async Task Export_WritesEachLayoutToAnXmlFileNamedAfterItsKey()
    {
        var first = Dtos.Layout("First");
        var second = Dtos.Layout("Second");
        second.Layout = "{\"blocks\":[{\"properties\":{\"text\":\"<b>&</b>\"}}]}";
        LayoutService.Setup(x => x.GetAllAsync(Database.Object)).ReturnsAsync(new[] { first, second }.ToAsyncEnumerable());

        Assert.IsType<OkResult>(await Controller.Export());

        Assert.Equal(
            new[] { $"{first.Key}.xml", $"{second.Key}.xml" }.Order(),
            Directory.EnumerateFiles(ExportFolder).Select(Path.GetFileName).Order());
        var exported = ReadLayoutFile(Path.Combine(ExportFolder, $"{second.Key}.xml"));
        Assert.Equal("Second", exported.Name);
        Assert.Equal(second.Layout, exported.Layout);
    }

    [Fact]
    public async Task Export_WithNoLayouts_StillSucceeds()
    {
        LayoutService.Setup(x => x.GetAllAsync(Database.Object)).ReturnsAsync((IAsyncEnumerable<BlockFarmEditorLayoutDTO>?)null);

        Assert.IsType<OkResult>(await Controller.Export());

        Assert.Empty(Directory.EnumerateFiles(ExportFolder));
    }

    [Fact]
    public async Task Import_WithoutAnExportFolder_DoesNothing()
    {
        Assert.IsType<OkResult>(await Controller.Import());

        LayoutService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Import_CreatesLayoutsThatDoNotExistYet()
    {
        var adminKey = UserService.AddAdminUser();
        Controller.SignedInAs("ghost@example.test");
        var layout = Dtos.Layout("Imported");
        WriteLayoutFile($"{layout.Key}.xml", layout);

        Assert.IsType<OkResult>(await Controller.Import());

        LayoutService.Verify(x => x.CreateAsync(Database.Object, It.Is<BlockFarmEditorLayoutDTO>(dto => dto.Key == layout.Key && dto.Name == "Imported"), adminKey), Times.Once);
        LayoutService.Verify(x => x.UpdateAsync(It.IsAny<IUmbracoDatabase>(), It.IsAny<int>(), It.IsAny<BlockFarmEditorLayoutDTO>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Import_UpdatesExistingLayouts_WithTheValuesFromTheFile()
    {
        var existing = Dtos.Layout("Old name", category: "Old", id: 8);
        var incoming = Dtos.Layout("New name", category: "New", key: existing.Key);
        incoming.Description = "New description";
        incoming.Layout = "{\"blocks\":[1]}";
        incoming.Type = "new-type";
        incoming.Icon = "icon-new";
        incoming.Enabled = false;
        WriteLayoutFile($"{existing.Key}.xml", incoming);
        LayoutService.Setup(x => x.GetByKeyAsync(Database.Object, existing.Key)).ReturnsAsync(existing);

        Assert.IsType<OkResult>(await Controller.Import());

        LayoutService.Verify(x => x.UpdateAsync(Database.Object, 8, existing, It.IsAny<Guid>()), Times.Once);
        Assert.Equal("New name", existing.Name);
        Assert.Equal("New", existing.Category);
        Assert.Equal("New description", existing.Description);
        Assert.Equal("{\"blocks\":[1]}", existing.Layout);
        Assert.Equal("new-type", existing.Type);
        Assert.Equal("icon-new", existing.Icon);
        Assert.False(existing.Enabled);
        LayoutService.Verify(x => x.CreateAsync(It.IsAny<IUmbracoDatabase>(), It.IsAny<BlockFarmEditorLayoutDTO>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Import_SkipsFilesThatAreNotLayouts_AndKeepsGoing()
    {
        var layout = Dtos.Layout("Imported");
        WriteLayoutFile($"{layout.Key}.xml", layout);
        WriteLayoutFile("not-a-guid.xml", Dtos.Layout("Badly named"));
        _workingDirectory.WriteFile("BlockFarmEditor/Definitions/corrupt.xml", "<not-closed");
        _workingDirectory.WriteFile("BlockFarmEditor/Definitions/heroBlock.xml", "<BlockFarmEditorDefinitionDTO><ContentTypeAlias>heroBlock</ContentTypeAlias></BlockFarmEditorDefinitionDTO>");

        Assert.IsType<OkResult>(await Controller.Import());

        LayoutService.Verify(x => x.CreateAsync(Database.Object, It.IsAny<BlockFarmEditorLayoutDTO>(), It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task Import_WhenOneLayoutFailsToSave_StillImportsTheOthers()
    {
        var broken = Dtos.Layout("Broken");
        var fine = Dtos.Layout("Fine");
        WriteLayoutFile($"{broken.Key}.xml", broken);
        WriteLayoutFile($"{fine.Key}.xml", fine);
        LayoutService
            .Setup(x => x.CreateAsync(Database.Object, It.Is<BlockFarmEditorLayoutDTO>(dto => dto.Key == broken.Key), It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        Assert.IsType<OkResult>(await Controller.Import());

        LayoutService.Verify(x => x.CreateAsync(Database.Object, It.Is<BlockFarmEditorLayoutDTO>(dto => dto.Key == fine.Key), It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task ExportThenImport_RoundTripsLayouts()
    {
        var layout = Dtos.Layout("Round trip");
        LayoutService.Setup(x => x.GetAllAsync(Database.Object)).ReturnsAsync(new[] { layout }.ToAsyncEnumerable());

        await Controller.Export();
        await Controller.Import();

        LayoutService.Verify(x => x.CreateAsync(Database.Object, It.Is<BlockFarmEditorLayoutDTO>(dto => dto.Key == layout.Key && dto.Layout == layout.Layout), It.IsAny<Guid>()), Times.Once);
    }
}
