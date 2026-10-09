using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Alarms.EfCore;

/// <summary>
/// Used only by the <c>dotnet ef</c> tooling at design time. Generating migrations
/// doesn't connect to the database, so the connection string is a placeholder.
/// </summary>
internal sealed class AlarmsDbContextFactory : IDesignTimeDbContextFactory<AlarmsDbContext>
{
    public AlarmsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AlarmsDbContext>()
            .UseSqlServer("Server=localhost;Database=AlarmsEf;Integrated Security=true;TrustServerCertificate=true")
            .Options);
}
