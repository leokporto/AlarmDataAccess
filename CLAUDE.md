# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A small public portfolio sample. It implements one `IAlarmRepository` three ways (ADO.NET, Dapper, EF Core) and creates the same schema two ways (DbUp SQL scripts and EF Core migrations). Clarity matters more than features, so don't add layers (API, MediatR, DI containers) or features beyond what exists.

## Commands

Docker must be running for tests, because Testcontainers starts SQL Server 2022.

```bash
dotnet build
dotnet test
dotnet test --filter-method "*DapperAlarmRepositoryTests.AddAsync_rejects_duplicate_name"   # single test
dotnet test --filter-class "*EfCoreAlarmRepositoryTests"                                     # one implementation
dotnet tool restore                                                                          # local dotnet-ef 10
dotnet ef migrations add <Name> --project src/Alarms.EfCore
```

- `global.json` opts `dotnet test` into **Microsoft.Testing.Platform** (required by xUnit v3 4.x on the .NET 10 SDK). VSTest-style `--filter "FullyQualifiedName~..."` does not work. Use `--filter-method` / `--filter-class`.
- `TreatWarningsAsErrors` is on for all projects (`Directory.Build.props`).
- Package versions live only in `Directory.Packages.props` (Central Package Management), so `PackageReference`s have no `Version`. `Microsoft.Data.SqlClient` is deliberately pinned to 6.x because EF Core 10 and dbup-sqlserver 7 are built against 6.x.

## Architecture

- **The schema exists twice and must stay in sync.** Any change to `dbo.Alarms` needs a new DbUp script in `src/Alarms.Migrations/Scripts/` (embedded, numbered `NNNN_*.sql`, never edit applied ones) **and** a matching Fluent API change plus a generated EF migration in `src/Alarms.EfCore`. Constraint and index names (`PK_Alarms`, `UX_Alarms_Name`, `CK_Alarms_Priority`) match across both.
- **Column mapping is repeated in all three repositories.** The `Timestamp_UTCTicks` column maps to `Alarm.TimestampUtcTicks` explicitly: by ordinal in ADO.NET, with a SQL alias in Dapper, and with `HasColumnName` in EF. The enums (`eAlarmState`, `AlarmCondition` in `AlarmEnums.cs`) are `byte`-backed and stored as `tinyint`. `Priority` is a plain `byte` from 1 to 10, enforced by a CHECK constraint. A new column means editing all three repositories. The ADO.NET reader depends on the column order of its `SelectColumns`.
- **The entity has no attributes.** EF configuration is Fluent API only, in `AlarmsDbContext.OnModelCreating`. `AlarmsDbContextFactory` exists only for `dotnet ef` design-time use.
- **Tests:** `SqlServerFixture` (xUnit collection fixture) starts one container and creates two databases: `AlarmsDbUp` through `AlarmsMigrator.Run` (used by the ADO.NET and Dapper tests) and `AlarmsEf` through `Database.MigrateAsync()` (used by the EF tests). All test cases live in the abstract `AlarmRepositoryContractTests`. The per-implementation subclasses in `RepositoryTests.cs` only provide `ConnectionString` and `CreateRepository()`. Add new behavior tests to the base class so every implementation gets them. The table is cleared in `InitializeAsync`, and all classes share one collection, so they run sequentially.
- Constraint-violation tests walk the exception chain to the `SqlException` (EF wraps it in `DbUpdateException`) and assert its `Number` (2601 unique index, 547 check).
