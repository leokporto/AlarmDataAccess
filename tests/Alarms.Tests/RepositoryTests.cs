using Alarms.AdoNet;
using Alarms.Core;
using Alarms.Dapper;
using Alarms.EfCore;

namespace Alarms.Tests;

[Collection(SqlServerCollection.Name)]
public sealed class AdoNetAlarmRepositoryTests(SqlServerFixture fixture) : AlarmRepositoryContractTests
{
    protected override string ConnectionString => fixture.DbUpConnectionString;

    protected override IAlarmRepository CreateRepository() => new AdoNetAlarmRepository(ConnectionString);
}

[Collection(SqlServerCollection.Name)]
public sealed class DapperAlarmRepositoryTests(SqlServerFixture fixture) : AlarmRepositoryContractTests
{
    protected override string ConnectionString => fixture.DbUpConnectionString;

    protected override IAlarmRepository CreateRepository() => new DapperAlarmRepository(ConnectionString);
}

[Collection(SqlServerCollection.Name)]
public sealed class EfCoreAlarmRepositoryTests(SqlServerFixture fixture) : AlarmRepositoryContractTests
{
    private AlarmsDbContext? _db;

    protected override string ConnectionString => fixture.EfConnectionString;

    protected override IAlarmRepository CreateRepository()
    {
        _db = fixture.CreateEfContext();
        return new EfCoreAlarmRepository(_db);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_db is not null)
        {
            await _db.DisposeAsync();
        }
    }
}
