using System.ComponentModel.DataAnnotations;

namespace ScarletPigsServices.Api.Authentication;

public sealed class RegisterRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    [MinLength(12)]
    public required string Password { get; init; }
}

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }
}

public sealed class RefreshRequest
{
    [Required]
    public required string RefreshToken { get; init; }
}

public sealed class RevokeRequest
{
    [Required]
    public required string RefreshToken { get; init; }
}

public sealed record TokenResponse(
    string TokenType,
    string AccessToken,
    long ExpiresIn,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
