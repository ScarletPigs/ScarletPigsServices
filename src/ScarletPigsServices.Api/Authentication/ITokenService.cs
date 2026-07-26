using ScarletPigsServices.Data.Auth;

namespace ScarletPigsServices.Api.Authentication;

public interface ITokenService
{
    Task<TokenResponse> IssueAsync(ApplicationUser user, CancellationToken cancellationToken);
    Task<TokenResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task RevokeAsync(string userId, string refreshToken, CancellationToken cancellationToken);
}
