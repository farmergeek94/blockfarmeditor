using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFarmEditor.Umbraco.Core.Models.ConfigModels
{
    public class BlockFarmEditorConfigItem : IBlockFarmEditorConfigItem
    {
        public string Alias { get; set; } = string.Empty;
        public object? Value { get; set; }
    }

    public interface IBlockFarmEditorConfigItem
    {
        string Alias { get; }
        object? Value { get; set; }
    }
}
