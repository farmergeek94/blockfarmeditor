using BlockFarmEditor.Umbraco.Core.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace BlockFarmEditor.Umbraco.TagHelpers
{
    [HtmlTargetElement("register-block-farm")]

    public class RegisterBlockFarmEditorTagHelper(IBlockFarmEditorContext blockFarmEditorContext, IUrlHelperFactory urlHelperFactory) : TagHelper
    {

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null; // Remove the tag name to prevent rendering a tag

            if (blockFarmEditorContext.IsEditMode)
            {
                var urlHelper = urlHelperFactory.GetUrlHelper(ViewContext);

                // Set the tag name to 'script' for the container
                output.Content.SetHtmlContent(@$"
                    <link rel='stylesheet' href='{urlHelper.Content("~/App_Plugins/BlockFarmEditor.ClientScripts.RCL/block-editor/dist/block-editor.css")}'>
                    <script>
                        window.blockFarmEditorUnique = '{blockFarmEditorContext.ContentUnique}';
                        window.blockFarmEditorBasePath = '{urlHelper.Content("~/").TrimEnd('/')}';
                    </script>
                    <script type='module' src='{urlHelper.Content("~/App_Plugins/BlockFarmEditor.ClientScripts.RCL/block-editor/dist/block-editor.js")}'></script>
                ");
            }
        }
    }
}
