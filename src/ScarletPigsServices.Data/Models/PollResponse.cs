namespace Piglet_Domain_Models.Models
{
    public class PollResponse
    {
        public int Id { get; set; }
        public string CreatorDiscordUsername { get; set; }
        public string Response { get; set; }

        public Poll Poll { get; set; }
    }
}
