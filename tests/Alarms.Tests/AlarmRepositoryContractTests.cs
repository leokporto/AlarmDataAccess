using Alarms.Core;
using Microsoft.Data.SqlClient;

namespace Alarms.Tests;

/// <summary>
/// The behavior every <see cref="IAlarmRepository"/> must have. Each implementation gets a
/// derived class, so all three run exactly the same tests against a real SQL Server.
/// </summary>
public abstract class AlarmRepositoryContractTests : IAsyncLifetime
{
    private const int UniqueIndexViolation = 2601;
    private const int CheckConstraintViolation = 547;

    private IAlarmRepository _repository = null!;

    protected abstract string ConnectionString { get; }

    protected abstract IAlarmRepository CreateRepository();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public virtual async ValueTask InitializeAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(Ct);
        await using var command = new SqlCommand("DELETE FROM dbo.Alarms", connection);
        await command.ExecuteNonQueryAsync(Ct);

        _repository = CreateRepository();
    }

    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task AddAsync_returns_generated_id()
    {
        var id = await _repository.AddAsync(NewAlarm("TT-101.HI"), Ct);

        Assert.True(id > 0);
    }

    [Fact]
    public async Task GetByIdAsync_round_trips_all_fields()
    {
        var alarm = new Alarm
        {
            Name = "PT-204.LO",
            State = eAlarmState.Acknowledge,
            Condition = AlarmCondition.LessThan,
            Limit = 12.75,
            Priority = 9,
            // Sub-millisecond ticks prove the timestamp is stored without loss.
            TimestampUtcTicks = new DateTime(2026, 10, 9, 14, 30, 0, DateTimeKind.Utc).Ticks + 1_234_567,
            Message = "Pressure below limit",
        };

        var id = await _repository.AddAsync(alarm, Ct);
        var loaded = await _repository.GetByIdAsync(id, Ct);

        Assert.NotNull(loaded);
        Assert.Equal(id, loaded.Id);
        Assert.Equal(alarm.Name, loaded.Name);
        Assert.Equal(alarm.State, loaded.State);
        Assert.Equal(alarm.Condition, loaded.Condition);
        Assert.Equal(alarm.Limit, loaded.Limit);
        Assert.Equal(alarm.Priority, loaded.Priority);
        Assert.Equal(alarm.TimestampUtcTicks, loaded.TimestampUtcTicks);
        Assert.Equal(alarm.Message, loaded.Message);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_missing_id()
    {
        Assert.Null(await _repository.GetByIdAsync(-1, Ct));
    }

    [Fact]
    public async Task GetByPriorityAsync_returns_only_matching_alarms()
    {
        await _repository.AddAsync(NewAlarm("A", priority: 2), Ct);
        await _repository.AddAsync(NewAlarm("B", priority: 7), Ct);
        await _repository.AddAsync(NewAlarm("C", priority: 7), Ct);

        var alarms = await _repository.GetByPriorityAsync(7, Ct);

        Assert.Equal(["B", "C"], alarms.Select(a => a.Name).Order());
        Assert.All(alarms, a => Assert.Equal(7, a.Priority));
    }

    [Fact]
    public async Task UpdatePriorityAsync_updates_existing_alarm()
    {
        var id = await _repository.AddAsync(NewAlarm("LT-300.HH", priority: 3), Ct);

        Assert.True(await _repository.UpdatePriorityAsync(id, 10, Ct));
        Assert.Equal(10, (await _repository.GetByIdAsync(id, Ct))!.Priority);
    }

    [Fact]
    public async Task UpdatePriorityAsync_returns_false_for_missing_id()
    {
        Assert.False(await _repository.UpdatePriorityAsync(-1, 5, Ct));
    }

    [Fact]
    public async Task DeleteAsync_removes_alarm()
    {
        var id = await _repository.AddAsync(NewAlarm("FT-400.LO"), Ct);

        Assert.True(await _repository.DeleteAsync(id, Ct));
        Assert.Null(await _repository.GetByIdAsync(id, Ct));
    }

    [Fact]
    public async Task DeleteAsync_returns_false_for_missing_id()
    {
        Assert.False(await _repository.DeleteAsync(-1, Ct));
    }

    [Fact]
    public async Task AddAsync_rejects_duplicate_name()
    {
        await _repository.AddAsync(NewAlarm("TT-500.HI"), Ct);

        var ex = await Assert.ThrowsAnyAsync<Exception>(() => _repository.AddAsync(NewAlarm("TT-500.HI"), Ct));

        Assert.Equal(UniqueIndexViolation, FindSqlException(ex).Number);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public async Task AddAsync_rejects_priority_outside_1_to_10(byte priority)
    {
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => _repository.AddAsync(NewAlarm("TT-600.HI", priority), Ct));

        Assert.Equal(CheckConstraintViolation, FindSqlException(ex).Number);
    }

    private static Alarm NewAlarm(string name, byte priority = 5) => new()
    {
        Name = name,
        State = eAlarmState.Active,
        Condition = AlarmCondition.GreaterThan,
        Limit = 80.0,
        Priority = priority,
        TimestampUtcTicks = DateTime.UtcNow.Ticks,
        Message = $"{name} triggered",
    };

    // ADO.NET and Dapper surface SqlException directly; EF Core wraps it in DbUpdateException.
    private static SqlException FindSqlException(Exception ex)
    {
        for (Exception? current = ex; current is not null; current = current.InnerException)
        {
            if (current is SqlException sqlException)
            {
                return sqlException;
            }
        }

        Assert.Fail($"Expected a SqlException, got {ex.GetType().Name}: {ex.Message}");
        return null!;
    }
}
