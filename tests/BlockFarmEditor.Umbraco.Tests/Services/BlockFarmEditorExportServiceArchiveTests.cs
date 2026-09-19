using System.IO.Compression;
using System.Text;
using BlockFarmEditor.Umbraco.Core.DTO;
using BlockFarmEditor.Umbraco.Tests.Helpers;

namespace BlockFarmEditor.Umbraco.Tests.Services;

public class BlockFarmEditorExportServiceZipTests : IDisposable
{
    private readonly ExportServiceFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    private static void AssertSamplePackage(BlockFarmEditorExportPackageDTO original, BlockFarmEditorExportPackageDTO result)
    {
        Assert.Equal(original.Definitions.Select(x => x.Key).Order(), result.Definitions.Select(x => x.Key).Order());
        Assert.Equal(["heroBlock", "seoComposition"], result.ElementTypes.Select(x => x.Alias).Order());
        Assert.Equal(["seoComposition"], result.ElementTypes.Single(x => x.Alias == "heroBlock").CompositionAliases);
        Assert.Equal("title", result.ElementTypes.Single(x => x.Alias == "heroBlock").PropertyGroups.Single().PropertyTypes.Single().Alias);
        Assert.Equal("50", Assert.Single(Assert.Single(result.DataTypes).ConfigurationItems).Value);
        var partialView = Assert.Single(result.PartialViews);
        Assert.Equal("Partials/BlockFarm/Hero.cshtml", partialView.Path);
        Assert.Equal("<h1>@Model.Title</h1>", partialView.Content);
    }

    private static byte[] Zip(params (string Name, string Content)[] entries)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                using var writer = new StreamWriter(archive.CreateEntry(name).Open(), Encoding.UTF8);
                writer.Write(content);
            }
        }

        return stream.ToArray();
    }

    [Fact]
    public async Task ExportToZip_ContainsAManifest_AndOneFilePerItem()
    {
        var package = Packages.Sample();

        var bytes = await _fixture.Service.ExportToZipAsync(package);

        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        Assert.Equal(
            new[]
            {
                "package.xml",
                "Definitions/heroBlock.xml",
                "Definitions/cardBlock.xml",
                "ElementTypes/seoComposition.xml",
                "ElementTypes/heroBlock.xml",
                $"DataTypes/{package.DataTypes[0].Key}.xml",
                "PartialViews/Partials_BlockFarm_Hero.cshtml.xml"
            }.Order(),
            archive.Entries.Select(x => x.FullName).Order());
    }

    [Fact]
    public async Task ExportToZip_FlattensWindowsStylePartialViewPaths()
    {
        var package = new BlockFarmEditorExportPackageDTO { PartialViews = [new PartialViewExportDTO { Path = "Partials\\Hero.cshtml", Content = "x" }] };

        var bytes = await _fixture.Service.ExportToZipAsync(package);

        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        Assert.Contains(archive.Entries, x => x.FullName == "PartialViews/Partials_Hero.cshtml.xml");
    }

    [Fact]
    public async Task ExportToZip_OfAnEmptyPackage_StillContainsTheManifest()
    {
        var bytes = await _fixture.Service.ExportToZipAsync(new BlockFarmEditorExportPackageDTO());

        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        Assert.Equal("package.xml", Assert.Single(archive.Entries).FullName);
    }

    [Fact]
    public async Task ReadFromZip_RoundTripsAnExportedPackage()
    {
        var package = Packages.Sample();
        var bytes = await _fixture.Service.ExportToZipAsync(package);

        var result = await _fixture.Service.ReadFromZipAsync(new MemoryStream(bytes));

        AssertSamplePackage(package, result);
        Assert.Equal(package.ExportedAt, result.ExportedAt.ToUniversalTime());
    }

    [Fact]
    public async Task ReadFromZip_WithoutAManifest_FallsBackToTheIndividualFiles()
    {
        var package = Packages.Sample();
        var bytes = await _fixture.Service.ExportToZipAsync(package);
        using (var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read))
        {
            bytes = Zip(archive.Entries
                .Where(x => x.FullName != "package.xml")
                .Select(x => (x.FullName, new StreamReader(x.Open()).ReadToEnd()))
                .ToArray());
        }

        var result = await _fixture.Service.ReadFromZipAsync(new MemoryStream(bytes));

        AssertSamplePackage(package, result);
    }

    [Fact]
    public async Task ReadFromZip_Fallback_SkipsCorruptAndUnrelatedEntries()
    {
        var package = Packages.Sample();
        var exported = await _fixture.Service.ExportToZipAsync(package);
        string heroDefinition;
        using (var archive = new ZipArchive(new MemoryStream(exported), ZipArchiveMode.Read))
        {
            heroDefinition = new StreamReader(archive.GetEntry("Definitions/heroBlock.xml")!.Open()).ReadToEnd();
        }

        var bytes = Zip(
            ("Definitions/heroBlock.xml", heroDefinition),
            ("Definitions/corrupt.xml", "<not-closed"),
            ("Definitions/readme.txt", "ignore me"),
            ("SomewhereElse/heroBlock.xml", heroDefinition));

        var result = await _fixture.Service.ReadFromZipAsync(new MemoryStream(bytes));

        Assert.Equal("heroBlock", Assert.Single(result.Definitions).ContentTypeAlias);
        Assert.Empty(result.ElementTypes);
    }

    [Fact]
    public async Task ReadFromZip_OfAnEmptyArchive_ReturnsAnEmptyPackage()
    {
        var result = await _fixture.Service.ReadFromZipAsync(new MemoryStream(Zip()));

        Assert.Empty(result.Definitions);
        Assert.Empty(result.ElementTypes);
        Assert.Empty(result.DataTypes);
        Assert.Empty(result.PartialViews);
    }

    [Fact]
    public async Task ReadFromZip_OfSomethingThatIsNotAZip_Throws()
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => _fixture.Service.ReadFromZipAsync(new MemoryStream(Encoding.UTF8.GetBytes("not a zip"))));
    }
}

[Collection(WorkingDirectoryCollection.Name)]
public class BlockFarmEditorExportServiceFolderTests : IDisposable
{
    private readonly ExportServiceFixture _fixture = new();
    private readonly TempWorkingDirectory _workingDirectory = new();

    public void Dispose()
    {
        _workingDirectory.Dispose();
        _fixture.Dispose();
    }

    private string[] ExportedFiles(string folder) =>
        Directory.Exists(_workingDirectory.Combine("BlockFarmEditor", folder))
            ? [.. Directory.EnumerateFiles(_workingDirectory.Combine("BlockFarmEditor", folder)).Select(Path.GetFileName).Order()!]
            : [];

    [Fact]
    public async Task ExportToFolder_WritesOneFilePerItem_UnderTheBlockFarmEditorFolder()
    {
        var package = Packages.Sample();

        await _fixture.Service.ExportToFolderAsync(package);

        Assert.Equal(["cardBlock.xml", "heroBlock.xml"], ExportedFiles("Definitions"));
        Assert.Equal(["heroBlock.xml", "seoComposition.xml"], ExportedFiles("ElementTypes"));
        Assert.Equal([$"{package.DataTypes[0].Key}.xml"], ExportedFiles("DataTypes"));
        Assert.Equal(["Partials_BlockFarm_Hero.cshtml.xml"], ExportedFiles("PartialViews"));
    }

    [Fact]
    public async Task ExportToFolder_OfAnEmptyPackage_StillCreatesTheFolderStructure()
    {
        await _fixture.Service.ExportToFolderAsync(new BlockFarmEditorExportPackageDTO());

        foreach (var folder in new[] { "Definitions", "ElementTypes", "DataTypes", "PartialViews" })
        {
            Assert.True(Directory.Exists(_workingDirectory.Combine("BlockFarmEditor", folder)));
        }
    }

    [Fact]
    public async Task ExportToFolder_OverwritesAPreviousExport()
    {
        var package = Packages.Sample();
        await _fixture.Service.ExportToFolderAsync(package);
        package.Definitions[0].Category = "Changed";

        await _fixture.Service.ExportToFolderAsync(package);

        var result = await _fixture.Service.ReadFromFolderAsync();
        Assert.Equal("Changed", result.Definitions.Single(x => x.ContentTypeAlias == "heroBlock").Category);
    }

    [Fact]
    public async Task ReadFromFolder_RoundTripsAnExportedPackage()
    {
        var package = Packages.Sample();
        await _fixture.Service.ExportToFolderAsync(package);

        var result = await _fixture.Service.ReadFromFolderAsync();

        Assert.Equal(package.Definitions.Select(x => x.Key).Order(), result.Definitions.Select(x => x.Key).Order());
        Assert.Equal(["heroBlock", "seoComposition"], result.ElementTypes.Select(x => x.Alias).Order());
        Assert.Equal(package.DataTypes[0].Key, Assert.Single(result.DataTypes).Key);
        Assert.Equal("<h1>@Model.Title</h1>", Assert.Single(result.PartialViews).Content);
    }

    [Fact]
    public async Task ReadFromFolder_WhenNothingWasExported_ReturnsAnEmptyPackage()
    {
        var result = await _fixture.Service.ReadFromFolderAsync();

        Assert.Empty(result.Definitions);
        Assert.Empty(result.ElementTypes);
        Assert.Empty(result.DataTypes);
        Assert.Empty(result.PartialViews);
    }

    [Fact]
    public async Task ReadFromFolder_SkipsFilesItCannotRead()
    {
        await _fixture.Service.ExportToFolderAsync(Packages.Sample());
        _workingDirectory.WriteFile("BlockFarmEditor/Definitions/corrupt.xml", "<not-closed");
        _workingDirectory.WriteFile("BlockFarmEditor/Definitions/notes.txt", "ignore me");
        _workingDirectory.WriteFile("BlockFarmEditor/ElementTypes/corrupt.xml", "<not-closed");

        var result = await _fixture.Service.ReadFromFolderAsync();

        Assert.Equal(2, result.Definitions.Count);
        Assert.Equal(2, result.ElementTypes.Count);
    }
}
