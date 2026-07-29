using Microsoft.Extensions.Options;
using ScarletPigsServices.Data.Models;

namespace ScarletPigsServices.Api.Services.Ocap;

public sealed class OcapEventLinker(
    IOcapEventStore eventStore,
    IOcapClient ocapClient,
    IOptions<OcapEventLinkingOptions> options,
    ILogger<OcapEventLinker> logger)
{
    public const string OperationEventType = "operation";

    private readonly OcapEventLinkingOptions _options = options.Value;

    public async Task<int> LinkDueEventsAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var earliestCandidateStart = now - _options.LookupWindow - _options.SweepInterval;
        var candidates = await eventStore.GetUnlinkedOperationEventsAsync(
            earliestCandidateStart,
            now,
            cancellationToken);

        var dueEvents = candidates
            .Where(item => IsDue(item, now))
            .ToList();

        if (dueEvents.Count == 0)
        {
            return 0;
        }

        // Persist the attempt before calling OCAP so a temporary OCAP failure still
        // observes the hourly retry interval and API restarts do not duplicate work.
        foreach (var item in dueEvents)
        {
            item.AarLookupLastAttemptAt = now;
            item.UpdatedAt = now;
        }

        await eventStore.SaveChangesAsync(cancellationToken);

        var operations = await ocapClient.GetOperationsAsync(cancellationToken);
        var recordings = new List<(OcapOperation Operation, OcapRecordingTimeRange Range)>();

        foreach (var operation in operations)
        {
            try
            {
                var range = await ocapClient.GetRecordingTimeRangeAsync(operation, cancellationToken);
                if (range is not null)
                {
                    recordings.Add((operation, range));
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(
                    exception,
                    "Could not inspect OCAP recording {RecordingId} ({Filename}).",
                    operation.Id,
                    operation.Filename);
            }
        }

        var linked = 0;

        foreach (var item in dueEvents)
        {
            var eventEnd = item.StartsAt.AddMinutes(item.DurationMinutes);
            var bestMatch = recordings
                .Select(recording => new
                {
                    Recording = recording,
                    Overlap = GetOverlap(
                        item.StartsAt,
                        eventEnd,
                        recording.Range.StartsAt,
                        recording.Range.EndsAt)
                })
                .Where(candidate => candidate.Overlap > TimeSpan.Zero)
                .OrderByDescending(candidate => candidate.Overlap)
                .ThenBy(candidate => Math.Abs(
                    (candidate.Recording.Range.StartsAt - item.StartsAt).Ticks))
                .ThenBy(candidate => candidate.Recording.Operation.Id)
                .FirstOrDefault();

            if (bestMatch is null)
            {
                continue;
            }

            item.AarUrl = ocapClient.BuildRecordingUri(bestMatch.Recording.Operation).ToString();
            item.UpdatedAt = now;
            linked++;

            logger.LogInformation(
                "Linked event {EventId} to OCAP recording {RecordingId} with {Overlap} overlap.",
                item.Id,
                bestMatch.Recording.Operation.Id,
                bestMatch.Overlap);
        }

        if (linked > 0)
        {
            await eventStore.SaveChangesAsync(cancellationToken);
        }

        return linked;
    }

    internal bool IsDue(Event item, DateTimeOffset now)
    {
        var elapsed = now - item.StartsAt;
        if (elapsed < TimeSpan.Zero || elapsed > _options.LookupWindow + _options.SweepInterval)
        {
            return false;
        }

        var maximumSlot = _options.LookupWindow.Ticks / _options.RetryInterval.Ticks;
        var currentSlot = Math.Min(
            elapsed.Ticks / _options.RetryInterval.Ticks,
            maximumSlot);
        var scheduledAttempt = item.StartsAt + currentSlot * _options.RetryInterval;

        return item.AarLookupLastAttemptAt is null
            || item.AarLookupLastAttemptAt < scheduledAttempt;
    }

    private static TimeSpan GetOverlap(
        DateTimeOffset firstStart,
        DateTimeOffset firstEnd,
        DateTimeOffset secondStart,
        DateTimeOffset secondEnd)
    {
        var start = firstStart > secondStart ? firstStart : secondStart;
        var end = firstEnd < secondEnd ? firstEnd : secondEnd;
        return end > start ? end - start : TimeSpan.Zero;
    }
}
