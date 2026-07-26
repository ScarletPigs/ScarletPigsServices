using Microsoft.AspNetCore.Identity;

namespace ScarletPigsServices.Data.Auth;

public sealed class ApplicationUser : IdentityUser
{
    public ICollection<RefreshToken> RefreshTokens { get; } = new List<RefreshToken>();
}
