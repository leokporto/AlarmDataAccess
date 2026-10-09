using Alarms.Core;
using Microsoft.EntityFrameworkCore;

namespace Alarms.EfCore;

public sealed class EfCoreAlarmRepository(AlarmsDbContext db) : IAlarmRepository
{
    public async Task<int> AddAsync(Alarm alarm, CancellationToken ct = default)
    {
        db.Alarms.Add(alarm);
        await db.SaveChangesAsync(ct);
        return alarm.Id;
    }

    public Task<Alarm?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Alarms.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<Alarm>> GetByPriorityAsync(byte priority, CancellationToken ct = default) =>
        await db.Alarms.AsNoTracking().Where(a => a.Priority == priority).ToListAsync(ct);

    public async Task<bool> UpdatePriorityAsync(int id, byte priority, CancellationToken ct = default) =>
        await db.Alarms
            .Where(a => a.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Priority, priority), ct) > 0;

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default) =>
        await db.Alarms.Where(a => a.Id == id).ExecuteDeleteAsync(ct) > 0;
}
