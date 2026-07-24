using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using ScarletPigsServices.Data.Auth;

namespace ScarletPigsServices.Api.Extensions
{
    internal static class ClaimsPrincipalExtensions
    {
        public static CurrentUserResponse ToCurrentUserResponse(this ClaimsPrincipal user)
        {
            var id = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? string.Empty;
            var userName = user.FindFirstValue(ClaimTypes.Name)
                ?? user.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
                ?? string.Empty;
            var roles = user.GetRoles();

            return new CurrentUserResponse
            {
                Id = id,
                Email = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                GlobalName = userName,
                UserName = userName,
                AvatarHash = string.Empty,
                AvatarUrl = "https://placehold.co/50",
                Roles = roles,
                IsAdmin = roles.Contains(AuthRoles.UnitOrganizer, StringComparer.OrdinalIgnoreCase),
                IsAllowedMissionUpload = roles.Contains(AuthRoles.UnitOrganizer, StringComparer.OrdinalIgnoreCase)
                    || roles.Contains(AuthRoles.MissionMaker, StringComparer.OrdinalIgnoreCase)
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
