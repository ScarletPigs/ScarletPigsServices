using ScarletPigsServices.Data.Events;

namespace ScarletPigsServices.Website.Data.Services.HTTP
{
    public interface IScarletPigsApi
    {
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
