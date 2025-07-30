using ScarletPigsServices.Website.Data.Models.Steamworks;
using System.Text.Json;

namespace ScarletPigsServices.Website.Data.Services.HTTP
{
    public interface ISteamworksApi
    {
        public Task<PublishedFileDetailsResponse> GetWorkshopModDetailsAsync(List<string> modIds);
    }

    public class SteamworksApi : ISteamworksApi
    {
        private readonly HttpClient _httpClient;

        public SteamworksApi(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PublishedFileDetailsResponse> GetWorkshopModDetailsAsync(List<string> modIds)
        {
            // Create the list of KeyValuePair with "itemcount" and mod ids dynamically
            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("itemcount", modIds.Count.ToString())
            };

            // Add each mod id to the parameters
            for (int i = 0; i < modIds.Count; i++)
            {
                parameters.Add(new KeyValuePair<string, string>($"publishedfileids[{i}]", modIds[i]));
            }

            // Create the request content from the parameters
            var requestContent = new FormUrlEncodedContent(parameters);

            // Send the POST request
            HttpResponseMessage response = await _httpClient.PostAsync("ISteamRemoteStorage/GetPublishedFileDetails/v1/", requestContent);
            response.EnsureSuccessStatusCode();

            // Deserialize the response using System.Text.Json
            string responseBody = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // Makes property mapping case-insensitive
            };

            return JsonSerializer.Deserialize<PublishedFileDetailsResponse>(responseBody, options);
        }

    }
}
