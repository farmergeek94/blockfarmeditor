using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFarmEditor.Umbraco.Library.Exceptions
{
    public class BlockFarmEditorError : Exception
    {
        public BlockFarmEditorError() { }
        public BlockFarmEditorError(string message) : base(message) { }
    }
}
