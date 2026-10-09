using Alarms.Core;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Alarms.Dapper;

public sealed class DapperAlarmRepository(string connectionString) : IAlarmRepository
{
    // The alias maps the Timestamp_UTCTicks column onto Alarm.TimestampUtcTicks.
    private const string SelectColumns = """
        SELECT Id, Name, State, Condition, Limit, Priority, Timestamp_UTCTicks AS TimestampUtcTicks, Message
        FROM dbo.Alarms
        """;

    public async Task<int> AddAsync(Alarm alarm, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.Alarms (Name, State, Condition, Limit, Priority, Timestamp_UTCTicks, Message)
            OUTPUT INSERTED.Id
            VALUES (@Name, @State, @Condition, @Limit, @Priority, @TimestampUtcTicks, @Message)
            """;

        await using var connection = new SqlConnection(connectionString);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, alarm, cancellationToken: ct));
    }

    public async Task<Alarm?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(connectionString);
        return await connection.QuerySingleOrDefaultAsync<Alarm>(
            new CommandDefinition($"{SelectColumns} WHERE Id = @id", new { id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Alarm>> GetByPriorityAsync(byte priority, CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var alarms = await connection.QueryAsync<Alarm>(
            new CommandDefinition($"{SelectColumns} WHERE Priority = @priority", new { priority }, cancellationToken: ct));
        return alarms.AsList();
    }

    public async Task<bool> UpdatePriorityAsync(int id, byte priority, CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var rows = await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE dbo.Alarms SET Priority = @priority WHERE Id = @id", new { id, priority }, cancellationToken: ct));
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var rows = await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM dbo.Alarms WHERE Id = @id", new { id }, cancellationToken: ct));
        return rows > 0;
    }
}
