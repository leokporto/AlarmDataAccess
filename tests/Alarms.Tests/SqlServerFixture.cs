using Alarms.EfCore;
using Alarms.Migrations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace Alarms.Tests;

/// <summary>
/// Starts one SQL Server container for the whole test run and creates two databases in it,
/// one per migration approach: AlarmsDbUp (DbUp scripts) and AlarmsEf (EF Core migrations).
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public string DbUpConnectionString { get; private set; } = "";
    public string EfConnectionString { get; private set; } = "";

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        DbUpConnectionString = ForDatabase("AlarmsDbUp");
        EfConnectionString = ForDatabase("AlarmsEf");

        var result = AlarmsMigrator.Run(DbUpConnectionString);
        if (!result.Successful)
        {
            throw new InvalidOperationException("DbUp migration failed.", result.Error);
        }

        await using var db = CreateEfContext();
        await db.Database.MigrateAsync();
    }

    public AlarmsDbContext CreateEfContext() =>
        new(new DbContextOptionsBuilder<AlarmsDbContext>().UseSqlServer(EfConnectionString).Options);

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    private string ForDatabase(string database) =>
        new SqlConnectionStringBuilder(_container.GetConnectionString()) { InitialCatalog = database }.ConnectionString;
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SQL Server";
}
