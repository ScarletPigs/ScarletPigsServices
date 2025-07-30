using System.Security.Claims;

namespace ScarletPigsServices.Website.Data.Models.Auth
{
    public class KeycloakUser : ClaimsPrincipal
    {
        // User Attributes
        public string Id => this.FindFirstValue("discordid") ?? string.Empty;
        public string Email => this.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        public string GlobalName => this.FindFirstValue("global_name") ?? string.Empty;
        public string UserName => this.FindFirstValue("username") ?? string.Empty;
        public string AvatarHash => this.FindFirstValue("useravatar") ?? string.Empty;
        public string AvatarUrl { get { return string.IsNullOrWhiteSpace(AvatarHash) || string.IsNullOrWhiteSpace(Id) ? "https://placehold.co/50" : $"https://cdn.discordapp.com/avatars/{Id}/{AvatarHash}.png"; } }

        // Permissions, roles, and such
        public List<string> Roles => FindAll(ClaimTypes.Role).Select(x => x.Value).ToList();
        public bool IsAdmin => IsInRole("UnitOrganizer");




        public KeycloakUser(ClaimsPrincipal principal) : base(principal) 
        {

        }
    }
}
