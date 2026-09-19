using NPoco;
using Umbraco.Cms.Infrastructure.Persistence;

namespace BlockFarmEditor.Umbraco.Tests.Helpers;

/// <summary>
/// NPoco's friendly overloads (e.g. <c>SingleOrDefaultAsync(sql, arg)</c>) are extension methods that wrap
/// the statement in an <see cref="Sql"/> and forward to the <c>(Sql, CancellationToken)</c> interface
/// members, so that is what gets mocked.
/// </summary>
internal static class DatabaseMockExtensions
{
    public static void SetupSingleOrDefault<T>(this Mock<IUmbracoDatabase> database, string sqlFragment, object argument, T? result) where T : class =>
        database
            .Setup(x => x.SingleOrDefaultAsync<T>(
                It.Is<Sql>(sql => sql.SQL.Contains(sqlFragment) && sql.Arguments.Length == 1 && Equals(sql.Arguments[0], argument)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result!);

    public static void SetupQuery<T>(this Mock<IUmbracoDatabase> database, string sqlFragment, params T[] rows) =>
        database
            .Setup(x => x.QueryAsync<T>(It.Is<string>(sql => sql.Contains(sqlFragment)), It.IsAny<CancellationToken>()))
            .Returns(rows.ToAsyncEnumerable());

    public static Mock<IUmbracoDatabaseFactory> ToFactory(this Mock<IUmbracoDatabase> database)
    {
        var factory = new Mock<IUmbracoDatabaseFactory>();
        factory.Setup(x => x.CreateDatabase()).Returns(database.Object);
        return factory;
    }
}
