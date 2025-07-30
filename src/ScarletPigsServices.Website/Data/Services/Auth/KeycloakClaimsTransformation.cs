using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace ScarletPigsServices.Website.Data.Services.Auth
{
    public class KeycloakClaimsTransformation : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal.HasClaim(c => c.Type == ClaimTypes.Role))
            {
                var identity = principal.Identity as ClaimsIdentity;

                // Copy roles to a new list to avoid modifying the collection during enumeration
                var roles = principal.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

                foreach (var role in roles)
                {
                    // Add the role claim to the identity without modifying the original collection during iteration
                    identity?.AddClaim(new Claim(identity?.RoleClaimType ?? ClaimTypes.Role, role));
                }
            }
            return Task.FromResult(principal);
        }
    }
}
