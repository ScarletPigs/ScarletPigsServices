using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using ScarletPigsServices.Api.Services.Ocap;

namespace ScarletPigsServices.Api.Controllers;

[ApiController]
[Route("api/ocap")]
public sealed class OcapProxyController(IOcapClient ocapClient) : ControllerBase
{
    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        HeaderNames.Connection,
        HeaderNames.KeepAlive,
        HeaderNames.ProxyAuthenticate,
        HeaderNames.ProxyAuthorization,
        HeaderNames.TE,
        HeaderNames.Trailer,
        HeaderNames.TransferEncoding,
        HeaderNames.Upgrade
    };

    [HttpGet("{**path}")]
    public async Task ProxyGet(string path, CancellationToken cancellationToken)
    {
        if (!OcapReadRoutePolicy.IsAllowed(path))
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var relativePathAndQuery = path + Request.QueryString.Value;
        using var upstream = await ocapClient.GetProxyResponseAsync(
            relativePathAndQuery,
            cancellationToken);

        Response.StatusCode = (int)upstream.StatusCode;
        CopyHeaders(upstream.Headers);
        CopyHeaders(upstream.Content.Headers);
        await upstream.Content.CopyToAsync(Response.Body, cancellationToken);
    }

    private void CopyHeaders(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
    {
        foreach (var header in headers)
        {
            if (!HopByHopHeaders.Contains(header.Key))
            {
                Response.Headers[header.Key] = header.Value.ToArray();
            }
        }
    }
}

internal static class OcapReadRoutePolicy
{
    private static readonly HashSet<string> ExactPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "api/healthcheck",
        "api/version",
        "api/v1/customize",
        "api/v1/operations",
        "api/v1/worlds"
    };

    public static bool IsAllowed(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)
            || path.StartsWith("/", StringComparison.Ordinal)
            || path.Contains('\\', StringComparison.Ordinal)
            || path.Split('/').Any(segment => segment is "." or ".."))
        {
            return false;
        }

        var normalized = path.TrimStart('/');
        return ExactPaths.Contains(normalized)
            || normalized.StartsWith("api/v1/operations/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("data/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("images/", StringComparison.OrdinalIgnoreCase);
    }
}
