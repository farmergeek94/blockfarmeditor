
using BlockFarmEditor.RCL.Library.Attributes;
using BlockFarmEditor.RCL.Library.Editors;
using BlockFarmEditor.RCL.Models.BuilderModels;
using BlockFarmEditor.Block.RichText.Components.Blocks.RichTextEditor;
using Umbraco.Cms.Core;

[assembly: BlockFarmEditorBlock("blockfarmeditor.richtexteditor", "Rich Text Editor", PropertiesType = typeof(RichTextEditorProperties), ViewPath = "~/Components/Blocks/RichTextEditor/RichTextEditor.cshtml", Icon = "icon-text")]

namespace BlockFarmEditor.Block.RichText.Components.Blocks.RichTextEditor
{
    public class RichTextEditorProperties : IBuilderProperties
    {
        [BlockFarmEditorDataType("Richtext editor", "Content")]
        public RichTextEditorValue? Html { get; set; }
    }


}
