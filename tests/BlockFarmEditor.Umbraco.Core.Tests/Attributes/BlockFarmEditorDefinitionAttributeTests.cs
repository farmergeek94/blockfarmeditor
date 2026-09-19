using BlockFarmEditor.Umbraco.Core.Attributes;

namespace BlockFarmEditor.Umbraco.Core.Tests.Attributes;

public class BlockFarmEditorDefinitionAttributeTests
{
    [Fact]
    public void Constructor_WithViewComponentType_SetsIdentifierAndType()
    {
        var attribute = new BlockFarmEditorDefinitionAttribute("heroBlock", typeof(string));

        Assert.Equal("heroBlock", attribute.Identifier);
        Assert.Equal(typeof(string), attribute.ViewComponentType);
    }

    [Fact]
    public void Constructor_WithNullViewComponentType_LeavesTypeNull()
    {
        var attribute = new BlockFarmEditorDefinitionAttribute("heroBlock", null);

        Assert.Equal("heroBlock", attribute.Identifier);
        Assert.Null(attribute.ViewComponentType);
    }

    [Fact]
    public void ObsoleteConstructor_SetsIdentifierOnly()
    {
#pragma warning disable CS0618 // exercising the deprecated constructor on purpose
        var attribute = new BlockFarmEditorDefinitionAttribute("heroBlock");
#pragma warning restore CS0618

        Assert.Equal("heroBlock", attribute.Identifier);
        Assert.Null(attribute.ViewComponentType);
    }

    [Fact]
    public void ViewComponentType_CanBeReassigned()
    {
        var attribute = new BlockFarmEditorDefinitionAttribute("heroBlock", null) { ViewComponentType = typeof(int) };

        Assert.Equal(typeof(int), attribute.ViewComponentType);
    }

    [Fact]
    public void AttributeUsage_IsAssemblyLevelAndAllowsMultiple()
    {
        var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(typeof(BlockFarmEditorDefinitionAttribute), typeof(AttributeUsageAttribute))!;

        Assert.Equal(AttributeTargets.Assembly, usage.ValidOn);
        Assert.True(usage.AllowMultiple);
    }
}
