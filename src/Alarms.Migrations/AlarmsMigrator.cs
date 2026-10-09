using DbUp;
using DbUp.Engine;

namespace Alarms.Migrations;

public static class AlarmsMigrator
{
    /// <summary>
    /// Creates the database if needed and applies any embedded script not yet recorded
    /// in DbUp's journal table (dbo.SchemaVersions). Safe to run repeatedly.
    /// </summary>
    public static DatabaseUpgradeResult Run(string connectionString)
    {
        EnsureDatabase.For.SqlDatabase(connectionString);

        var upgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(AlarmsMigrator).Assembly)
            .LogToConsole()
            .Build();

        return upgrader.PerformUpgrade();
    }
}
