using BlockFarmEditor.Umbraco.Controllers;
using BlockFarmEditor.Umbraco.Core.DTO;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Tests.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Authorization;

namespace BlockFarmEditor.Umbraco.Tests.Controllers;

public class BlockFarmEditorDefinitionControllerTests
{
    private readonly Mock<IBlockFarmEditorDefinitionService> _definitionService = new();
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<IUmbracoDatabase> _database = new();
    private readonly Mock<IBlockDefinitionService> _blockDefinitionService = new();
    private readonly Mock<IBlockFarmEditorExportService> _exportService = new();
    private readonly BlockFarmEditorDefinitionController _controller;

    public BlockFarmEditorDefinitionControllerTests()
    {
        _controller = new BlockFarmEditorDefinitionController(
            _definitionService.Object,
            NullLogger<BlockFarmEditorDefinitionController>.Instance,
            _userService.Object,
            _database.ToFactory().Object,
            _blockDefinitionService.Object,
            _exportService.Object).WithHttpContext();
    }

    private static CreateBlockFarmEditorDefinitionRequest CreateRequest() => new()
    {
        ContentTypeAlias = "heroBlock",
        Type = "partial",
        ViewPath = "~/Views/Partials/Hero.cshtml",
        Category = "Content",
        Enabled = true
    };

    private static UpdateBlockFarmEditorDefinitionRequest UpdateRequest() => new()
    {
        Type = "viewcomponent",
        ViewPath = "new.cshtml",
        Category = "New",
        Enabled = false
    };

    #region Authorization

    [Fact]
    public void Controller_RequiresBackOfficeAccess()
    {
        var attribute = Assert.Single(typeof(BlockFarmEditorDefinitionController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());

        Assert.Equal(AuthorizationPolicies.BackOfficeAccess, attribute.Policy);
    }

    [Theory]
    [InlineData(nameof(BlockFarmEditorDefinitionController.Create))]
    [InlineData(nameof(BlockFarmEditorDefinitionController.Update))]
    [InlineData(nameof(BlockFarmEditorDefinitionController.ExportPackage))]
    [InlineData(nameof(BlockFarmEditorDefinitionController.ImportPackage))]
    [InlineData(nameof(BlockFarmEditorDefinitionController.Exportable))]
    public void MutatingAndExportActions_RequireSettingsSectionAccess(string action)
    {
        var attribute = Assert.Single(typeof(BlockFarmEditorDefinitionController).GetMethod(action)!.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());

        Assert.Equal(AuthorizationPolicies.SectionAccessSettings, attribute.Policy);
    }

    #endregion

    #region Index / Categories

    [Fact]
    public async Task Index_WhenFound_ReturnsTheDefinition()
    {
        var definition = Dtos.Definition("heroBlock", id: 4);
        _definitionService.Setup(x => x.GetByAliasAsync(_database.Object, "heroBlock")).ReturnsAsync(definition);

        var result = Assert.IsType<OkObjectResult>(await _controller.Index("heroBlock"));

        var data = result.Json().GetProperty("data");
        Assert.Equal(4, data.GetProperty("id").GetInt32());
        Assert.Equal("heroBlock", data.GetProperty("contentTypeAlias").GetString());
    }

    [Fact]
    public async Task Index_WhenNotFound_ReturnsAnEmptyObject()
    {
        var result = Assert.IsType<OkObjectResult>(await _controller.Index("missing"));

        Assert.Equal("{}", result.Json().GetRawText());
    }

    [Fact]
    public async Task Index_WhenTheLookupFails_Returns500()
    {
        _definitionService.Setup(x => x.GetByAliasAsync(_database.Object, It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("db down"));

        (await _controller.Index("heroBlock")).AssertServerError("Internal server error");
    }

    [Fact]
    public async Task Categories_ReturnsTheDistinctCategories()
    {
        _definitionService.Setup(x => x.GetCategories(_database.Object)).ReturnsAsync(["Content", "Containers"]);

        var result = Assert.IsType<OkObjectResult>(await _controller.Categories());

        Assert.Equal(["Content", "Containers"], result.Json().GetProperty("data").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public async Task Categories_WhenTheLookupFails_Returns500()
    {
        _definitionService.Setup(x => x.GetCategories(_database.Object)).ThrowsAsync(new InvalidOperationException("db down"));

        (await _controller.Categories()).AssertServerError("Internal server error");
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_WithInvalidModel_ReturnsBadRequest()
    {
        _controller.ModelState.AddModelError("Type", "Required");

        Assert.IsType<BadRequestObjectResult>(await _controller.Create(CreateRequest()));
        _definitionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_WhenTheAliasAlreadyHasADefinition_ReturnsConflict()
    {
        _definitionService.Setup(x => x.GetByAliasAsync(_database.Object, "heroBlock")).ReturnsAsync(Dtos.Definition("heroBlock"));

        var result = Assert.IsType<ConflictObjectResult>(await _controller.Create(CreateRequest()));

        Assert.Contains("heroBlock", Assert.IsType<string>(result.Value));
        _definitionService.Verify(x => x.CreateAsync(It.IsAny<IUmbracoDatabase>(), It.IsAny<BlockFarmEditorDefinitionDTO>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Create_PersistsTheRequest_AsTheSignedInUser_AndRefreshesTheCache()
    {
        var userKey = _userService.AddUser("editor@example.test");
        _controller.SignedInAs("editor@example.test");
        BlockFarmEditorDefinitionDTO? created = null;
        _definitionService
            .Setup(x => x.CreateAsync(_database.Object, It.IsAny<BlockFarmEditorDefinitionDTO>(), userKey))
            .Callback((IUmbracoDatabase _, BlockFarmEditorDefinitionDTO dto, Guid _) => created = dto)
            .ReturnsAsync((IUmbracoDatabase _, BlockFarmEditorDefinitionDTO dto, Guid _) => dto);

        var result = Assert.IsType<CreatedAtActionResult>(await _controller.Create(CreateRequest()));

        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Key);
        Assert.Equal("heroBlock", created.ContentTypeAlias);
        Assert.Equal("partial", created.Type);
        Assert.Equal("~/Views/Partials/Hero.cshtml", created.ViewPath);
        Assert.Equal("Content", created.Category);
        Assert.True(created.Enabled);
        Assert.Equal(userKey, created.CreatedBy);
        Assert.Equal(userKey, created.UpdatedBy);

        Assert.Equal(nameof(BlockFarmEditorDefinitionController.Index), result.ActionName);
        Assert.Equal("heroBlock", result.RouteValues!["alias"]);
        Assert.Equal("heroBlock", result.Json().GetProperty("data").GetProperty("contentTypeAlias").GetString());
        _blockDefinitionService.Verify(x => x.ClearCache(), Times.Once);
    }

    [Fact]
    public async Task Create_WhenPersistingFails_Returns500_AndLeavesTheCacheAlone()
    {
        _definitionService
            .Setup(x => x.CreateAsync(_database.Object, It.IsAny<BlockFarmEditorDefinitionDTO>(), It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        (await _controller.Create(CreateRequest())).AssertServerError("Internal server error");
        _blockDefinitionService.Verify(x => x.ClearCache(), Times.Never);
    }

    #endregion

    #region Current user resolution

    private async Task<Guid> UserKeyUsedForUpdate()
    {
        Guid? used = null;
        _definitionService
            .Setup(x => x.UpdateAsync(_database.Object, 1, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Guid>()))
            .Callback((IUmbracoDatabase _, int _, string _, string _, string _, bool _, Guid user) => used = user)
            .ReturnsAsync(Dtos.Definition());

        await _controller.Update(1, UpdateRequest());

        return Assert.NotNull(used);
    }

    [Fact]
    public async Task AuditUser_IsTheSignedInBackofficeUser()
    {
        var userKey = _userService.AddUser("editor@example.test");
        _userService.AddAdminUser();
        _controller.SignedInAs("editor@example.test");

        Assert.Equal(userKey, await UserKeyUsedForUpdate());
    }

    [Fact]
    public async Task AuditUser_FallsBackToTheAdminUser_WhenTheUsernameIsUnknown()
    {
        var adminKey = _userService.AddAdminUser();
        _controller.SignedInAs("ghost@example.test");

        Assert.Equal(adminKey, await UserKeyUsedForUpdate());
    }

    [Fact]
    public async Task AuditUser_IsEmpty_ForUnauthenticatedRequests()
    {
        _userService.AddAdminUser();

        Assert.Equal(Guid.Empty, await UserKeyUsedForUpdate());
    }

    [Fact]
    public async Task AuditUser_IsEmpty_WhenTheUserLookupFails()
    {
        _userService.Setup(x => x.GetByUsername(It.IsAny<string>())).Throws(new InvalidOperationException("db down"));
        _controller.SignedInAs("editor@example.test");

        Assert.Equal(Guid.Empty, await UserKeyUsedForUpdate());
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_WithInvalidModel_ReturnsBadRequest()
    {
        _controller.ModelState.AddModelError("Type", "Required");

        Assert.IsType<BadRequestObjectResult>(await _controller.Update(1, UpdateRequest()));
        _definitionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_WhenTheDefinitionDoesNotExist_ReturnsNotFound()
    {
        var result = Assert.IsType<NotFoundObjectResult>(await _controller.Update(99, UpdateRequest()));

        Assert.Contains("99", Assert.IsType<string>(result.Value));
        _blockDefinitionService.Verify(x => x.ClearCache(), Times.Never);
    }

    [Fact]
    public async Task Update_PassesTheRequestThrough_AndRefreshesTheCache()
    {
        var updated = Dtos.Definition("heroBlock", category: "New", id: 7);
        _definitionService
            .Setup(x => x.UpdateAsync(_database.Object, 7, "viewcomponent", "new.cshtml", "New", false, It.IsAny<Guid>()))
            .ReturnsAsync(updated);

        var result = Assert.IsType<OkObjectResult>(await _controller.Update(7, UpdateRequest()));

        Assert.Equal(7, result.Json().GetProperty("data").GetProperty("id").GetInt32());
        _blockDefinitionService.Verify(x => x.ClearCache(), Times.Once);
    }

    [Fact]
    public async Task Update_WhenPersistingFails_Returns500()
    {
        _definitionService
            .Setup(x => x.UpdateAsync(_database.Object, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        (await _controller.Update(7, UpdateRequest())).AssertServerError("Internal server error");
    }

    #endregion

    #region ExportPackage

    [Fact]
    public async Task ExportPackage_WritesTheSelectedDefinitionsToTheServerFolder()
    {
        var keys = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var package = new BlockFarmEditorExportPackageDTO { Definitions = [Dtos.Definition("a"), Dtos.Definition("b")] };
        _exportService.Setup(x => x.BuildExportPackageAsync(It.Is<IEnumerable<Guid>>(requested => requested.SequenceEqual(keys)))).ReturnsAsync(package);

        var result = Assert.IsType<OkObjectResult>(await _controller.ExportPackage(new ExportPackageRequest { DefinitionKeys = [.. keys.Select(x => x.ToString())] }));

        Assert.Equal(2, result.Json().GetProperty("definitionCount").GetInt32());
        _exportService.Verify(x => x.ExportToFolderAsync(package), Times.Once);
        _exportService.Verify(x => x.ExportToZipAsync(It.IsAny<BlockFarmEditorExportPackageDTO>()), Times.Never);
    }

    [Fact]
    public async Task ExportPackage_WithoutKeys_ExportsEverything()
    {
        _exportService.Setup(x => x.BuildExportPackageAsync(It.Is<IEnumerable<Guid>>(requested => !requested.Any()))).ReturnsAsync(new BlockFarmEditorExportPackageDTO());

        Assert.IsType<OkObjectResult>(await _controller.ExportPackage(new ExportPackageRequest { DefinitionKeys = null }));
    }

    [Fact]
    public async Task ExportPackage_WithDownload_AlsoReturnsTheZip()
    {
        var package = new BlockFarmEditorExportPackageDTO();
        var zip = new byte[] { 0x50, 0x4B, 0x05, 0x06 };
        _exportService.Setup(x => x.BuildExportPackageAsync(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(package);
        _exportService.Setup(x => x.ExportToZipAsync(package)).ReturnsAsync(zip);

        var result = Assert.IsType<FileContentResult>(await _controller.ExportPackage(new ExportPackageRequest { Download = true }));

        Assert.Same(zip, result.FileContents);
        Assert.Equal("application/zip", result.ContentType);
        Assert.Matches(@"^blockfarmeditor-export-\d{14}\.zip$", result.FileDownloadName);
        _exportService.Verify(x => x.ExportToFolderAsync(package), Times.Once);
    }

    [Fact]
    public async Task ExportPackage_WithAMalformedKey_Returns500_WithoutExporting()
    {
        (await _controller.ExportPackage(new ExportPackageRequest { DefinitionKeys = ["not-a-guid"] })).AssertServerError("Failed to export package");

        _exportService.VerifyNoOtherCalls();
    }

    #endregion

    #region ImportPackage

    private static IFormFile Upload(byte[] content)
    {
        var file = new Mock<IFormFile>();
        file.SetupGet(x => x.Length).Returns(content.Length);
        file.Setup(x => x.OpenReadStream()).Returns(new MemoryStream(content));
        return file.Object;
    }

    private void ImportReturns(BlockFarmEditorExportPackageDTO package, ImportResultDTO result) =>
        _exportService
            .Setup(x => x.ImportPackageAsync(package, It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(result);

    [Fact]
    public async Task ImportPackage_WithAnUpload_ImportsTheZip_AndReportsWhatWasImported()
    {
        var package = new BlockFarmEditorExportPackageDTO();
        _exportService.Setup(x => x.ReadFromZipAsync(It.IsAny<Stream>())).ReturnsAsync(package);
        ImportReturns(package, new ImportResultDTO
        {
            Definitions = { Created = 1, Updated = 2, Skipped = 9 },
            ElementTypes = { Created = 3, Failed = 9 },
            Compositions = { Updated = 4 },
            DataTypes = { Created = 5 },
            PartialViews = { Updated = 6 }
        });

        var result = Assert.IsType<OkObjectResult>(await _controller.ImportPackage(Upload([1, 2, 3])));

        var json = result.Json();
        Assert.Equal(3, json.GetProperty("definitions").GetInt32());
        Assert.Equal(3, json.GetProperty("elementTypes").GetInt32());
        Assert.Equal(4, json.GetProperty("compositions").GetInt32());
        Assert.Equal(5, json.GetProperty("dataTypes").GetInt32());
        Assert.Equal(6, json.GetProperty("partialViews").GetInt32());
        _exportService.Verify(x => x.ReadFromFolderAsync(), Times.Never);
        _blockDefinitionService.Verify(x => x.ClearCache(), Times.Once);
    }

    [Fact]
    public async Task ImportPackage_WithoutAnUpload_ImportsTheServerFolder()
    {
        var package = new BlockFarmEditorExportPackageDTO();
        _exportService.Setup(x => x.ReadFromFolderAsync()).ReturnsAsync(package);
        ImportReturns(package, new ImportResultDTO());

        Assert.IsType<OkObjectResult>(await _controller.ImportPackage(null));
        Assert.IsType<OkObjectResult>(await _controller.ImportPackage(Upload([])));

        _exportService.Verify(x => x.ReadFromFolderAsync(), Times.Exactly(2));
        _exportService.Verify(x => x.ReadFromZipAsync(It.IsAny<Stream>()), Times.Never);
    }

    [Fact]
    public async Task ImportPackage_DefaultsToOverwritingEverythingExceptDataTypes()
    {
        var package = new BlockFarmEditorExportPackageDTO();
        _exportService.Setup(x => x.ReadFromFolderAsync()).ReturnsAsync(package);
        ImportReturns(package, new ImportResultDTO());

        await _controller.ImportPackage(null);

        _exportService.Verify(x => x.ImportPackageAsync(package, true, true, true, true, false), Times.Once);
    }

    [Fact]
    public async Task ImportPackage_PassesEachOverwriteFlagToTheRightParameter()
    {
        var package = new BlockFarmEditorExportPackageDTO();
        _exportService.Setup(x => x.ReadFromFolderAsync()).ReturnsAsync(package);
        ImportReturns(package, new ImportResultDTO());

        await _controller.ImportPackage(null, overwriteElementTypes: false, overwriteCompositions: true, overwriteBlockDefinitions: false, overwritePartialViews: true, overwriteDataTypes: true);

        _exportService.Verify(x => x.ImportPackageAsync(package, false, true, false, true, true), Times.Once);
    }

    [Fact]
    public async Task ImportPackage_WhenTheImportFails_Returns500_AndLeavesTheCacheAlone()
    {
        _exportService.Setup(x => x.ReadFromFolderAsync()).ThrowsAsync(new InvalidOperationException("corrupt"));

        (await _controller.ImportPackage(null)).AssertServerError("Failed to import package");
        _blockDefinitionService.Verify(x => x.ClearCache(), Times.Never);
    }

    #endregion

    #region Exportable

    [Fact]
    public async Task Exportable_ListsTheCachedDefinitions_WithTheirElementTypeDetails()
    {
        var hero = Definitions.Expanded("heroBlock", Guid.NewGuid(), category: "Content", name: "Hero", icon: "icon-star", description: "A hero");
        _blockDefinitionService.Setup(x => x.RetrieveBlockFarmEditorDefinitions(false)).Returns(new Dictionary<Guid, Core.Models.BlockFarmEditorDefinitionExpanded> { [hero.ContentType!.Key] = hero });

        var result = Assert.IsType<OkObjectResult>(await _controller.Exportable());

        var item = Assert.Single(result.Json().EnumerateArray());
        Assert.Equal(hero.Key.ToString(), item.GetProperty("key").GetString());
        Assert.Equal("Hero", item.GetProperty("name").GetString());
        Assert.Equal("heroBlock", item.GetProperty("alias").GetString());
        Assert.Equal("Content", item.GetProperty("category").GetString());
        Assert.True(item.GetProperty("enabled").GetBoolean());
        Assert.Equal("icon-star", item.GetProperty("icon").GetString());
        Assert.Equal("A hero", item.GetProperty("description").GetString());
    }

    [Fact]
    public async Task Exportable_WhenTheLookupFails_Returns500()
    {
        _blockDefinitionService.Setup(x => x.RetrieveBlockFarmEditorDefinitions(It.IsAny<bool>())).Throws(new InvalidOperationException("db down"));

        (await _controller.Exportable()).AssertServerError("Internal server error");
    }

    #endregion
}
