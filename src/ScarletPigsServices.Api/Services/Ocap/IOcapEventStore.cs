using Microsoft.EntityFrameworkCore;
using ScarletPigsServices.Data;
using ScarletPigsServices.Data.Models;

namespace ScarletPigsServices.Api.Services.Ocap;

public interface IOcapEventStore
{
    Task<IReadOnlyList<Event>> GetUnlinkedOperationEventsAsync(
        DateTimeOffset earliestStart,
        DateTimeOffset latestStart,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class OcapEventStore(ScarletPigsDbContext db) : IOcapEventStore
{
    public async Task<IReadOnlyList<Event>> GetUnlinkedOperationEventsAsync(
        DateTimeOffset earliestStart,
        DateTimeOffset latestStart,
        CancellationToken cancellationToken)
    {
        return await db.Events
            .Where(item =>
                item.TypeKey == OcapEventLinker.OperationEventType
                && item.AarUrl == null
                && item.StartsAt <= latestStart
                && item.StartsAt >= earliestStart)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return db.SaveChangesAsync(cancellationToken);
    }
}
