using ScarletPigsServices.Data.Auth;
using ScarletPigsServices.Data.Events;
using ScarletPigsServices.Data.Files;

namespace ScarletPigsServices.Website.Data.Services.HTTP
{
    public interface IScarletPigsApi
    {
        public Task<CurrentUserResponse?> GetCurrentUserAsync();
        public Task<HavocFoldersResponse?> GetHavocFoldersAsync(string target = "server", CancellationToken cancellationToken = default);
        public Task<MissionUploadResponse?> UploadMissionAsync(string fileName, Stream fileContent, string folder = "/", string target = "server", CancellationToken cancellationToken = default);
        public Task<Event?> GetEventAsync(string id);
        public Task<List<Event>> GetEventsAsync();
        public Task<Event?> CreateEventAsync(CreateEventDTO newEvent);
        public Task<bool> UpdateEventAsync(string id, EditEventDTO updatedEvent);
        public Task DeleteEventAsync(string id);
    }

    public class ScarletPigsApi : IScarletPigsApi
    {
        private readonly HttpClient _httpClient;

        public ScarletPigsApi(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<CurrentUserResponse?> GetCurrentUserAsync()
        {
            return await _httpClient.GetFromJsonAsync<CurrentUserResponse>("users/me");
        }

        public async Task<HavocFoldersResponse?> GetHavocFoldersAsync(string target = "server", CancellationToken cancellationToken = default)
        {
            return await _httpClient.GetFromJsonAsync<HavocFoldersResponse>($"files/folders?target={Uri.EscapeDataString(target)}", cancellationToken);
        }

        public async Task<MissionUploadResponse?> UploadMissionAsync(string fileName, Stream fileContent, string folder = "/", string target = "server", CancellationToken cancellationToken = default)
        {
            using var form = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileContent);
            form.Add(streamContent, "file", fileName);
            form.Add(new StringContent(folder), "folder");
            form.Add(new StringContent(target), "target");

            var response = await _httpClient.PostAsync("files/missions", form, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MissionUploadResponse>(cancellationToken);
        }

        public async Task<Event?> GetEventAsync(string id)
        {
            return await _httpClient.GetFromJsonAsync<Event>($"events/{id}");
        }

        public async Task<List<Event>> GetEventsAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<Event>>("events/") ?? new List<Event>();
        }


        public async Task<Event?> CreateEventAsync(CreateEventDTO newEvent)
        {
            return await (await _httpClient.PostAsJsonAsync("events/", newEvent)).Content.ReadFromJsonAsync<Event>();
        }

        public async Task<bool> UpdateEventAsync(string id, EditEventDTO updatedEvent)
        {
            return (await _httpClient.PutAsJsonAsync($"events/{id}", updatedEvent)).IsSuccessStatusCode;
        }

        public async Task DeleteEventAsync(string id)
        {
            await _httpClient.DeleteAsync($"events/{id}");
        }
    }
}
