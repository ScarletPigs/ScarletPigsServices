using System.Security.Claims;
using ScarletPigsServices.Data.Auth;

namespace ScarletPigsServices.Api.Extensions
{
    internal static class ClaimsPrincipalExtensions
    {
        public static CurrentUserResponse ToCurrentUserResponse(this ClaimsPrincipal user)
        {
            var id = user.FindFirstValue("discordid") ?? string.Empty;
            var avatarHash = user.FindFirstValue("useravatar") ?? string.Empty;
            var roles = user.GetRoles();

            return new CurrentUserResponse
            {
                Id = id,
                Email = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                GlobalName = user.FindFirstValue("global_name") ?? string.Empty,
                UserName = user.FindFirstValue("username") ?? string.Empty,
                AvatarHash = avatarHash,
                AvatarUrl = string.IsNullOrWhiteSpace(avatarHash) || string.IsNullOrWhiteSpace(id)
                    ? "https://placehold.co/50"
                    : $"https://cdn.discordapp.com/avatars/{id}/{avatarHash}.png",
                Roles = roles,
                IsAdmin = roles.Contains("UnitOrganizer", StringComparer.OrdinalIgnoreCase),
                IsAllowedMissionUpload = roles.Contains("UnitOrganizer", StringComparer.OrdinalIgnoreCase)
                    || roles.Contains("MissionMaker", StringComparer.OrdinalIgnoreCase)
            };
        }

        private static IReadOnlyList<string> GetRoles(this ClaimsPrincipal user)
        {
            var roles = user.Claims
                .Where(claim => claim.Type == ClaimTypes.Role || claim.Type == "roles")
                .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return roles;
        }
    }
}