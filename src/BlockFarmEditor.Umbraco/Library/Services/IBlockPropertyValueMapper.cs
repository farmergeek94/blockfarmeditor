using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using BlockFarmEditor.Umbraco.Library.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace BlockFarmEditor.Umbraco.Library.Services
{
    /// <summary>
    /// Converts the property values of a block tree between the editor, the stored and the published formats.
    /// </summary>
    public interface IBlockPropertyValueMapper
    {
        /// <summary>
        /// Runs every block property through its value editor's FromEditor, turning editor values into stored values.  Updates the tree in place.
        /// </summary>
        void FromEditor(BlockData root);

        /// <summary>
        /// Runs every block property through its value editor's ToEditor, turning stored values into editor values.  Updates the tree in place.
        /// </summary>
        void ToEditor(BlockData root);

        /// <summary>
        /// Builds the published block (and its nested blocks) from stored values.  Returns null when the block has no valid unique.
        /// </summary>
        BlockDefinition<IPublishedElement>? ToBlockDefinition(BlockData block);
    }
}
