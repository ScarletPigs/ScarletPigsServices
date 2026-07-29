using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScarletPigsServices.Data;
using ScarletPigsServices.Data.Models;

namespace ScarletPigsServices.Api.Controllers;

/// <summary>Read access to the attendance recorded by <c>POST addon/user-info</c>.</summary>
[ApiController]
[Route("api/attendance")]
public sealed class AttendanceController(ScarletPigsDbContext db) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<MissionAttendance>>> GetAll(
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (!PagingRange.IsValid(offset, limit))
        {
            return BadRequest(PagingRange.ErrorMessage);
        }

        return Ok(await Ordered().Skip(offset).Take(limit).ToListAsync(cancellationToken));
    }

    /// <summary>Attendance for one mission. The name is a query parameter because mission names contain spaces and punctuation.</summary>
    [HttpGet("mission")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<MissionAttendance>>> GetByMission(
        [FromQuery] string? name,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("name is required.");
        }

        if (!PagingRange.IsValid(offset, limit))
        {
            return BadRequest(PagingRange.ErrorMessage);
        }

        var missionName = name.Trim();
        return Ok(await Ordered()
            .Where(item => item.MissionName == missionName)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken));
    }

    /// <summary>Attendance recorded between <paramref name="start"/> (inclusive) and <paramref name="end"/> (exclusive).</summary>
    [HttpGet("range")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<MissionAttendance>>> GetByRange(
        [FromQuery] DateTimeOffset start,
        [FromQuery] DateTimeOffset end,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (end <= start)
        {
            return BadRequest("end must be later than start.");
        }

        if (!PagingRange.IsValid(offset, limit))
        {
            return BadRequest(PagingRange.ErrorMessage);
        }

        return Ok(await Ordered()
            .Where(item => item.RecordedAt >= start && item.RecordedAt < end)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken));
    }

    [HttpGet("{steamId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<MissionAttendance>>> GetBySteamId(
        long steamId,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (!PagingRange.IsValid(offset, limit))
        {
            return BadRequest(PagingRange.ErrorMessage);
        }

        return Ok(await Ordered()
            .Where(item => item.SteamId == steamId)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken));
    }

    private IQueryable<MissionAttendance> Ordered() =>
        db.MissionAttendance.AsNoTracking().OrderByDescending(item => item.RecordedAt);
}

/// <summary>Read access to the Steam DLC each player owns.</summary>
[ApiController]
[Route("api/dlc-ownership")]
public sealed class DlcOwnershipController(ScarletPigsDbContext db) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<DlcOwnershipResponse>>> GetAll(
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (!PagingRange.IsValid(offset, limit))
        {
            return BadRequest(PagingRange.ErrorMessage);
        }

        // Page over players rather than rows, so nobody's DLC list is split across pages.
        var steamIds = await db.SteamDlcOwnership
            .AsNoTracking()
            .Select(item => item.SteamId)
            .Distinct()
            .OrderBy(steamId => steamId)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (steamIds.Count == 0)
        {
            return Ok(Array.Empty<DlcOwnershipResponse>());
        }

        var rows = await db.SteamDlcOwnership
            .AsNoTracking()
            .Where(item => steamIds.Contains(item.SteamId))
            .OrderBy(item => item.SteamId)
            .ThenBy(item => item.DlcId)
            .ToListAsync(cancellationToken);

        return Ok(rows
            .GroupBy(item => item.SteamId)
            .Select(group => new DlcOwnershipResponse(group.Key, group.Select(item => item.DlcId).ToArray()))
            .ToList());
    }

    [HttpGet("{steamId:long}")]
    [ProducesResponseType<DlcOwnershipResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DlcOwnershipResponse>> GetBySteamId(
        long steamId,
        CancellationToken cancellationToken)
    {
        var dlcIds = await db.SteamDlcOwnership
            .AsNoTracking()
            .Where(item => item.SteamId == steamId)
            .OrderBy(item => item.DlcId)
            .Select(item => item.DlcId)
            .ToListAsync(cancellationToken);

        return dlcIds.Count == 0 ? NotFound() : Ok(new DlcOwnershipResponse(steamId, dlcIds));
    }
}

public sealed record DlcOwnershipResponse(long SteamId, IReadOnlyList<long> OwnedDlc);

/// <summary>Read access to the profile names a player has used over time.</summary>
[ApiController]
[Route("api/profile-names")]
public sealed class ProfileNamesController(ScarletPigsDbContext db) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ProfileNameHistory>>> GetAll(
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (!PagingRange.IsValid(offset, limit))
        {
            return BadRequest(PagingRange.ErrorMessage);
        }

        return Ok(await Ordered().Skip(offset).Take(limit).ToListAsync(cancellationToken));
    }

    [HttpGet("{steamId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ProfileNameHistory>>> GetBySteamId(
        long steamId,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (!PagingRange.IsValid(offset, limit))
        {
            return BadRequest(PagingRange.ErrorMessage);
        }

        return Ok(await Ordered()
            .Where(item => item.SteamId == steamId)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken));
    }

    private IQueryable<ProfileNameHistory> Ordered() =>
        db.ProfileNameHistory.AsNoTracking().OrderByDescending(item => item.RecordedAt);
}

/// <summary>Matches the paging contract of <see cref="DataControllerBase{TEntity, TKey}"/>.</summary>
internal static class PagingRange
{
    internal const string ErrorMessage = "offset must be non-negative and limit must be between 1 and 1000.";

    internal static bool IsValid(int offset, int limit) => offset >= 0 && limit is >= 1 and <= 1000;
}
