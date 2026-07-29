using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ScarletPigsServices.Api.Json;
using ScarletPigsServices.Api.Services.Addon;
using ScarletPigsServices.Data;
using ScarletPigsServices.Data.Models;

namespace ScarletPigsServices.Api.Controllers;

[ApiController]
[Route("addon")]
public sealed class AddonController(ScarletPigsDbContext db) : ControllerBase
{
    private const string UniqueViolation = "23505";

    /// <summary>
    /// Reports a player from the in-game addon. Always reconciles their DLC ownership, and during
    /// the Sunday operation window also records their current profile name and mission attendance.
    /// </summary>
    [HttpPost("user-info")]
    [ProducesResponseType<UserInfoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserInfoResponse>> PostUserInfo(
        [FromBody] UserInfoRequest? request,
        CancellationToken cancellationToken)
    {
        var receivedAt = DateTimeOffset.UtcNow;

        if (request is null)
        {
            return BadRequest("A request body is required.");
        }

        if (request.SteamId <= 0)
        {
            return BadRequest("steam_id must be a positive number.");
        }

        var steamId = request.SteamId;
        var ownedDlc = request.OwnedDlc
            .Where(static dlcId => dlcId > 0)
            .Distinct()
            .ToHashSet();

        var profileName = request.ProfileName?.Trim();
        var missionName = request.MissionName?.Trim();

        await ReplaceDlcOwnershipAsync(steamId, ownedDlc, receivedAt, cancellationToken);

        var withinSessionWindow = SessionWindow.TryGetSessionDate(receivedAt, out var sessionDate);
        var profileNameRecorded = false;
        var attendanceRecorded = false;

        if (withinSessionWindow)
        {
            profileNameRecorded = await TryAddProfileNameAsync(steamId, profileName, receivedAt, cancellationToken);
            attendanceRecorded = await TryAddAttendanceAsync(
                steamId,
                missionName,
                sessionDate,
                receivedAt,
                cancellationToken);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            // A concurrent post inserted one or more rows first. Retry once by reloading/recomputing
            // the intended state so we don't drop unrelated changes (e.g., DLC reconciliation).
            db.ChangeTracker.Clear();

            await ReplaceDlcOwnershipAsync(steamId, ownedDlc, receivedAt, cancellationToken);

            profileNameRecorded = false;
            attendanceRecorded = false;
            if (withinSessionWindow)
            {
                profileNameRecorded = await TryAddProfileNameAsync(steamId, profileName, receivedAt, cancellationToken);
                attendanceRecorded = await TryAddAttendanceAsync(steamId, missionName, sessionDate, receivedAt, cancellationToken);
            }

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException retryException) when (IsUniqueViolation(retryException))
            {
                // If we still raced on attendance, drop only the conflicting insert and persist the rest.
                foreach (var entry in retryException.Entries)
                {
                    if (entry.State == EntityState.Added && entry.Entity is MissionAttendance)
                    {
                        attendanceRecorded = false;
                        entry.State = EntityState.Detached;
                    }
                }

                await db.SaveChangesAsync(cancellationToken);
            }
        }

        return Ok(new UserInfoResponse(
            steamId,
            receivedAt,
            ownedDlc.Count,
            withinSessionWindow,
            withinSessionWindow ? sessionDate : null,
            profileNameRecorded,
            attendanceRecorded));
    }

    /// <summary>
    /// Makes the posted list the exact truth for this player: adds what is missing and removes
    /// what is no longer owned.
    /// </summary>
    private async Task ReplaceDlcOwnershipAsync(
        long steamId,
        HashSet<long> ownedDlc,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        var existing = await db.SteamDlcOwnership
            .Where(item => item.SteamId == steamId)
            .ToListAsync(cancellationToken);

        foreach (var item in existing.Where(item => !ownedDlc.Contains(item.DlcId)))
        {
            db.SteamDlcOwnership.Remove(item);
        }

        var existingDlcIds = existing.Select(item => item.DlcId).ToHashSet();
        foreach (var dlcId in ownedDlc.Where(dlcId => !existingDlcIds.Contains(dlcId)))
        {
            db.SteamDlcOwnership.Add(new SteamDlcOwnership
            {
                SteamId = steamId,
                DlcId = dlcId,
                CreatedAt = receivedAt
            });
        }
    }

    /// <summary>Appends a history row only when the name differs from the player's latest one.</summary>
    private async Task<bool> TryAddProfileNameAsync(
        long steamId,
        string? profileName,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            return false;
        }

        var latest = await db.ProfileNameHistory
            .Where(item => item.SteamId == steamId)
            .OrderByDescending(item => item.RecordedAt)
            .Select(item => item.ProfileName)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.Equals(latest, profileName, StringComparison.Ordinal))
        {
            return false;
        }

        db.ProfileNameHistory.Add(new ProfileNameHistory
        {
            SteamId = steamId,
            ProfileName = profileName,
            RecordedAt = receivedAt
        });

        return true;
    }

    /// <summary>Records one attendance row per player, mission and session date.</summary>
    private async Task<bool> TryAddAttendanceAsync(
        long steamId,
        string? missionName,
        DateOnly sessionDate,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(missionName))
        {
            return false;
        }

        var exists = await db.MissionAttendance.AnyAsync(
            item => item.SteamId == steamId
                && item.MissionName == missionName
                && item.SessionDate == sessionDate,
            cancellationToken);

        if (exists)
        {
            return false;
        }

        db.MissionAttendance.Add(new MissionAttendance
        {
            SteamId = steamId,
            MissionName = missionName,
            SessionDate = sessionDate,
            RecordedAt = receivedAt
        });

        return true;
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: UniqueViolation };
}

public sealed record UserInfoRequest
{
    [JsonConverter(typeof(FlexibleInt64JsonConverter))]
    public long SteamId { get; init; }

    public string? ProfileName { get; init; }

    public string? MissionName { get; init; }

    [JsonConverter(typeof(FlexibleInt64ListJsonConverter))]
    public IReadOnlyList<long> OwnedDlc { get; init; } = [];
}

public sealed record UserInfoResponse(
    long SteamId,
    DateTimeOffset ReceivedAt,
    int OwnedDlcCount,
    bool WithinSessionWindow,
    DateOnly? SessionDate,
    bool ProfileNameRecorded,
    bool AttendanceRecorded);
