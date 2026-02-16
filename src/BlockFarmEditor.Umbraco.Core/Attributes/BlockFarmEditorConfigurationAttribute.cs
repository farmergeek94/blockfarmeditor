using BlockFarmEditor.Umbraco.Core.Models.ConfigModels;

namespace BlockFarmEditor.Umbraco.Core.Attributes
{
    /// <summary>
    /// Attribute to specify the configuration type for a BlockFarmEditor data type.  Does not work for the Block List or Grid Type. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class BlockFarmEditorConfigurationAttribute : Attribute
    {
        public string Alias { get; }
        public string PropertyAlias { get; }
        private Type _configType { get; set; }

        public BlockFarmEditorConfigurationAttribute(string alias, string propertyAlias, Type configType)
        {
            if (!configType.Implements<IBlockFarmEditorConfig>())
            {
                throw new Exception($"{configType.AssemblyQualifiedName} is not a valid config type.  Config Class must inherit from {nameof(IBlockFarmEditorConfig)}");
            }

            Alias = alias;
            PropertyAlias = propertyAlias;
            _configType = configType;
        }

        public Type GetConfigType => _configType;
    }
}
