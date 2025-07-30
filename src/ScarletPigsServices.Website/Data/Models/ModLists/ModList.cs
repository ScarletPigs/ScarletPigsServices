using ScarletPigsServices.Website.Data.Models.ModLists;

namespace ScarletPigsServices.Website.Data.Models.ModLists
{
    public partial class ModList
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public HashSet<Mod> Dlcs { get; set; }
        public HashSet<Mod> Mods { get; set; }
    }
}
