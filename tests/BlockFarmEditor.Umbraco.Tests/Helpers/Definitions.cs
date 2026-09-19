using BlockFarmEditor.Umbraco.Core.Attributes;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Core.Models;
using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace BlockFarmEditor.Umbraco.Tests.Helpers;

internal static class Definitions
{
    /// <summary>An enabled definition as <see cref="IBlockDefinitionService"/> would hand it out.</summary>
    public static BlockFarmEditorDefinitionExpanded Expanded(
        string alias,
        Guid contentTypeKey,
        string category = "Content",
        string viewPath = "~/Views/Partials/Block.cshtml",
        Type? viewComponentType = null,
        string? name = null,
        string? icon = null,
        string? description = null)
    {
        var contentType = new Mock<IContentType>();
        contentType.SetupGet(x => x.Key).Returns(contentTypeKey);
        contentType.SetupGet(x => x.Alias).Returns(alias);
        contentType.SetupGet(x => x.Name).Returns(name ?? alias);
        contentType.SetupGet(x => x.Icon).Returns(icon);
        contentType.SetupGet(x => x.Description).Returns(description);

        var expanded = BlockFarmEditorDefinitionExpanded.ExpandDto(Dtos.Definition(alias, category, viewPath: viewPath));
        expanded.ContentType = contentType.Object;
        expanded.DefinitionAttribute = viewComponentType == null ? null : new BlockFarmEditorDefinitionAttribute(alias, viewComponentType);
        return expanded;
    }

    public static Mock<IBlockDefinitionService> ToService(params BlockFarmEditorDefinitionExpanded[] definitions)
    {
        var service = new Mock<IBlockDefinitionService>();
        service
            .Setup(x => x.RetrieveBlockFarmEditorDefinitions(It.IsAny<bool>()))
            .Returns(definitions.ToDictionary(x => x.ContentType!.Key, x => x));
        return service;
    }

    public static IPublishedElement Element(Guid contentTypeKey, Guid? key = null)
    {
        var contentType = new Mock<IPublishedContentType>();
        contentType.SetupGet(x => x.Key).Returns(contentTypeKey);

        var element = new Mock<IPublishedElement>();
        element.SetupGet(x => x.Key).Returns(key ?? Guid.NewGuid());
        element.SetupGet(x => x.ContentType).Returns(contentType.Object);
        element.SetupGet(x => x.Properties).Returns([]);
        return element.Object;
    }

    public static BlockDefinition<IPublishedElement> Block(Guid contentTypeKey, params BlockDefinition<IPublishedElement>[] children) => new()
    {
        Properties = Element(contentTypeKey),
        Blocks = children
    };
}
