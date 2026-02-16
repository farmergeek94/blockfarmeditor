using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFarmEditor.Umbraco.Core.Models.ConfigModels
{
    public class DropdownEditorConfigItem
    {
        public DropdownEditorConfigItem() { }
        public DropdownEditorConfigItem(string name, string? value)
        {
            Name = name;
            Value = value;
        }
        public string Name { get; set; } = string.Empty;
        public string? Value { get; set; }
    }
}
