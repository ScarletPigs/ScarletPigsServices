namespace Piglet_Domain_Models.Models
{
    public class Statistic
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Value { get; set; }
        public string CreatorDiscordUsername { get; set; }
        public DateTime LastModified { get; set; }
    }
}
