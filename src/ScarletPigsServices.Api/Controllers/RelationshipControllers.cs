using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScarletPigsServices.Data;
using ScarletPigsServices.Data.Models;

namespace ScarletPigsServices.Api.Controllers;

[ApiController]
[Route("api/modlist-mods")]
public sealed class ModListModsController(ScarletPigsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ModListMod>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await db.ModListMods.AsNoTracking().ToListAsync(cancellationToken));

    [HttpGet("{modlistId:guid}/{steamId}")]
    public async Task<ActionResult<ModListMod>> GetById(
        Guid modlistId,
        string steamId,
        CancellationToken cancellationToken)
    {
        var entity = await db.ModListMods.FindAsync([modlistId, steamId], cancellationToken);
        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public async Task<ActionResult<ModListMod>> Create(ModListMod entity, CancellationToken cancellationToken)
    {
        db.ModListMods.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { modlistId = entity.ModListId, steamId = entity.SteamId },
            entity);
    }

    [HttpPatch("{modlistId:guid}/{steamId}")]
    public async Task<ActionResult<ModListMod>> Patch(
        Guid modlistId,
        string steamId,
        ModListModPatch patch,
        CancellationToken cancellationToken)
    {
        var entity = await db.ModListMods.FindAsync([modlistId, steamId], cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        entity.Position = patch.Position;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpDelete("{modlistId:guid}/{steamId}")]
    public async Task<IActionResult> Delete(
        Guid modlistId,
        string steamId,
        CancellationToken cancellationToken)
    {
        var entity = await db.ModListMods.FindAsync([modlistId, steamId], cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        db.ModListMods.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public sealed record ModListModPatch(int Position);

[ApiController]
[Route("api/role-capabilities")]
public sealed class RoleCapabilitiesController(ScarletPigsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleCapability>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await db.RoleCapabilities.AsNoTracking().ToListAsync(cancellationToken));

    [HttpGet("{roleId}/{capabilityKey}")]
    public async Task<ActionResult<RoleCapability>> GetById(
        string roleId,
        string capabilityKey,
        CancellationToken cancellationToken)
    {
        var entity = await db.RoleCapabilities.FindAsync([roleId, capabilityKey], cancellationToken);
        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public async Task<ActionResult<RoleCapability>> Create(RoleCapability entity, CancellationToken cancellationToken)
    {
        db.RoleCapabilities.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { roleId = entity.RoleId, capabilityKey = entity.CapabilityKey },
            entity);
    }

    [HttpDelete("{roleId}/{capabilityKey}")]
    public async Task<IActionResult> Delete(
        string roleId,
        string capabilityKey,
        CancellationToken cancellationToken)
    {
        var entity = await db.RoleCapabilities.FindAsync([roleId, capabilityKey], cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        db.RoleCapabilities.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route("api/user-discord-roles")]
public sealed class UserDiscordRolesController(ScarletPigsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDiscordRole>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await db.UserDiscordRoles.AsNoTracking().ToListAsync(cancellationToken));

    [HttpGet("{userId:guid}/{roleId}")]
    public async Task<ActionResult<UserDiscordRole>> GetById(
        Guid userId,
        string roleId,
        CancellationToken cancellationToken)
    {
        var entity = await db.UserDiscordRoles.FindAsync([userId, roleId], cancellationToken);
        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public async Task<ActionResult<UserDiscordRole>> Create(UserDiscordRole entity, CancellationToken cancellationToken)
    {
        db.UserDiscordRoles.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { userId = entity.UserId, roleId = entity.RoleId },
            entity);
    }

    [HttpDelete("{userId:guid}/{roleId}")]
    public async Task<IActionResult> Delete(Guid userId, string roleId, CancellationToken cancellationToken)
    {
        var entity = await db.UserDiscordRoles.FindAsync([userId, roleId], cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        db.UserDiscordRoles.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
