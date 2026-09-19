namespace BlockFarmEditor.Umbraco.Tests.Helpers;

internal sealed class TempDirectory : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("bfe-tests-").FullName;

    public string Combine(params string[] parts) => System.IO.Path.Combine([Path, .. parts]);

    public string WriteFile(string relativePath, string content)
    {
        var fullPath = Combine(relativePath.Split('/'));
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // best effort - the OS cleans the temp folder eventually
        }
    }
}

/// <summary>
/// Some production code reads and writes the relative "BlockFarmEditor" folder, i.e. relative to the process'
/// working directory. Tests for it switch the working directory to a temp folder, which is process-wide
/// state, so they all live in this non-parallel collection.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class WorkingDirectoryCollection
{
    public const string Name = "Working directory";
}

internal sealed class TempWorkingDirectory : IDisposable
{
    private readonly string _original = Directory.GetCurrentDirectory();
    private readonly TempDirectory _directory = new();

    public TempWorkingDirectory() => Directory.SetCurrentDirectory(_directory.Path);

    public string Path => _directory.Path;

    public string Combine(params string[] parts) => _directory.Combine(parts);

    public string WriteFile(string relativePath, string content) => _directory.WriteFile(relativePath, content);

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_original);
        _directory.Dispose();
    }
}
