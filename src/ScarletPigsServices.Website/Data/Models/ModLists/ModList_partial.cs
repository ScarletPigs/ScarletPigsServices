using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging.Abstractions;
using ScarletPigsServices.Website.Data.Enums;
using ScarletPigsServices.Website.Data.Models.Helpers;
using ScarletPigsServices.Website.Data.Models.Steamworks;
using ScarletPigsServices.Website.Data.Util;
using System.Text;
using System.Xml.Linq;

namespace ScarletPigsServices.Website.Data.Models.ModLists
{
    public partial class ModList
    {
        public ModList()
        {
            Name = "";
            Description = "";
            Author = "";
            CreatedAt = DateTime.Now;
            LastUpdatedAt = DateTime.Now;
            Mods = new HashSet<Mod>();
            Dlcs = new HashSet<Mod>();
        }

        public static async Task<ModList> CreateModListFromFileAsync(IBrowserFile file)
        {
            return await HtmlModListParser.CreateModListFromFileAsync(file);
        }

        public static ModList CreateModListFromSteamIds(PublishedFileDetailsResponse publishedFileDetailsResponse)
        {
            if (publishedFileDetailsResponse?.Response?.Result == 1) // OK
            {
                // Handle successful response
                ModList modList = new ModList();

                var fileDetails = publishedFileDetailsResponse.Response.PublishedFileDetails;
                // Process fileDetails as needed
                foreach (var file in fileDetails)
                {
                    // Only add mods that were successfully retrieved
                    if (file.Result != 1)
                        continue;

                    // Only add mods that are for Arma 3
                    if (file.CreatorAppId != Constants.Arma3AppId)
                        continue;

                    //get the filesize in kb
                    long fileSize = 0;
                    long.TryParse(file.FileSize, out fileSize);

                    Mod mod = new Mod()
                    {
                        Name = file.Title,
                        UID = file.PublishedFileId.ToString(),
                        SizeInBytes = fileSize
                    };
                    modList.AddMod(mod);
                }

                return modList;
            }
            else
            {
                return null;
            }
        }

        //loop through all the mods and create the command line
        public string GetCommandLine()
        {
            return string.Join(";", Dlcs
                .Concat(Mods)
                .Select(mod => mod.GetCommandLineName())
                .Where(commandLineName => !string.IsNullOrEmpty(commandLineName))); // Filter out null or empty entries
        }


        //TODO: Move this to a database table on the server and clean up the enum stuff.
        public static HashSet<Mod> GetAvailableDlcs()
        {
            HashSet<Mod> allDlcMods = new HashSet<Mod>();
            foreach (DlcEnum dlc in Enum.GetValues(typeof(DlcEnum)))
            {
                Mod mod = new Mod()
                {
                    UID = ((int)dlc).ToString(),
                    Name = EnumUtil.GetDisplayName(dlc)
                };
                allDlcMods.Add(mod);
            }

            return allDlcMods;
        }

        //see if any of the mods in the list are dlcs and return those
        public static HashSet<Mod> FindDlcFromIds(List<string> Uids)
        {
            HashSet<Mod> dlcs = new HashSet<Mod>();
            foreach (string uid in Uids)
            {
                if (int.TryParse(uid, out int uidValue))
                {
                    if (Enum.IsDefined(typeof(DlcEnum), uidValue))
                    {
                        Mod mod = new Mod()
                        {
                            UID = uid,
                            Name = EnumUtil.GetDisplayName((DlcEnum)uidValue)
                        };
                        dlcs.Add(mod);
                    }
                }
            }

            return dlcs;
        }

        public long GetTotalSizeInBytes()
        {
            return Mods.Sum(mod => mod.SizeInBytes) + Dlcs.Sum(mod => mod.SizeInBytes);
        }

        public bool UpdateDlcs(HashSet<Mod> dlcs)
        {
            if (dlcs.Count == Dlcs.Count && dlcs.All(Dlcs.Contains))
                return false;

            Dlcs = dlcs;
            return true;
        }

        public bool HasMods()
        {
            return Mods.Count + Dlcs.Count > 0;
        }

        public void AddMod(Mod mod)
        {
            if (mod.IsDlc())
                Dlcs.Add(mod);
            else
                Mods.Add(mod);
        }
        
        public void RemoveMod(Mod mod)
        {
            if (mod.IsDlc())
                Dlcs.Remove(mod);
            else
                Mods.Remove(mod);
        }

    }
}
