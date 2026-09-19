using BlockFarmEditor.Umbraco.Core.Database.BlockFarmEditorDefinitions;
using BlockFarmEditor.Umbraco.Core.Database.BlockFarmEditorLayouts;
using BlockFarmEditor.Umbraco.Core.DTO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NPoco;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseModelDefinitions;
using Umbraco.Cms.Infrastructure.Persistence.SqlSyntax;
using ColumnInfo = Umbraco.Cms.Infrastructure.Persistence.SqlSyntax.ColumnInfo;

namespace BlockFarmEditor.Umbraco.Core.Tests.Database;

public class MigrationTests
{
    private readonly Mock<IUmbracoDatabase> _database = new();
    private readonly Mock<ISqlSyntaxProvider> _sqlSyntax = new();
    private readonly Mock<IMigrationContext> _context = new();
    private readonly List<string> _executedSql = [];

    public MigrationTests()
    {
        var sqlContext = new Mock<ISqlContext>();
        sqlContext.SetupGet(x => x.SqlSyntax).Returns(_sqlSyntax.Object);
        sqlContext.SetupGet(x => x.DatabaseType).Returns(DatabaseType.SQLite);

        _database.SetupGet(x => x.SqlContext).Returns(sqlContext.Object);
        _database.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<object[]>()))
            .Callback((string sql, object[] _) => _executedSql.Add(sql))
            .Returns(0);
        _database.Setup(x => x.Execute(It.IsAny<Sql>()))
            .Callback((Sql sql) => _executedSql.Add(sql.SQL))
            .Returns(0);

        _context.SetupGet(x => x.Database).Returns(_database.Object);
        _context.SetupGet(x => x.SqlContext).Returns(sqlContext.Object);
        _context.SetupGet(x => x.Logger).Returns(NullLogger<IMigrationContext>.Instance);

        _sqlSyntax.Setup(x => x.GetTablesInSchema(It.IsAny<IDatabase>())).Returns([]);
        _sqlSyntax.Setup(x => x.GetColumnsInSchema(It.IsAny<IDatabase>())).Returns([]);
    }

    private void ExistingTables(params string[] tables) =>
        _sqlSyntax.Setup(x => x.GetTablesInSchema(It.IsAny<IDatabase>())).Returns(tables);

    private void ExistingColumns(params (string Table, string Column)[] columns) =>
        _sqlSyntax.Setup(x => x.GetColumnsInSchema(It.IsAny<IDatabase>()))
            .Returns(columns.Select((c, i) => new ColumnInfo(c.Table, c.Column, i, "NULL", "YES", "DATETIME")));

    private void VerifyTableCreated(string tableName, Times times) =>
        _sqlSyntax.Verify(x => x.HandleCreateTable(_database.Object, It.Is<TableDefinition>(t => t.Name == tableName), It.IsAny<bool>()), times);

    [Fact]
    public async Task DefinitionTable_WhenTableIsMissing_CreatesItFromTheDto()
    {
        ExistingTables("umbracoNode", BlockFarmEditorLayoutDTO.TableName);

        await new BlockFarmEditorDefinitionTable(_context.Object).RunAsync();

        VerifyTableCreated(BlockFarmEditorDefinitionDTO.TableName, Times.Once());
    }

    [Fact]
    public async Task DefinitionTable_WhenTableAlreadyExists_DoesNotCreateIt()
    {
        ExistingTables(BlockFarmEditorDefinitionDTO.TableName);

        await new BlockFarmEditorDefinitionTable(_context.Object).RunAsync();

        VerifyTableCreated(BlockFarmEditorDefinitionDTO.TableName, Times.Never());
    }

    [Fact]
    public async Task DefinitionTable_ExistenceCheck_IsCaseInsensitive()
    {
        ExistingTables(BlockFarmEditorDefinitionDTO.TableName.ToLowerInvariant());

        await new BlockFarmEditorDefinitionTable(_context.Object).RunAsync();

        VerifyTableCreated(BlockFarmEditorDefinitionDTO.TableName, Times.Never());
    }

    [Fact]
    public async Task LayoutTable_WhenTableIsMissing_CreatesItFromTheDto()
    {
        ExistingTables("umbracoNode", BlockFarmEditorDefinitionDTO.TableName);

        await new BlockFarmEditorLayoutTable(_context.Object).RunAsync();

        VerifyTableCreated(BlockFarmEditorLayoutDTO.TableName, Times.Once());
    }

    [Fact]
    public async Task LayoutTable_WhenTableAlreadyExists_DoesNotCreateIt()
    {
        ExistingTables(BlockFarmEditorLayoutDTO.TableName);

        await new BlockFarmEditorLayoutTable(_context.Object).RunAsync();

        VerifyTableCreated(BlockFarmEditorLayoutDTO.TableName, Times.Never());
    }

    [Fact]
    public async Task DeletedAtColumn_WhenColumnAlreadyExists_DoesNothing()
    {
        ExistingColumns((BlockFarmEditorDefinitionDTO.TableName, BlockFarmEditorDefinitionTableDeletedAt.ColumnName));

        await new BlockFarmEditorDefinitionTableDeletedAt(_context.Object).RunAsync();

        Assert.Empty(_executedSql);
    }

    [Fact]
    public async Task DeletedAtColumn_WhenColumnIsMissing_AddsNullableDateTimeColumn()
    {
        ExistingColumns((BlockFarmEditorDefinitionDTO.TableName, "Category"), ("SomeOtherTable", BlockFarmEditorDefinitionTableDeletedAt.ColumnName));

        await new BlockFarmEditorDefinitionTableDeletedAt(_context.Object).RunAsync();

        var sql = Assert.Single(_executedSql);
        Assert.Equal("ALTER TABLE BlockFarmEditorDefinition ADD COLUMN DeletedAt DATETIME NULL;", sql);
    }

    [Fact]
    public void DeletedAtColumn_NameMatchesTheDtoMapping()
    {
        var column = typeof(BlockFarmEditorDefinitionDTO)
            .GetProperty(nameof(BlockFarmEditorDefinitionDTO.DeleteDate))!
            .GetCustomAttributes(typeof(ColumnAttribute), false)
            .Cast<ColumnAttribute>()
            .Single();

        Assert.Equal(BlockFarmEditorDefinitionTableDeletedAt.ColumnName, column.Name);
    }
}
