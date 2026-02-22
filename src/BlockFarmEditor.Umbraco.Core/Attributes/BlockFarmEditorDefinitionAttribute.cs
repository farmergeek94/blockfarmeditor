using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFarmEditor.Umbraco.Core.Attributes
{
    /// <summary>
    /// Defines a Block Farm block.
    /// </summary>
    /// <param name="identifier"></param>
    /// <param name="name"></param>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class BlockFarmEditorDefinitionAttribute : Attribute
    {
        [Obsolete("This constructor is deprecated. Please use the constructor that includes the viewComponentType parameter.")]
        public BlockFarmEditorDefinitionAttribute(string identifier)
        {
            Identifier = identifier;
        }

        public BlockFarmEditorDefinitionAttribute(string identifier, Type? viewComponentType = null)
        {
            Identifier = identifier;
            ViewComponentType = viewComponentType;
        }
        /// <summary>
        /// The identifier of the block. This should be unique across all blocks.
        /// </summary>
        public string Identifier { get; }
        /// <summary>
        /// The type of the view component to use for rendering the block.
        /// </summary>
        public Type? ViewComponentType { get; set; }
    }
}
