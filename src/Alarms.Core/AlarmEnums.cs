namespace Alarms.Core;

// Both enums are backed by byte so they map 1:1 to tinyint columns.

public enum eAlarmState : byte
{
    Undefined = 0,
    Active = 1,
    Normalized = 2,
    Acknowledge = 3,
}

public enum AlarmCondition : byte
{
    GreaterThan = 0,
    LessThan = 1,
    Equal = 2,
    NotEqual = 3,
}
