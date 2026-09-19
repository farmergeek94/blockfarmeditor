using BlockFarmEditor.Umbraco.Core.Attributes;
using BlockFarmEditor.Umbraco.Core.Models.ConfigModels;

namespace BlockFarmEditor.Umbraco.Core.Tests.Attributes;

public class BlockFarmEditorConfigurationAttributeTests
{
    private class ValidConfig : IBlockFarmEditorConfig
    {
        public Task<IEnumerable<BlockFarmEditorConfigItem>> GetItems() => Task.FromResult<IEnumerable<BlockFarmEditorConfigItem>>([]);
    }

    private class NotAConfig { }

    [Fact]
    public void Constructor_WithValidConfigType_SetsProperties()
    {
        var attribute = new BlockFarmEditorConfigurationAttribute("heroBlock", "color", typeof(ValidConfig));

        Assert.Equal("heroBlock", attribute.Alias);
        Assert.Equal("color", attribute.PropertyAlias);
        Assert.Equal(typeof(ValidConfig), attribute.GetConfigType);
    }

    [Fact]
    public void Constructor_WithTypeNotImplementingConfigInterface_Throws()
    {
        var ex = Assert.Throws<Exception>(() => new BlockFarmEditorConfigurationAttribute("heroBlock", "color", typeof(NotAConfig)));

        Assert.Contains(nameof(IBlockFarmEditorConfig), ex.Message);
        Assert.Contains(nameof(NotAConfig), ex.Message);
    }

    [Fact]
    public void AttributeUsage_IsAssemblyLevelAndAllowsMultiple()
    {
        var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(typeof(BlockFarmEditorConfigurationAttribute), typeof(AttributeUsageAttribute))!;

        Assert.Equal(AttributeTargets.Assembly, usage.ValidOn);
        Assert.True(usage.AllowMultiple);
    }
}
