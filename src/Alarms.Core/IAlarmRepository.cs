namespace Alarms.Core;

public interface IAlarmRepository
{
    Task<int> AddAsync(Alarm alarm, CancellationToken ct = default);
    Task<Alarm?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Alarm>> GetByPriorityAsync(byte priority, CancellationToken ct = default);
    Task<bool> UpdatePriorityAsync(int id, byte priority, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
