using Microsoft.Extensions.Options;

namespace ScarletPigsServices.Api.Services.Ocap;

public sealed class OcapEventLinkingWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<OcapEventLinkingOptions> options,
    ILogger<OcapEventLinkingWorker> logger) : BackgroundService
{
    private readonly OcapEventLinkingOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunSweepAsync(stoppingToken);

        using var timer = new PeriodicTimer(_options.SweepInterval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunSweepAsync(stoppingToken);
        }
    }

    private async Task RunSweepAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var linker = scope.ServiceProvider.GetRequiredService<OcapEventLinker>();
            await linker.LinkDueEventsAsync(timeProvider.GetUtcNow(), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "The OCAP event-linking sweep failed.");
        }
    }
}
