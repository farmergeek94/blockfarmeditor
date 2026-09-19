using BlockFarmEditor.Umbraco.Core.Attributes;
using BlockFarmEditor.Umbraco.Tests.Services;

// BlockDefinitionService discovers blocks through assembly-level attributes, so the test assembly
// declares a few, exactly like a consuming site would. BlockDefinitionServiceTests scans this assembly.
[assembly: BlockFarmEditorDefinition(BlockDefinitionServiceTests.HeroAlias, typeof(BlockDefinitionServiceTests.HeroViewComponent))]
[assembly: BlockFarmEditorDefinition(BlockDefinitionServiceTests.HeroAlias, typeof(BlockDefinitionServiceTests.DuplicateHeroViewComponent))]
[assembly: BlockFarmEditorConfiguration(BlockDefinitionServiceTests.HeroAlias, "theme", typeof(BlockDefinitionServiceTests.ThemeConfig))]
[assembly: BlockFarmEditorConfiguration(BlockDefinitionServiceTests.HeroAlias, "size", typeof(BlockDefinitionServiceTests.SizeConfig))]
[assembly: BlockFarmEditorConfiguration(BlockDefinitionServiceTests.CardAlias, "theme", typeof(BlockDefinitionServiceTests.ThemeConfig))]
