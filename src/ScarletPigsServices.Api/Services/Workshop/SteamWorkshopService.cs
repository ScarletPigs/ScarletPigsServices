using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScarletPigsServices.Api.Services.Workshop;

public interface ISteamWorkshopService
{
    Task<IReadOnlyList<WorkshopModSummary>> GetWorkshopModsAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken);
}

public sealed class SteamWorkshopService : ISteamWorkshopService
{
    private const string Arma3AppId = "107410";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;

    public SteamWorkshopService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<WorkshopModSummary>> GetWorkshopModsAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<WorkshopModSummary>();
        }

        var parameters = new List<KeyValuePair<string, string>>
        {
            new("itemcount", ids.Count.ToString()),
        };

        for (var index = 0; index < ids.Count; index++)
        {
            parameters.Add(new KeyValuePair<string, string>($"publishedfileids[{index}]", ids[index]));
        }

        using var requestContent = new FormUrlEncodedContent(parameters);
        using var response = await _httpClient.PostAsync("ISteamRemoteStorage/GetPublishedFileDetails/v1/", requestContent, cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<SteamPublishedFileDetailsEnvelope>(stream, JsonOptions, cancellationToken);

        return payload?.Response?.PublishedFileDetails?
            .Where(item => item.Result == 1 && ReadJsonValue(item.CreatorAppId) == Arma3AppId)
            .Select(item => new WorkshopModSummary(
                ReadJsonValue(item.PublishedFileId),
                item.Title ?? string.Empty,
                long.TryParse(ReadJsonValue(item.FileSize), out var sizeInBytes) ? sizeInBytes : 0))
            .Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.Title))
            .ToArray()
            ?? Array.Empty<WorkshopModSummary>();
    }

    private sealed class SteamPublishedFileDetailsEnvelope
    {
        [JsonPropertyName("response")]
        public SteamPublishedFileDetailsResponse? Response { get; set; }
    }

    private sealed class SteamPublishedFileDetailsResponse
    {
        [JsonPropertyName("publishedfiledetails")]
        public List<SteamPublishedFileDetail>? PublishedFileDetails { get; set; }
    }

    private sealed class SteamPublishedFileDetail
    {
        [JsonPropertyName("result")]
        public int Result { get; set; }

        [JsonPropertyName("creator_app_id")]
        public JsonElement CreatorAppId { get; set; }

        [JsonPropertyName("publishedfileid")]
        public JsonElement PublishedFileId { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("file_size")]
        public JsonElement FileSize { get; set; }
    }

    private static string ReadJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            _ => string.Empty,
        };
    }
}

public sealed record WorkshopModSummary(string Id, string Title, long SizeInBytes);