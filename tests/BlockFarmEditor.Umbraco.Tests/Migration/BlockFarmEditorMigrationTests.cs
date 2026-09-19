using BlockFarmEditor.Umbraco.Core.Database.BlockFarmEditorDefinitions;
using BlockFarmEditor.Umbraco.Core.Database.BlockFarmEditorLayouts;
using BlockFarmEditor.Umbraco.Library.Exceptions;
using BlockFarmEditor.Umbraco.Library.Migration;

namespace BlockFarmEditor.Umbraco.Tests.Migration;

public class BlockFarmEditorMigrationTests
{
    private readonly BlockFarmEditorMigration _plan = new();

    [Fact]
    public void Plan_IsNamedAfterThePackage()
    {
        Assert.Equal("Block Farm Editor", _plan.PackageName);
    }

    [Fact]
    public void Plan_AlwaysRunsRegardlessOfStoredState()
    {
        Assert.True(_plan.IgnoreCurrentState);
    }

    [Fact]
    public void Plan_RunsStepsInOrder_FromAnEmptyInstall()
    {
        var steps = new List<(string State, Type Migration)>();
        var state = _plan.InitialState;
        while (_plan.Transitions.TryGetValue(state, out var transition) && transition != null)
        {
            steps.Add((transition.TargetState, transition.MigrationType));
            state = transition.TargetState;
        }

        Assert.Equal(string.Empty, _plan.InitialState);
        Assert.Equal(
            [
                ("BlockFarmEditorDefinition-db", typeof(BlockFarmEditorDefinitionTable)),
                ("DeletedAt-db-column", typeof(BlockFarmEditorDefinitionTableDeletedAt)),
                ("BlockFarmEditorLayout-db", typeof(BlockFarmEditorLayoutTable)),
                ("BlockFarmEditor-Composer-Install-Update", typeof(BlockFarmEditorInstall)),
            ],
            steps);
        Assert.Equal("BlockFarmEditor-Composer-Install-Update", _plan.FinalState);
    }

    [Fact]
    public void InstallMigration_HasItsPackageXmlEmbedded()
    {
        var resources = typeof(BlockFarmEditorInstall).Assembly.GetManifestResourceNames();

        Assert.Contains(resources, x => x.EndsWith("Library.Migration.package.xml"));
    }
}

public class BlockFarmEditorErrorTests
{
    [Fact]
    public void CarriesItsMessage()
    {
        Assert.Equal("boom", new BlockFarmEditorError("boom").Message);
    }

    [Fact]
    public void IsAnException_AndCanBeCreatedWithoutAMessage()
    {
        Assert.IsAssignableFrom<Exception>(new BlockFarmEditorError());
    }
}
