using ScarletPigsServices.Website.Data.Enums;

namespace ScarletPigsServices.Website.Data.Models.Helpers
{
    public static class EnumUtil
    {
        public static string GetCommandLineName(Enum enumEntry)
        {
            var fieldInfo = enumEntry.GetType().GetField(enumEntry.ToString());
            var attributes = (CommandLineNameAttribute[])fieldInfo.GetCustomAttributes(typeof(CommandLineNameAttribute), false);
            return attributes.Length > 0 ? attributes[0].Name : enumEntry.ToString();
        }

        public static string GetDisplayName(Enum enumEntry)
        {
            var fieldInfo = enumEntry.GetType().GetField(enumEntry.ToString());
            var attributes = (DisplayNameAttribute[])fieldInfo.GetCustomAttributes(typeof(DisplayNameAttribute), false);
            return attributes.Length > 0 ? attributes[0].DisplayName : enumEntry.ToString();
        }
    }
}
