using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Xml;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;

namespace BlockFarmEditor.Umbraco.TagHelpers
{
    [HtmlTargetElement("block-area")]
    public class BlockAreaTagHelper(IBlockFarmEditorContext blockFarmEditorContext, IBlockFarmEditorRenderService blockFarmEditorRenderService, IHtmlHelper htmlHelper) : TagHelper
    {
        public string Identifier { get; set; }

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        public IEnumerable<string> AllowedBlocks { get; set; } = [];

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (blockFarmEditorContext.IsEditMode)
            {
                // Set the tag name to 'div' for the container
                output.TagName = "block-area";

                output.Attributes.SetAttribute("identifier", Identifier);

                output.Attributes.SetAttribute("unique", FromString(Identifier, new Guid(PageDefinition.GuidUnique)));

                if (AllowedBlocks.Any())
                {
                    output.Attributes.SetAttribute("allowedblocks", string.Join(",", AllowedBlocks));
                }
            }
            else
            {
                output.TagName = null; // Remove the tag name to prevent rendering a tag
                var currentScope = blockFarmEditorContext.GetBlockScope();
                var container = currentScope?.Block;
                if (container != null && container is IContainerDefinition containerDefinition && containerDefinition.Blocks?.Count() > 0)
                {
                    var blockSection = containerDefinition.Blocks.FirstOrDefault(x => x?.Unique == FromString(Identifier, new Guid(PageDefinition.GuidUnique)));
                    if (blockSection == null)
                    {
                        output.Content.AppendHtml("<div class=\"block-not-found\" style=\"display:none;\">Block not found</div>");
                        return;
                    }
                    foreach (var block in blockSection.Blocks)
                    {
                        if (block != null)
                        {
                            // Initialize the HtmlHelper with the current ViewContext
                            (htmlHelper as IViewContextAware)?.Contextualize(ViewContext);
                            // preserve the inheritence
                            using var scope = blockFarmEditorContext.GetBlockScope(block);
                            // Render the content of the block
                            var renderedComponent = await blockFarmEditorRenderService.RenderComponent(htmlHelper, block);

                            // return the rendered content
                            output.Content.AppendHtml(renderedComponent);
                        }
                    }  
                }
                else
                {
                    output.SuppressOutput();
                }

            }
        }
        private static Guid FromString(string input, Guid namespaceId)
        {
            // Convert namespace to big-endian
            byte[] ns = namespaceId.ToByteArray();
            SwapByteOrder(ns);

            // Combine namespace and input
            byte[] nameBytes = Encoding.UTF8.GetBytes(input);
            byte[] data = new byte[ns.Length + nameBytes.Length];
            Buffer.BlockCopy(ns, 0, data, 0, ns.Length);
            Buffer.BlockCopy(nameBytes, 0, data, ns.Length, nameBytes.Length);

            // SHA-1 hash
            byte[] hash = SHA1.HashData(data);

            // Set UUID version and variant
            hash[6] = (byte)((hash[6] & 0x0F) | 0x50); // version 5
            hash[8] = (byte)((hash[8] & 0x3F) | 0x80); // variant RFC 4122

            byte[] guidBytes = new byte[16];
            Array.Copy(hash, 0, guidBytes, 0, 16);
            SwapByteOrder(guidBytes);

            return new Guid(guidBytes);
        }

        private static void SwapByteOrder(byte[] guid)
        {
            void Swap(int a, int b) { (guid[a], guid[b]) = (guid[b], guid[a]); }
            Swap(0, 3); Swap(1, 2); Swap(4, 5); Swap(6, 7);
        }
    }
}
