namespace ScarletPigsServices.Api.Services.Ocap;

public interface IOcapClient
{
    Task<IReadOnlyList<OcapOperation>> GetOperationsAsync(CancellationToken cancellationToken);

    Task<OcapRecordingTimeRange?> GetRecordingTimeRangeAsync(
        OcapOperation operation,
        CancellationToken cancellationToken);

    Task<HttpResponseMessage> GetProxyResponseAsync(
        string relativePathAndQuery,
        CancellationToken cancellationToken);

    Uri BuildRecordingUri(OcapOperation operation);
}
