using System.Text.Json.Serialization;

namespace ScarletPigsServices.Api.Services.Ocap;

public sealed class OcapOperation
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("filename")]
    public string Filename { get; init; } = string.Empty;

    [JsonPropertyName("world_name")]
    public string WorldName { get; init; } = string.Empty;

    [JsonPropertyName("mission_duration")]
    public double MissionDurationSeconds { get; init; }

    [JsonPropertyName("mission_name")]
    public string MissionName { get; init; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; init; } = string.Empty;

    [JsonPropertyName("tag")]
    public string Tag { get; init; } = string.Empty;

    [JsonPropertyName("storageFormat")]
    public string StorageFormat { get; init; } = string.Empty;

    [JsonPropertyName("conversionStatus")]
    public string ConversionStatus { get; init; } = string.Empty;

    [JsonPropertyName("schemaVersion")]
    public uint SchemaVersion { get; init; }

    [JsonPropertyName("chunkCount")]
    public int ChunkCount { get; init; }

    [JsonPropertyName("player_count")]
    public int PlayerCount { get; init; }

    [JsonPropertyName("kill_count")]
    public int KillCount { get; init; }

    [JsonPropertyName("side_composition")]
    public IReadOnlyDictionary<string, OcapSideCounts> SideComposition { get; init; } =
        new Dictionary<string, OcapSideCounts>();

    [JsonPropertyName("player_kill_count")]
    public int PlayerKillCount { get; init; }

    [JsonPropertyName("focusStart")]
    public long? FocusStart { get; init; }

    [JsonPropertyName("focusEnd")]
    public long? FocusEnd { get; init; }
}

public sealed record OcapSideCounts(
    [property: JsonPropertyName("players")] int Players,
    [property: JsonPropertyName("units")] int Units,
    [property: JsonPropertyName("dead")] int Dead,
    [property: JsonPropertyName("kills")] int Kills);

public sealed record OcapHealthResponse(
    [property: JsonPropertyName("status")] string Status);

public sealed record OcapVersionResponse(
    [property: JsonPropertyName("BuildVersion")] string BuildVersion,
    [property: JsonPropertyName("BuildCommit")] string BuildCommit,
    [property: JsonPropertyName("BuildDate")] string BuildDate);

public sealed record OcapWorld(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("displayName")] string DisplayName);

public sealed class OcapCustomize
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }

    [JsonPropertyName("websiteURL")]
    public string WebsiteUrl { get; init; } = string.Empty;

    [JsonPropertyName("websiteLogo")]
    public string WebsiteLogo { get; init; } = string.Empty;

    [JsonPropertyName("websiteLogoSize")]
    public string WebsiteLogoSize { get; init; } = string.Empty;

    [JsonPropertyName("disableKillCount")]
    public bool DisableKillCount { get; init; }

    [JsonPropertyName("headerTitle")]
    public string HeaderTitle { get; init; } = string.Empty;

    [JsonPropertyName("headerSubtitle")]
    public string HeaderSubtitle { get; init; } = string.Empty;

    [JsonPropertyName("cssOverrides")]
    public IReadOnlyDictionary<string, string>? CssOverrides { get; init; }
}

public sealed record OcapRecordingTimeRange(DateTimeOffset StartsAt, DateTimeOffset EndsAt);
