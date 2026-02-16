using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.PublishedContent;
using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;

namespace BlockFarmEditor.Umbraco.Core.Interfaces
{
    public interface IBlockFarmEditorRenderService
    {
        Task<IHtmlContent?> RenderComponent<T>(IHtmlHelper htmlHelper, BlockDefinition<T> element) where T : IPublishedElement;
    }
}