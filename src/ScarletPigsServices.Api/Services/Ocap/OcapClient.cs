using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace ScarletPigsServices.Api.Services.Ocap;

public sealed class OcapClient(
    HttpClient httpClient,
    IOptions<OcapOptions> options) : IOcapClient
{
    private readonly Uri _publicBaseUri = new(options.Value.PublicBaseUrl, UriKind.Absolute);

    public async Task<IReadOnlyList<OcapOperation>> GetOperationsAsync(
        CancellationToken cancellationToken)
    {
        return await httpClient.GetFromJsonAsync<List<OcapOperation>>(
                "api/v1/operations",
                cancellationToken)
            ?? [];
    }

    public async Task<OcapRecordingTimeRange?> GetRecordingTimeRangeAsync(
        OcapOperation operation,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(operation.Filename)
            || !operation.StorageFormat.Equals("protobuf", StringComparison.OrdinalIgnoreCase)
            || !operation.ConversionStatus.Equals("completed", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var filename = Uri.EscapeDataString(operation.Filename);
        using var response = await httpClient.GetAsync(
            $"data/{filename}/manifest.pb",
            HttpCompletionOption.ResponseContentRead,
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var manifest = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return OcapManifestReader.Read(manifest, operation.MissionDurationSeconds);
    }

    public Task<HttpResponseMessage> GetProxyResponseAsync(
        string relativePathAndQuery,
        CancellationToken cancellationToken)
    {
        return httpClient.GetAsync(
            new Uri(relativePathAndQuery, UriKind.Relative),
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
    }

    public Uri BuildRecordingUri(OcapOperation operation)
    {
        var basePath = _publicBaseUri.AbsolutePath.TrimEnd('/');
        var builder = new UriBuilder(_publicBaseUri)
        {
            Path = $"{basePath}/recording/{operation.Id}/{Uri.EscapeDataString(operation.Filename)}",
            Query = string.Empty,
            Fragment = string.Empty
        };

        return builder.Uri;
    }
}
