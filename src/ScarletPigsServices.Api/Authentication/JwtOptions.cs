namespace ScarletPigsServices.Api.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Authentication";

    public string Issuer { get; set; } = "ScarletPigsServices.Api";
    public string Audience { get; set; } = "ScarletPigsClients";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
}
