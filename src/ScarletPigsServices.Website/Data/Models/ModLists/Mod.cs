
using ScarletPigsServices.Website.Data.Enums;
using ScarletPigsServices.Website.Data.Models.Helpers;
using System.Text.RegularExpressions;

namespace ScarletPigsServices.Website.Data.Models.ModLists
{
    public class Mod
    {

        public string UID { get; set; }

        public long SizeInBytes { get; set; }

        private string _name;
        public string Name
        {
            //get our hardcoded name if it's a dlc, otherwise get the normal mod name
            get => IsDlc() && Enum.TryParse(typeof(DlcEnum), UID, out var dlcEnum)
                        ? EnumUtil.GetDisplayName((DlcEnum)dlcEnum)
                        : _name;
            set => _name = value;
        }

        public bool IsDlc()
        {
            if (string.IsNullOrEmpty(UID))
                return false; // UID is null or empty, not a valid ID

            if (int.TryParse(UID, out int uidValue))
            {
                return Enum.IsDefined(typeof(DlcEnum), uidValue);
            }

            return false; // UID is not a valid integer
        }

        internal string GetCommandLineName()
        {
            // If it's a CDLC, we can just return the command line name
            if (IsDlc())
                return EnumUtil.GetCommandLineName((DlcEnum)int.Parse(UID));

            // else it's a normal mod and we need to clean the name
            // Regular expression pattern for invalid characters - no longer in use
            //string pattern = @"[.()!:/]+";

            //Check for allowed characters
            string pattern = @"[^a-zA-Z0-9' +\-@_()\[\]]+";


            // Replace invalid characters with an empty string
            return $"@{Regex.Replace(Name, pattern, string.Empty)}";
        }

        public override bool Equals(object o)
        {
            var other = o as Mod;
            return other?.UID == UID;
        }

        // Note: this is important so the select can compare pizzas
        public override int GetHashCode() => UID.GetHashCode();
    }
}