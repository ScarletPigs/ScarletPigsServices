using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ScarletPigsServices.Data;
using ScarletPigsServices.Data.Auth;

namespace ScarletPigsServices.Api.Authentication;

internal sealed class JwtTokenService(
    ScarletPigsDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> options,
    TimeProvider timeProvider) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public async Task<TokenResponse> IssueAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var accessToken = await CreateAccessTokenAsync(user, now);
        var refreshToken = await CreateRefreshTokenAsync(user, now);

        dbContext.RefreshTokens.Add(refreshToken.Entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreateResponse(accessToken, refreshToken);
    }

    public async Task<TokenResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var tokenHash = HashToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();

        if (storedToken.RevokedAtUtc is not null)
        {
            await RevokeAllActiveTokensAsync(storedToken.UserId, now.UtcDateTime, cancellationToken);
            return null;
        }

        if (storedToken.ExpiresAtUtc <= now.UtcDateTime)
        {
            storedToken.RevokedAtUtc = now.UtcDateTime;
            storedToken.ConcurrencyStamp = Guid.NewGuid();
            await TrySaveChangesAsync(cancellationToken);
            return null;
        }

        var currentSecurityStamp = await userManager.GetSecurityStampAsync(storedToken.User);
        if (!string.Equals(storedToken.SecurityStamp, currentSecurityStamp, StringComparison.Ordinal)
            || await userManager.IsLockedOutAsync(storedToken.User))
        {
            await RevokeAllActiveTokensAsync(storedToken.UserId, now.UtcDateTime, cancellationToken);
            return null;
        }

        var accessToken = await CreateAccessTokenAsync(storedToken.User, now);
        var nextRefreshToken = await CreateRefreshTokenAsync(storedToken.User, now);

        storedToken.RevokedAtUtc = now.UtcDateTime;
        storedToken.ReplacedByTokenId = nextRefreshToken.Entity.Id;
        storedToken.ConcurrencyStamp = Guid.NewGuid();
        dbContext.RefreshTokens.Add(nextRefreshToken.Entity);

        if (!await TrySaveChangesAsync(cancellationToken))
        {
            return null;
        }

        return CreateResponse(accessToken, nextRefreshToken);
    }

    public async Task RevokeAsync(string userId, string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var tokenHash = HashToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens.SingleOrDefaultAsync(
            token => token.UserId == userId && token.TokenHash == tokenHash,
            cancellationToken);

        if (storedToken is null || storedToken.RevokedAtUtc is not null)
        {
            return;
        }

        storedToken.RevokedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        storedToken.ConcurrencyStamp = Guid.NewGuid();
        await TrySaveChangesAsync(cancellationToken);
    }

    private async Task<AccessTokenResult> CreateAccessTokenAsync(ApplicationUser user, DateTimeOffset now)
    {
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);
        var roles = await userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(now.UtcDateTime).ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new("client_id", _options.Audience),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new AccessTokenResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt.UtcDateTime,
            (long)TimeSpan.FromMinutes(_options.AccessTokenMinutes).TotalSeconds);
    }

    private async Task<RefreshTokenResult> CreateRefreshTokenAsync(ApplicationUser user, DateTimeOffset now)
    {
        var value = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
        var expiresAt = now.AddDays(_options.RefreshTokenDays);
        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(value),
            SecurityStamp = await userManager.GetSecurityStampAsync(user),
            CreatedAtUtc = now.UtcDateTime,
            ExpiresAtUtc = expiresAt.UtcDateTime
        };

        return new RefreshTokenResult(value, expiresAt.UtcDateTime, entity);
    }

    private async Task RevokeAllActiveTokensAsync(string userId, DateTime revokedAtUtc, CancellationToken cancellationToken)
    {
        await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAtUtc, revokedAtUtc)
                    .SetProperty(token => token.ConcurrencyStamp, Guid.NewGuid()),
                cancellationToken);
    }

    private async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return false;
        }
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private static TokenResponse CreateResponse(AccessTokenResult accessToken, RefreshTokenResult refreshToken)
    {
        return new TokenResponse(
            "Bearer",
            accessToken.Value,
            accessToken.ExpiresIn,
            accessToken.ExpiresAtUtc,
            refreshToken.Value,
            refreshToken.ExpiresAtUtc);
    }

    private sealed record AccessTokenResult(string Value, DateTime ExpiresAtUtc, long ExpiresIn);
    private sealed record RefreshTokenResult(string Value, DateTime ExpiresAtUtc, RefreshToken Entity);
}
