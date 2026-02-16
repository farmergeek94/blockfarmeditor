using BlockFarmEditor.Umbraco.Core.Models;
using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;

namespace BlockFarmEditor.Umbraco.Core.Interfaces
{
    public interface IBlockFarmEditorContext
    {
        Guid ContentUnique { get; set; }
        PageDefinition? PageDefinition { get; set; }
        BlockFarmEditorContainerScope? _currentScope { get; set; }
        bool IsEditMode { get; }
        bool IsPreview { get; }

        BlockFarmEditorContainerScope? GetBlockScope();
        BlockFarmEditorContainerScope? GetBlockScope(IContainerDefinition block);
        Task SetPageDefinition();
        Task SetPageDefinition(IUmbracoContext context, IPublishedContent? content, string? domain, string? culture = null, bool editMode = false, bool preview = false);
    }
}