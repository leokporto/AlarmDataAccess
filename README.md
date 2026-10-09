# AlarmsDataAccess

[![CI](https://github.com/leokporto/AlarmDataAccess/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/leokporto/AlarmDataAccess/actions/workflows/ci.yml)

One repository interface, three implementations: **ADO.NET**, **Dapper** and **EF Core**, side by side.
The schema is created two ways: SQL-first with **DbUp** and code-first with **EF Core migrations**.
Every implementation runs the same contract tests against a real SQL Server started by **Testcontainers**.

The goal is a small, readable comparison of how each library expresses the same operations. It
isn't a framework, and it makes no performance claims.

## Running it

Requirements: the [.NET 10 SDK](https://dotnet.microsoft.com/download) and a running Docker engine.

```bash
dotnet test
```

The first run pulls the `mcr.microsoft.com/mssql/server:2022-latest` image, which takes a while.

## Layout

```
src/
  Alarms.Core/        Alarm entity, enums, IAlarmRepository
  Alarms.AdoNet/      SqlConnection / SqlCommand / SqlDataReader
  Alarms.Dapper/      Plain SQL + Dapper
  Alarms.EfCore/      AlarmsDbContext, EF Core repository, EF migrations
  Alarms.Migrations/  DbUp runner + embedded .sql scripts
tests/
  Alarms.Tests/       Contract tests run against all three implementations
```

```csharp
public interface IAlarmRepository
{
    Task<int> AddAsync(Alarm alarm, CancellationToken ct = default);
    Task<Alarm?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Alarm>> GetByPriorityAsync(byte priority, CancellationToken ct = default);
    Task<bool> UpdatePriorityAsync(int id, byte priority, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
```

## Same query, three libraries

`GetByPriorityAsync` in each implementation:

**ADO.NET**: explicit parameter types, rows read by ordinal.

```csharp
await using var connection = await OpenAsync(ct);
await using var command = new SqlCommand($"{SelectColumns} WHERE Priority = @Priority", connection);
command.Parameters.Add("@Priority", SqlDbType.TinyInt).Value = priority;

var alarms = new List<Alarm>();
await using var reader = await command.ExecuteReaderAsync(ct);
while (await reader.ReadAsync(ct))
{
    alarms.Add(ReadAlarm(reader)); // reader.GetByte(5), reader.GetInt64(6), ...
}
return alarms;
```

**Dapper**: the same SQL, but Dapper maps the columns. `Timestamp_UTCTicks AS TimestampUtcTicks` handles the one column whose name differs.

```csharp
await using var connection = new SqlConnection(connectionString);
var alarms = await connection.QueryAsync<Alarm>(
    new CommandDefinition($"{SelectColumns} WHERE Priority = @priority", new { priority }, cancellationToken: ct));
return alarms.AsList();
```

**EF Core**: LINQ, with no change tracking for a read-only query.

```csharp
await db.Alarms.AsNoTracking().Where(a => a.Priority == priority).ToListAsync(ct);
```

Writes follow the same pattern. ADO.NET and Dapper use `OUTPUT INSERTED.Id` to return the new id.
EF Core uses `ExecuteUpdateAsync` / `ExecuteDeleteAsync`, so updates and deletes run as a single
statement without loading the entity first.

## Two ways to create the schema

The schema is identical either way, so ADO.NET and Dapper could run against either database. The
tests keep the two separate to show that both approaches produce the same result.

**DbUp** (`src/Alarms.Migrations/Scripts/0001_CreateAlarms.sql`): you write the SQL. DbUp records
applied scripts in `dbo.SchemaVersions`, so `AlarmsMigrator.Run(connectionString)` is safe to run on every startup.

```sql
CREATE TABLE dbo.Alarms
(
    Id                 int           IDENTITY(1,1) NOT NULL,
    Name               nvarchar(100) NOT NULL,
    State              tinyint       NOT NULL,
    Condition          tinyint       NOT NULL,
    Limit              float         NOT NULL,
    Priority           tinyint       NOT NULL,
    Timestamp_UTCTicks bigint        NOT NULL,
    Message            nvarchar(500) NOT NULL,
    CONSTRAINT PK_Alarms PRIMARY KEY (Id),
    CONSTRAINT CK_Alarms_Priority CHECK (Priority BETWEEN 1 AND 10)
);

CREATE UNIQUE INDEX UX_Alarms_Name ON dbo.Alarms (Name);
```

**EF Core** (`src/Alarms.EfCore/Migrations/*_InitialCreate.cs`): `dotnet ef migrations add InitialCreate`
generates this from the Fluent API model. It's tracked in `__EFMigrationsHistory` and applied with
`Database.MigrateAsync()`.

```csharp
migrationBuilder.CreateTable(
    name: "Alarms",
    schema: "dbo",
    columns: table => new
    {
        Id = table.Column<int>(type: "int", nullable: false)
            .Annotation("SqlServer:Identity", "1, 1"),
        Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
        State = table.Column<byte>(type: "tinyint", nullable: false),
        Condition = table.Column<byte>(type: "tinyint", nullable: false),
        Limit = table.Column<double>(type: "float", nullable: false),
        Priority = table.Column<byte>(type: "tinyint", nullable: false),
        Timestamp_UTCTicks = table.Column<long>(type: "bigint", nullable: false),
        Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_Alarms", x => x.Id);
        table.CheckConstraint("CK_Alarms_Priority", "[Priority] BETWEEN 1 AND 10");
    });

migrationBuilder.CreateIndex(
    name: "UX_Alarms_Name", schema: "dbo", table: "Alarms", column: "Name", unique: true);
```

## How the tests work

- One SQL Server container is shared by all tests through an xUnit collection fixture. On startup
  the fixture creates two databases in it: `AlarmsDbUp` with DbUp (used by the ADO.NET and Dapper
  tests) and `AlarmsEf` with `MigrateAsync()` (used by the EF Core tests).
- `AlarmRepositoryContractTests` is an abstract class that holds every test case. Each
  implementation has a three-line subclass that only says how to create its repository, so all
  three run exactly the same tests.
- The table is cleared before each test.
- The tests cover the generated id, a round trip of every field (enums and sub-millisecond ticks
  included), filtering by priority, update/delete returning `true`/`false` for existing/missing
  ids, the unique `Name` index, and the 1–10 `Priority` check constraint.

## Storage choices

- **Enums as `tinyint`.** `eAlarmState` and `AlarmCondition` are `byte`-backed and stored as
  numbers. EF Core maps them with `HasConversion<byte>()`, Dapper converts them automatically,
  and ADO.NET casts them explicitly.
- **Priority is a plain number from 1 to 10** (`tinyint`). The database enforces the range with a
  `CHECK` constraint, so all three implementations reject out-of-range values the same way.
- **Timestamps as `bigint` UTC ticks.** `Timestamp_UTCTicks` stores `DateTime.UtcNow.Ticks`
  rather than a `datetime2`. This is common in SCADA/historian systems. Integers are cheap to
  index, compare and sort. The value is UTC by definition, so there's no timezone or DST
  ambiguity. It keeps full 100 ns precision, and it matches what many legacy systems and
  wire protocols already exchange. The cost is readability in ad-hoc SQL: convert with
  `new DateTime(ticks, DateTimeKind.Utc)` in code.

## License

[MIT](LICENSE)
