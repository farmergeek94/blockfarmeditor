using BlockFarmEditor.Umbraco.Core.DTO;

namespace BlockFarmEditor.Umbraco.Core.Tests.DTO;

public class ImportResultDTOTests
{
    [Fact]
    public void NewResult_HasZeroedCountsForEveryItemType()
    {
        var result = new ImportResultDTO();

        foreach (var counts in new[] { result.DataTypes, result.ElementTypes, result.Compositions, result.Definitions, result.PartialViews })
        {
            Assert.NotNull(counts);
            Assert.Equal(0, counts.Total);
            Assert.Equal(0, counts.Imported);
        }
    }

    [Fact]
    public void ItemTypes_TrackCountsIndependently()
    {
        var result = new ImportResultDTO();

        result.DataTypes.Created++;

        Assert.Equal(1, result.DataTypes.Created);
        Assert.Equal(0, result.ElementTypes.Created);
        Assert.NotSame(result.DataTypes, result.ElementTypes);
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(1, 2, 3, 4, 10, 3)]
    [InlineData(5, 0, 0, 0, 5, 5)]
    [InlineData(0, 0, 2, 3, 5, 0)]
    public void Counts_ComputeTotalAndImported(int created, int updated, int skipped, int failed, int expectedTotal, int expectedImported)
    {
        var counts = new ImportItemCounts { Created = created, Updated = updated, Skipped = skipped, Failed = failed };

        Assert.Equal(expectedTotal, counts.Total);
        Assert.Equal(expectedImported, counts.Imported);
    }
}
