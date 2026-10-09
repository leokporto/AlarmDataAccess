using Alarms.Core;
using Microsoft.EntityFrameworkCore;

namespace Alarms.EfCore;

public sealed class AlarmsDbContext(DbContextOptions<AlarmsDbContext> options) : DbContext(options)
{
    public DbSet<Alarm> Alarms => Set<Alarm>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Alarm>(alarm =>
        {
            alarm.ToTable("Alarms", "dbo", t =>
                t.HasCheckConstraint("CK_Alarms_Priority", "[Priority] BETWEEN 1 AND 10"));

            alarm.HasKey(a => a.Id).HasName("PK_Alarms");
            alarm.HasIndex(a => a.Name).IsUnique().HasDatabaseName("UX_Alarms_Name");

            alarm.Property(a => a.Name).HasMaxLength(100);
            alarm.Property(a => a.State).HasConversion<byte>();
            alarm.Property(a => a.Condition).HasConversion<byte>();
            alarm.Property(a => a.TimestampUtcTicks).HasColumnName("Timestamp_UTCTicks");
            alarm.Property(a => a.Message).HasMaxLength(500);
        });
    }
}
