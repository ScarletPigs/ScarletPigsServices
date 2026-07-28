using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ScarletPigsServices.Api.Services.Ocap;
using ScarletPigsServices.Data.Models;

namespace ScarletPigsServices.Api.Tests;

public sealed class OcapEventLinkerTests
{
    private static readonly DateTimeOffset EventStart =
        new(2026, 7, 28, 19, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task LinkDueEventsAsync_LinksTheRecordingWithTheMostOverlap()
    {
        var item = CreateEvent();
        var store = new FakeOcapEventStore([item]);

        var shortRecording = CreateOperation(1, "short");
        var bestRecording = CreateOperation(2, "best");
        var client = new FakeOcapClient(
            [shortRecording, bestRecording],
            new Dictionary<long, OcapRecordingTimeRange>
            {
                [shortRecording.Id] = new(EventStart.AddHours(-1), EventStart.AddMinutes(30)),
                [bestRecording.Id] = new(EventStart.AddMinutes(15), EventStart.AddHours(2))
            });

        var linker = CreateLinker(store, client);
        var linked = await linker.LinkDueEventsAsync(EventStart);

        Assert.Equal(1, linked);
        Assert.Equal("https://aar.example/recording/2/best", item.AarUrl);
        Assert.Equal(EventStart, item.AarLookupLastAttemptAt);
    }

    [Fact]
    public async Task LinkDueEventsAsync_RetriesHourlyAndStopsAfterFindingAMatch()
    {
        var item = CreateEvent();
        var store = new FakeOcapEventStore([item]);

        var operation = CreateOperation(7, "event-recording");
        var client = new FakeOcapClient(
            [operation],
            new Dictionary<long, OcapRecordingTimeRange>());
        var linker = CreateLinker(store, client);

        Assert.Equal(0, await linker.LinkDueEventsAsync(EventStart));
        Assert.Equal(1, client.GetOperationsCallCount);

        Assert.Equal(0, await linker.LinkDueEventsAsync(EventStart.AddMinutes(59)));
        Assert.Equal(1, client.GetOperationsCallCount);

        client.Ranges[operation.Id] = new(
            EventStart.AddMinutes(10),
            EventStart.AddHours(2));

        Assert.Equal(1, await linker.LinkDueEventsAsync(EventStart.AddHours(1)));
        Assert.Equal(2, client.GetOperationsCallCount);
        Assert.NotNull(item.AarUrl);

        Assert.Equal(0, await linker.LinkDueEventsAsync(EventStart.AddHours(2)));
        Assert.Equal(2, client.GetOperationsCallCount);
    }

    [Fact]
    public async Task LinkDueEventsAsync_StopsAfterTheFiveHourWindow()
    {
        var item = CreateEvent();
        var store = new FakeOcapEventStore([item]);

        var client = new FakeOcapClient(
            [],
            new Dictionary<long, OcapRecordingTimeRange>());
        var linker = CreateLinker(store, client);

        var linked = await linker.LinkDueEventsAsync(
            EventStart.AddHours(5).AddMinutes(2));

        Assert.Equal(0, linked);
        Assert.Equal(0, client.GetOperationsCallCount);
        Assert.Null(item.AarLookupLastAttemptAt);
    }

    [Fact]
    public async Task LinkDueEventsAsync_AllowsTheFinalAttemptAtFiveHours()
    {
        var item = CreateEvent();
        var store = new FakeOcapEventStore([item]);

        var client = new FakeOcapClient(
            [],
            new Dictionary<long, OcapRecordingTimeRange>());
        var linker = CreateLinker(store, client);

        Assert.Equal(0, await linker.LinkDueEventsAsync(EventStart.AddHours(5)));
        Assert.Equal(1, client.GetOperationsCallCount);
        Assert.Equal(EventStart.AddHours(5), item.AarLookupLastAttemptAt);
    }

    [Fact]
    public async Task LinkDueEventsAsync_IgnoresNonOperationEvents()
    {
        var store = new FakeOcapEventStore([CreateEvent(typeKey: "training")]);

        var client = new FakeOcapClient(
            [],
            new Dictionary<long, OcapRecordingTimeRange>());
        var linker = CreateLinker(store, client);

        Assert.Equal(0, await linker.LinkDueEventsAsync(EventStart));
        Assert.Equal(0, client.GetOperationsCallCount);
    }

    private static OcapEventLinker CreateLinker(
        IOcapEventStore store,
        IOcapClient client)
    {
        return new OcapEventLinker(
            store,
            client,
            Options.Create(new OcapEventLinkingOptions()),
            NullLogger<OcapEventLinker>.Instance);
    }

    private static Event CreateEvent(string typeKey = OcapEventLinker.OperationEventType)
    {
        return new Event
        {
            DurationMinutes = 180,
            Name = "Operation",
            StartsAt = EventStart,
            TypeKey = typeKey
        };
    }

    private static OcapOperation CreateOperation(long id, string filename)
    {
        return new OcapOperation
        {
            Id = id,
            Filename = filename,
            MissionDurationSeconds = 3600,
            StorageFormat = "protobuf",
            ConversionStatus = "completed"
        };
    }

    private sealed class FakeOcapClient(
        IReadOnlyList<OcapOperation> operations,
        IReadOnlyDictionary<long, OcapRecordingTimeRange> ranges) : IOcapClient
    {
        public Dictionary<long, OcapRecordingTimeRange> Ranges { get; } = new(ranges);
        public int GetOperationsCallCount { get; private set; }

        public Uri BuildRecordingUri(OcapOperation operation)
        {
            return new Uri($"https://aar.example/recording/{operation.Id}/{operation.Filename}");
        }

        public Task<IReadOnlyList<OcapOperation>> GetOperationsAsync(
            CancellationToken cancellationToken)
        {
            GetOperationsCallCount++;
            return Task.FromResult(operations);
        }

        public Task<OcapRecordingTimeRange?> GetRecordingTimeRangeAsync(
            OcapOperation operation,
            CancellationToken cancellationToken)
        {
            Ranges.TryGetValue(operation.Id, out var range);
            return Task.FromResult(range);
        }

        public Task<HttpResponseMessage> GetProxyResponseAsync(
            string relativePathAndQuery,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeOcapEventStore(IReadOnlyList<Event> events) : IOcapEventStore
    {
        public Task<IReadOnlyList<Event>> GetUnlinkedOperationEventsAsync(
            DateTimeOffset earliestStart,
            DateTimeOffset latestStart,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<Event> result = events
                .Where(item =>
                    item.TypeKey == OcapEventLinker.OperationEventType
                    && item.AarUrl is null
                    && item.StartsAt >= earliestStart
                    && item.StartsAt <= latestStart)
                .ToList();
            return Task.FromResult(result);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
