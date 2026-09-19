using BlockFarmEditor.Umbraco.Core.DTO;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Library.Services;
using BlockFarmEditor.Umbraco.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Infrastructure.Persistence;

namespace BlockFarmEditor.Umbraco.Tests.Services;

/// <summary>Everything <see cref="BlockFarmEditorExportService"/> depends on, with a temp folder as the site root.</summary>
internal sealed class ExportServiceFixture : IDisposable
{
    public const int NewDataTypeId = 4242;
    public static readonly Guid AdminKey = Guid.NewGuid();

    public TempDirectory Site { get; } = new();
    public Mock<IBlockFarmEditorDefinitionService> DefinitionService { get; } = new();
    public Mock<IContentTypeService> ContentTypeService { get; } = new();
    public Mock<IDataTypeService> DataTypeService { get; } = new();
    public Mock<IFolderStructureService> FolderStructureService { get; } = new();
    public Mock<IUmbracoDatabase> Database { get; } = new();
    public Mock<IUserService> UserService { get; } = new();
    public List<IDataEditor> DataEditors { get; } = [];
    public List<BlockFarmEditorDefinitionDTO> Definitions { get; } = [];
    public List<IContentType> ContentTypes { get; } = [];
    public BlockFarmEditorExportService Service { get; }

    public ExportServiceFixture()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.ContentRootPath).Returns(Site.Path);

        var admin = new Mock<IUser>();
        admin.SetupGet(x => x.Key).Returns(AdminKey);
        UserService.Setup(x => x.GetUserById(-1)).Returns(admin.Object);

        DefinitionService.Setup(x => x.GetAllAsync(Database.Object)).ReturnsAsync(() => Definitions.ToAsyncEnumerable());
        DefinitionService
            .Setup(x => x.GetByAliasAsync(Database.Object, It.IsAny<string>()))
            .ReturnsAsync((IUmbracoDatabase _, string alias) => Definitions.FirstOrDefault(x => x.ContentTypeAlias == alias));

        ContentTypeService.Setup(x => x.GetAll()).Returns(() => ContentTypes);
        ContentTypeService.Setup(x => x.Get(It.IsAny<string>())).Returns((string alias) => ContentTypes.FirstOrDefault(x => x.Alias == alias));
        ContentTypeService
            .Setup(x => x.CreateAsync(It.IsAny<IContentType>(), It.IsAny<Guid>()))
            .ReturnsAsync((IContentType contentType, Guid _) =>
            {
                ContentTypes.Add(contentType);
                return Attempt.Succeed(ContentTypeOperationStatus.Success);
            });

        DataTypeService
            .Setup(x => x.CreateAsync(It.IsAny<IDataType>(), It.IsAny<Guid>()))
            .ReturnsAsync((IDataType dataType, Guid _) =>
            {
                dataType.Id = NewDataTypeId; // saving is what gives a data type its identity
                return Attempt.SucceedWithStatus(DataTypeOperationStatus.Success, dataType);
            });

        FolderStructureService.Setup(x => x.EnsureContentTypeFolderStructureAsync(It.IsAny<string?>())).ReturnsAsync(-1);
        FolderStructureService.Setup(x => x.EnsureDataTypeFolderStructureAsync(It.IsAny<string?>())).ReturnsAsync(-1);

        Service = new BlockFarmEditorExportService(
            DefinitionService.Object,
            ContentTypeService.Object,
            DataTypeService.Object,
            FolderStructureService.Object,
            Database.ToFactory().Object,
            environment.Object,
            UserService.Object,
            UmbracoModels.ShortStringHelper,
            new PropertyEditorCollection(new DataEditorCollection(() => DataEditors)),
            Mock.Of<IConfigurationEditorJsonSerializer>(),
            NullLogger<BlockFarmEditorExportService>.Instance);
    }

    public void AddDataEditor(string alias)
    {
        var configurationEditor = new Mock<IConfigurationEditor>();
        configurationEditor.SetupGet(x => x.DefaultConfiguration).Returns(new Dictionary<string, object>());

        var editor = new Mock<IDataEditor>();
        editor.SetupGet(x => x.Alias).Returns(alias);
        editor.Setup(x => x.GetConfigurationEditor()).Returns(configurationEditor.Object);
        DataEditors.Add(editor.Object);
    }

    public Mock<IDataType> AddExistingDataType(Guid key, string name = "Textstring", IDictionary<string, object>? configuration = null)
    {
        var dataType = UmbracoModels.DataType(key, name, configuration: configuration);
        DataTypeService.Setup(x => x.GetAsync(key)).ReturnsAsync(dataType.Object);
        return dataType;
    }

    /// <summary>A real, persisted-looking content type, for exercising the update path against Umbraco's own model.</summary>
    public ContentType AddExistingContentType(string alias, params PropertyGroup[] groups)
    {
        var contentType = new ContentType(UmbracoModels.ShortStringHelper, -1)
        {
            Key = Guid.NewGuid(),
            Alias = alias,
            Name = $"Old {alias}",
            IsElement = true
        };
        foreach (var group in groups)
        {
            contentType.PropertyGroups.Add(group);
        }

        ContentTypes.Add(contentType);
        return contentType;
    }

    public void Dispose() => Site.Dispose();
}
