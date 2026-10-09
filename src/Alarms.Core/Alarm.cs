namespace Alarms.Core;

public sealed class Alarm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public eAlarmState State { get; set; }
    public AlarmCondition Condition { get; set; }
    public double Limit { get; set; }

    /// <summary>Numeric priority from 1 to 10, enforced by a CHECK constraint.</summary>
    public byte Priority { get; set; }

    /// <summary>UTC timestamp stored as <see cref="DateTime.Ticks"/> (bigint).</summary>
    public long TimestampUtcTicks { get; set; }

    public string Message { get; set; } = "";
}
