using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ScarletPigsServices.Data;
using ScarletPigsServices.Data.Models;

namespace ScarletPigsServices.Api.Controllers;

[Route("api/admin-audit")]
public sealed class AdminAuditController(
    ScarletPigsDbContext db,
    IOptions<JsonOptions> jsonOptions)
    : GuidDataController<AdminAuditEntry>(db, jsonOptions, static item => item.Id);

[Route("api/app-settings")]
public sealed class AppSettingsController(
    ScarletPigsDbContext db,
    IOptions<JsonOptions> jsonOptions)
    : StringDataController<AppSetting>(db, jsonOptions, static item => item.Key, "key");

[Route("api/banner-images")]
public sealed class BannerImagesController(
    ScarletPigsDbContext db,
    IOptions<JsonOptions> jsonOptions)
    : GuidDataController<BannerImage>(db, jsonOptions, static item => item.Id);

[Route("api/capabilities")]
public sealed class CapabilitiesController(
    ScarletPigsDbContext db,
    IOptions<JsonOptions> jsonOptions)
    : StringDataController<Capability>(db, jsonOptions, static item => item.Key, "key");

[Route("api/discord-roles")]
public sealed class DiscordRolesController(
    ScarletPigsDbContext db,
    IOptions<JsonOptions> jsonOptions)
    : StringDataController<DiscordRole>(db, jsonOptions, static item => item.Id, "id");

[Route("api/event-types")]
public sealed class EventTypesController(
    ScarletPigsDbContext db,
    IOptions<JsonOptions> jsonOptions)
    : StringDataController<EventType>(db, jsonOptions, static item => item.Key, "key");

[Route("api/events")]
public sealed class EventsController(
    ScarletPigsDbContext db,
    IOptions<JsonOptions> jsonOptions)
    : GuidDataController<Event>(db, jsonOptions, static item => item.Id);

[Route("api/highlight-videos")]
public sealed class HighlightVideosController(
    ScarletPigsDbContext db,
    IOptions<JsonOptions> jsonOptions)
    : GuidDataController<HighlightVideo>(db, jsonOptions, static item => item.Id);

[Route("api/mission-uploads")]
public sealed class MissionUploadsController(
    ScarletPigsDbContext db,
    IOptions<JsonOptions> jsonOptions)
    : GuidDataController<MissionUpload>(db, jsonOptions, static item => item.Id);

[Route("api/modlists")]
public sealed class ModListsController(
    ScarletPigsDbContext db,
    IOptions<JsonOptions> jsonOptions)
    : GuidDataController<ModList>(db, jsonOptions, static item => item.Id);

[Route("api/mods")]
public sealed class ModsController(
    ScarletPigsDbContext db,
    IOptions<JsonOptions> jsonOptions)
    : StringDataController<Mod>(db, jsonOptions, static item => item.SteamId, "steam_id");

[Route("api/profiles")]
public sealed class ProfilesController(
    ScarletPigsDbContext db,
    IOptions<JsonOptions> jsonOptions)
    : GuidDataController<Profile>(db, jsonOptions, static item => item.Id);

[Route("api/role-overrides")]
public sealed class RoleOverridesController(
    ScarletPigsDbContext db,
    IOptions<JsonOptions> jsonOptions)
    : GuidDataController<RoleOverride>(db, jsonOptions, static item => item.Id);

[Route("api/user-capability-overrides")]
public sealed class UserCapabilityOverridesController(
    ScarletPigsDbContext db,
    IOptions<JsonOptions> jsonOptions)
    : GuidDataController<UserCapabilityOverride>(db, jsonOptions, static item => item.Id);
