using Microsoft.AspNetCore.Components.Forms;
using ScarletPigsServices.Website.Data.Models.ModLists;
using System.Xml.Linq;

namespace ScarletPigsServices.Website.Data.Models.Helpers
{
    public static class HtmlModListParser
    {
        public static async Task<ModList> CreateModListFromFileAsync(IBrowserFile file)
        {
            using (var stream = file.OpenReadStream(10_000_000))
            using (var reader = new StreamReader(stream))
            {
                string content = await reader.ReadToEndAsync();

                // Load the XML content into XDocument
                XDocument doc = XDocument.Parse(content);
                string presetName = GetPresetName(file, doc);

                HashSet<Mod> modEntries = GetModEntriesFromDocument(doc);
                HashSet<Mod> dlcEntries = GetDlcEntriesFromDocument(doc);

                //order by cDLC first, then order by Name

                modEntries = modEntries.OrderBy(mod => mod.Name).ToHashSet();
                dlcEntries = dlcEntries.OrderBy(mod => mod.Name).ToHashSet();

                ModList modlist = new ModList()
                {
                    Name = presetName,
                    Mods = modEntries,
                    Dlcs = dlcEntries
                };

                return modlist;
            }
        }

        private static HashSet<Mod> GetDlcEntriesFromDocument(XDocument doc)
        {
            // Query for DlcContainer entries, if they exist
            var cdlcEntries = doc.Descendants("tr")
                .Where(tr => tr.Attribute("data-type")?.Value == "DlcContainer")
                .Select(tr => new Mod
                {
                    Name = tr.Elements("td")
                        .FirstOrDefault(td => td.Attribute("data-type")?.Value == "DisplayName")?.Value ?? "Unknown", // Default to "Unknown" if Name is not found
                    UID = ExtractIdFromUrl(tr.Elements("td")
                        .Skip(1) // Skip the first <td> to access the second <td>
                        .Select(td => td.Elements("a").FirstOrDefault()?.Attribute("href")?.Value) // Access the <a> tag's href attribute
                        .FirstOrDefault()) ?? string.Empty // Default to empty if no href is found
                })
                .ToHashSet();

            return cdlcEntries;
        }

        private static string GetPresetName(IBrowserFile file, XDocument doc)
        {

            // Get the value of the PresetName meta tag
            string presetName = doc.Descendants("meta")
                .FirstOrDefault(meta => meta.Attribute("name")?.Value == "arma:PresetName")?.Attribute("content")?.Value;

            // If presetName is null or empty, use the file name
            if (string.IsNullOrEmpty(presetName))
            {
                presetName = file.Name; // Use the file name
            }

            return presetName;
        }

        private static HashSet<Mod> GetModEntriesFromDocument(XDocument doc)
        {


            // Query for ModContainer entries
            var modEntries = doc.Descendants("tr")
                .Where(tr => tr.Attribute("data-type")?.Value == "ModContainer")
                .Select(tr => new Mod
                {
                    Name = tr.Elements("td").FirstOrDefault(td => td.Attribute("data-type")?.Value == "DisplayName")?.Value,
                    UID = ExtractIdFromUrl(tr.Elements("td")
                        .Skip(2) // Skip to the third <td>
                        .Select(td => td.Elements("a").FirstOrDefault()?.Attribute("href")?.Value) // Access the <a> tag's href attribute
                        .FirstOrDefault())
                })
                .ToHashSet();
            return modEntries;
        }

        private static string ExtractIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            var uri = new Uri(url);

            // For cDLC URLs, the ID is the last segment after the last '/'
            if (uri.Host.Contains("steampowered.com"))
            {
                // Extract ID from path
                var segments = uri.Segments;
                return segments.Length > 0 ? segments[^1].TrimEnd('/') : null; // Last segment
            }
            else if (uri.Query.Contains("id="))
            {
                // Extract ID from query string if it exists
                var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
                return queryParams["id"];
            }

            return null;
        }
    }
}
