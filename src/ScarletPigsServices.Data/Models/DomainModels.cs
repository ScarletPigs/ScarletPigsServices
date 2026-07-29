using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScarletPigsServices.Data.Models;

public enum ModSide
{
    Both,
    ServerOnly,
    ClientOnly
}

public enum OverrideMode
{
    Grant,
    Revoke
}

public sealed class AdminAuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Action { get; set; }
    public Guid? ActorId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public JsonDocument Detail { get; set; } = JsonDocument.Parse("{}");
    public Guid? TargetUserId { get; set; }
}

public sealed class AppSetting
{
    public required string Key { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public JsonDocument Value { get; set; } = JsonDocument.Parse("{}");
}

public sealed class BannerImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int Height { get; set; }
    public bool IsDefault { get; set; }
    public long SizeBytes { get; set; }
    public required string StoragePath { get; set; }
    public required string ThumbPath { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int Width { get; set; }
}

public sealed class Capability
{
    public required string Key { get; set; }
    public string? Description { get; set; }
    public required string Label { get; set; }
    public int SortOrder { get; set; }
}

public sealed class DiscordRole
{
    public required string Id { get; set; }
    public int Color { get; set; }
    public required string Name { get; set; }
    public int Position { get; set; }
    public DateTimeOffset SyncedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class EventType
{
    public required string Key { get; set; }
    public required string CapabilityKey { get; set; }
    public string Color { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int? FixedDurationMinutes { get; set; }
    public int? FixedStartMinutes { get; set; }
    public bool ForceUnlimitedSlots { get; set; }
    public required string Label { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? AarUrl { get; set; }
    public int? AttendanceCount { get; set; }
    public string Author { get; set; } = string.Empty;
    public string? Briefing { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public int DurationMinutes { get; set; }
    public string? ExternalId { get; set; }
    public string? Faction { get; set; }
    public JsonDocument Metadata { get; set; } = JsonDocument.Parse("{}");
    [JsonPropertyName("modlist_id")]
    public Guid? ModListId { get; set; }

    [JsonPropertyName("modlist_url")]
    public string? ModListUrl { get; set; }
    public required string Name { get; set; }
    public int? Slots { get; set; }
    public required DateTimeOffset StartsAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public required string TypeKey { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class HighlightVideo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int Position { get; set; }
    public required string Title { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public required string Url { get; set; }
    public required string VideoId { get; set; }
}

public sealed class MissionAttendance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string MissionName { get; set; }
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The Europe/Stockholm local date of the session window this attendance was recorded in.
    /// Always a Sunday; exists so repeated posts during one session collapse to a single row.
    /// </summary>
    public required DateOnly SessionDate { get; set; }
    public required long SteamId { get; set; }
}

public sealed class MissionUpload
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public required string FileName { get; set; }
    public long FileSize { get; set; }
    public string? StoragePath { get; set; }
    public string? TargetPath { get; set; }
    public string? TransferMessage { get; set; }
    public string TransferStatus { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public required Guid UploadedBy { get; set; }
    public string? ValidationMessage { get; set; }
    public string ValidationStatus { get; set; } = string.Empty;
}

public sealed class ModListMod
{
    [JsonPropertyName("modlist_id")]
    public required Guid ModListId { get; set; }
    public int Position { get; set; }
    public required string SteamId { get; set; }
}

public sealed class ModList
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CommandLine { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public string[] DlcAppIds { get; set; } = [];
    public bool IsPublic { get; set; }
    public required string Name { get; set; }
    public int Position { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Mod
{
    public required string SteamId { get; set; }
    public string CommandLineName { get; set; } = string.Empty;
    public bool CommandLineNameLocked { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string DisplayName { get; set; } = string.Empty;
    public DateTimeOffset? LastSyncedAt { get; set; }
    public ModSide Side { get; set; } = ModSide.Both;
    public long SizeBytes { get; set; }
    public DateTimeOffset? TimeUpdated { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Profile
{
    public required Guid Id { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public required string DiscordId { get; set; }
    public required string DiscordUsername { get; set; }
    public string? DisplayName { get; set; }
    public DateTimeOffset? GuildJoinedAt { get; set; }
    public bool IsBanned { get; set; }
    public bool IsGuildMember { get; set; }
    public DateTimeOffset? LastRoleSyncAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ProfileNameHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string ProfileName { get; set; }
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
    public required long SteamId { get; set; }
}

public sealed class RoleCapability
{
    public required string CapabilityKey { get; set; }
    public required string RoleId { get; set; }
}

public sealed class RoleOverride
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public required OverrideMode Mode { get; set; }
    public string? Note { get; set; }
    public required string RoleId { get; set; }
    public required Guid UserId { get; set; }
}

public sealed class SteamDlcOwnership
{
    public required long SteamId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public required long DlcId { get; set; }
}

public sealed class UserCapabilityOverride
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string CapabilityKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public required OverrideMode Mode { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public required Guid UserId { get; set; }
}

public sealed class UserDiscordRole
{
    public required string RoleId { get; set; }
    public required Guid UserId { get; set; }
}
