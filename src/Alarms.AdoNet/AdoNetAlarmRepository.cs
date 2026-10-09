using System.Data;
using Alarms.Core;
using Microsoft.Data.SqlClient;

namespace Alarms.AdoNet;

public sealed class AdoNetAlarmRepository(string connectionString) : IAlarmRepository
{
    // Column order matters: ReadAlarm reads by ordinal.
    private const string SelectColumns =
        "SELECT Id, Name, State, Condition, Limit, Priority, Timestamp_UTCTicks, Message FROM dbo.Alarms";

    public async Task<int> AddAsync(Alarm alarm, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.Alarms (Name, State, Condition, Limit, Priority, Timestamp_UTCTicks, Message)
            OUTPUT INSERTED.Id
            VALUES (@Name, @State, @Condition, @Limit, @Priority, @Timestamp, @Message)
            """;

        await using var connection = await OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = alarm.Name;
        command.Parameters.Add("@State", SqlDbType.TinyInt).Value = (byte)alarm.State;
        command.Parameters.Add("@Condition", SqlDbType.TinyInt).Value = (byte)alarm.Condition;
        command.Parameters.Add("@Limit", SqlDbType.Float).Value = alarm.Limit;
        command.Parameters.Add("@Priority", SqlDbType.TinyInt).Value = alarm.Priority;
        command.Parameters.Add("@Timestamp", SqlDbType.BigInt).Value = alarm.TimestampUtcTicks;
        command.Parameters.Add("@Message", SqlDbType.NVarChar, 500).Value = alarm.Message;

        return (int)(await command.ExecuteScalarAsync(ct))!;
    }

    public async Task<Alarm?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(ct);
        await using var command = new SqlCommand($"{SelectColumns} WHERE Id = @Id", connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, ct);
        return await reader.ReadAsync(ct) ? ReadAlarm(reader) : null;
    }

    public async Task<IReadOnlyList<Alarm>> GetByPriorityAsync(byte priority, CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(ct);
        await using var command = new SqlCommand($"{SelectColumns} WHERE Priority = @Priority", connection);
        command.Parameters.Add("@Priority", SqlDbType.TinyInt).Value = priority;

        var alarms = new List<Alarm>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            alarms.Add(ReadAlarm(reader));
        }
        return alarms;
    }

    public async Task<bool> UpdatePriorityAsync(int id, byte priority, CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(ct);
        await using var command = new SqlCommand("UPDATE dbo.Alarms SET Priority = @Priority WHERE Id = @Id", connection);
        command.Parameters.Add("@Priority", SqlDbType.TinyInt).Value = priority;
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(ct);
        await using var command = new SqlCommand("DELETE FROM dbo.Alarms WHERE Id = @Id", connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }

    private static Alarm ReadAlarm(SqlDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        Name = reader.GetString(1),
        State = (eAlarmState)reader.GetByte(2),
        Condition = (AlarmCondition)reader.GetByte(3),
        Limit = reader.GetDouble(4),
        Priority = reader.GetByte(5),
        TimestampUtcTicks = reader.GetInt64(6),
        Message = reader.GetString(7),
    };
}
