namespace ScarletPigsServices.Api.Services.Ocap;

public sealed class OcapOptions
{
    public const string SectionName = "Ocap";

    public string PublicBaseUrl { get; set; } = string.Empty;
}

public sealed class OcapEventLinkingOptions
{
    public const string SectionName = "OcapEventLinking";

    public TimeSpan LookupWindow { get; set; } = TimeSpan.FromHours(5);
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromMinutes(1);
}
