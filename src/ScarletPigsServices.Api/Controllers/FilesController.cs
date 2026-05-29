using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScarletPigsServices.Api.Services.Files;
using ScarletPigsServices.Data.Files;

namespace ScarletPigsServices.Api.Controllers;

[ApiController]
[Route("files")]
public sealed class FilesController : ControllerBase
{
    private const long MaxMissionUploadBytes = 1024L * 1024L * 1024L;
    private readonly IHavocFileService _havocFileService;

    public FilesController(IHavocFileService havocFileService)
    {
        _havocFileService = havocFileService;
    }

    [HttpGet("folders")]
    [ProducesResponseType<HavocFoldersResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<HavocFoldersResponse>> GetFolders([FromQuery] string? target, CancellationToken cancellationToken)
    {
        return Ok(await _havocFileService.GetFoldersAsync(target ?? "server", cancellationToken));
    }

    [Authorize(Policy = "CanUploadMissions")]
    [RequestSizeLimit(MaxMissionUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxMissionUploadBytes)]
    [HttpPost("missions")]
    [ProducesResponseType<MissionUploadResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MissionUploadResponse>> UploadMission(
        IFormFile? file,
        [FromForm] string? folder,
        [FromForm] string? target,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("A mission file is required.");
        }

        if (file.Length > MaxMissionUploadBytes)
        {
            return BadRequest($"Mission files must be {MaxMissionUploadBytes} bytes or smaller.");
        }

        await using var stream = file.OpenReadStream();
        var result = await _havocFileService.UploadMissionAsync(target ?? "server", folder ?? "/", file.FileName, stream, cancellationToken);
        return Ok(result);
    }
}