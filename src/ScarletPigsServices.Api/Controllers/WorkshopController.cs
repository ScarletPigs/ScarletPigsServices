using Microsoft.AspNetCore.Mvc;
using ScarletPigsServices.Api.Services.Workshop;

namespace ScarletPigsServices.Api.Controllers;

[ApiController]
[Route("workshop")]
public sealed class WorkshopController : ControllerBase
{
    private readonly ISteamWorkshopService _steamWorkshopService;

    public WorkshopController(ISteamWorkshopService steamWorkshopService)
    {
        _steamWorkshopService = steamWorkshopService;
    }

    [HttpPost("mods")]
    [ProducesResponseType<WorkshopLookupResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorkshopLookupResponse>> LookupMods([FromBody] WorkshopLookupRequest? request, CancellationToken cancellationToken)
    {
        var ids = request?.Ids
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (ids is null || ids.Length == 0)
        {
            return BadRequest("At least one workshop id is required.");
        }

        var mods = await _steamWorkshopService.GetWorkshopModsAsync(ids, cancellationToken);
        return Ok(new WorkshopLookupResponse(mods));
    }
}

public sealed record WorkshopLookupRequest(IReadOnlyList<string> Ids);

public sealed record WorkshopLookupResponse(IReadOnlyList<WorkshopModSummary> Mods);