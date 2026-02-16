using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Packaging;

namespace BlockFarmEditor.Umbraco.Library.Migration;

internal class BlockFarmEditorInstall(
    IPackagingService packagingService
        , IMediaService mediaService
        , MediaFileManager mediaFileManager
        , MediaUrlGeneratorCollection mediaUrlGenerators
        , IShortStringHelper shortStringHelper
        , IContentTypeBaseServiceProvider contentTypeBaseServiceProvider
        , IMigrationContext context
        , IOptions<PackageMigrationSettings> packageMigrationsSettings
        ) : AsyncPackageMigrationBase(packagingService, mediaService, mediaFileManager, mediaUrlGenerators, shortStringHelper, contentTypeBaseServiceProvider, context, packageMigrationsSettings)
{
  protected override Task MigrateAsync()
  {
        // Imports the main package into your Umbraco installation
        // This will create the necessary configurations for the Bootstrap blocks
        ImportPackage.FromEmbeddedResource<BlockFarmEditorInstall>().Do();
    return Task.CompletedTask;
  }
}