namespace ScarletPigsServices.Website.Data.Models.Steamworks
{
    public class Response
    {
        public int Result { get; set; }
        public int ResultCount { get; set; }
        public List<PublishedFileDetails> PublishedFileDetails { get; set; }
    }
}
