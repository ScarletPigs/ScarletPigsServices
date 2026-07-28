using System.Text.Json.Serialization;

namespace ScarletPigsServices.Api.Services.Ocap;

public sealed class OcapOperation
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("filename")]
    public string Filename { get; init; } = string.Empty;

    [JsonPropertyName("mission_duration")]
    public double MissionDurationSeconds { get; init; }

    [JsonPropertyName("mission_name")]
    public string MissionName { get; init; } = string.Empty;

    [JsonPropertyName("storageFormat")]
    public string StorageFormat { get; init; } = string.Empty;

    [JsonPropertyName("conversionStatus")]
    public string ConversionStatus { get; init; } = string.Empty;
}

public sealed record OcapRecordingTimeRange(DateTimeOffset StartsAt, DateTimeOffset EndsAt);
