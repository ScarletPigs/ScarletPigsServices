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

    /// <summary>Checks whether the OCAP service is healthy.</summary>
    [HttpGet("api/healthcheck")]
    [Produces("application/json")]
    [ProducesResponseType<OcapHealthResponse>(StatusCodes.Status200OK)]
    public Task GetHealthcheck(CancellationToken cancellationToken)
    {
        return ProxyGet("api/healthcheck", cancellationToken);
    }

    /// <summary>Gets OCAP build version information.</summary>
    [HttpGet("api/version")]
    [Produces("application/json")]
    [ProducesResponseType<OcapVersionResponse>(StatusCodes.Status200OK)]
    public Task GetVersion(CancellationToken cancellationToken)
    {
        return ProxyGet("api/version", cancellationToken);
    }

    /// <summary>Lists OCAP recordings.</summary>
    /// <param name="name">Optional mission-name filter.</param>
    /// <param name="older">Optional upper date boundary understood by OCAP.</param>
    /// <param name="newer">Optional lower date boundary understood by OCAP.</param>
    /// <param name="tag">Optional recording-tag filter.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    [HttpGet("api/v1/operations")]
    [Produces("application/json")]
    [ProducesResponseType<IReadOnlyList<OcapOperation>>(StatusCodes.Status200OK)]
    public Task GetOperations(
        [FromQuery] string? name = null,
        [FromQuery] string? older = null,
        [FromQuery] string? newer = null,
        [FromQuery] string? tag = null,
        CancellationToken cancellationToken = default)
    {
        return ProxyGet("api/v1/operations", cancellationToken);
    }

    /// <summary>Gets one OCAP recording by numeric ID or filename.</summary>
    [HttpGet("api/v1/operations/{id}")]
    [Produces("application/json")]
    [ProducesResponseType<OcapOperation>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task GetOperation([FromRoute] string id, CancellationToken cancellationToken)
    {
        return ProxyGet(
            $"api/v1/operations/{Uri.EscapeDataString(id)}",
            cancellationToken);
    }

    /// <summary>Gets the player entity IDs hidden from a recording's marker display.</summary>
    [HttpGet("api/v1/operations/{id:long}/marker-blacklist")]
    [Produces("application/json")]
    [ProducesResponseType<IReadOnlyList<int>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task GetMarkerBlacklist([FromRoute] long id, CancellationToken cancellationToken)
    {
        return ProxyGet(
            $"api/v1/operations/{id}/marker-blacklist",
            cancellationToken);
    }

    /// <summary>Lists the Arma worlds installed in OCAP.</summary>
    [HttpGet("api/v1/worlds")]
    [Produces("application/json")]
    [ProducesResponseType<IReadOnlyList<OcapWorld>>(StatusCodes.Status200OK)]
    public Task GetWorlds(CancellationToken cancellationToken)
    {
        return ProxyGet("api/v1/worlds", cancellationToken);
    }

    /// <summary>Gets OCAP viewer customization settings.</summary>
    [HttpGet("api/v1/customize")]
    [Produces("application/json")]
    [ProducesResponseType<OcapCustomize>(StatusCodes.Status200OK)]
    public Task GetCustomize(CancellationToken cancellationToken)
    {
        return ProxyGet("api/v1/customize", cancellationToken);
    }

    /// <summary>Downloads a recording manifest, chunk, or legacy JSON recording.</summary>
    [HttpGet("data/{**path}")]
    [Produces("application/octet-stream", "application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task GetRecordingData([FromRoute] string path, CancellationToken cancellationToken)
    {
        return ProxyAsset("data", path, cancellationToken);
    }

    /// <summary>Gets a rendered OCAP marker icon.</summary>
    [HttpGet("images/markers/{name}/{color}")]
    [Produces("image/png")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task GetMarker(
        [FromRoute] string name,
        [FromRoute] string color,
        CancellationToken cancellationToken)
    {
        return ProxyGet(
            $"images/markers/{Uri.EscapeDataString(name)}/{Uri.EscapeDataString(color)}",
            cancellationToken);
    }

    /// <summary>Gets an OCAP ammunition icon.</summary>
    [HttpGet("images/markers/magicons/{name}")]
    [Produces("image/png")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task GetAmmunitionIcon(
        [FromRoute] string name,
        CancellationToken cancellationToken)
    {
        return ProxyGet(
            $"images/markers/magicons/{Uri.EscapeDataString(name)}",
            cancellationToken);
    }

    /// <summary>Gets a map font range used by the OCAP viewer.</summary>
    [HttpGet("images/maps/fonts/{fontstack}/{range}")]
    [Produces("application/x-protobuf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task GetMapFont(
        [FromRoute] string fontstack,
        [FromRoute] string range,
        CancellationToken cancellationToken)
    {
        return ProxyGet(
            $"images/maps/fonts/{Uri.EscapeDataString(fontstack)}/{Uri.EscapeDataString(range)}",
            cancellationToken);
    }

    /// <summary>Gets a map sprite image or metadata document used by the OCAP viewer.</summary>
    [HttpGet("images/maps/sprites/{name}")]
    [Produces("application/json", "image/png")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task GetMapSprite([FromRoute] string name, CancellationToken cancellationToken)
    {
        return ProxyGet(
            $"images/maps/sprites/{Uri.EscapeDataString(name)}",
            cancellationToken);
    }

    /// <summary>Gets a map tile or map metadata asset used by the OCAP viewer.</summary>
    [HttpGet("images/maps/{**path}")]
    [Produces("application/octet-stream", "application/json", "image/png", "image/webp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task GetMapAsset([FromRoute] string path, CancellationToken cancellationToken)
    {
        return ProxyAsset("images/maps", path, cancellationToken);
    }

    private async Task ProxyAsset(
        string upstreamPrefix,
        string path,
        CancellationToken cancellationToken)
    {
        if (!OcapAssetPath.IsSafe(path))
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var escapedPath = string.Join(
            '/',
            path.Split('/').Select(Uri.EscapeDataString));
        await ProxyGet($"{upstreamPrefix}/{escapedPath}", cancellationToken);
    }

    private async Task ProxyGet(string upstreamPath, CancellationToken cancellationToken)
    {
        var relativePathAndQuery = upstreamPath + Request.QueryString.Value;
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

internal static class OcapAssetPath
{
    public static bool IsSafe(string? path)
    {
        return !string.IsNullOrWhiteSpace(path)
            && !path.StartsWith("/", StringComparison.Ordinal)
            && !path.Contains('\\', StringComparison.Ordinal)
            && !path.Split('/').Any(segment => segment is "." or "..");
    }
}
