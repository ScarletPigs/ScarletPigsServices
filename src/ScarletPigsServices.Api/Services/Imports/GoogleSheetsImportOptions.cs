namespace ScarletPigsServices.Api.Services.Imports;

public sealed class GoogleSheetsImportOptions
{
    public const string SectionName = "GoogleSheetsImport";

    public string SpreadsheetName { get; set; } = string.Empty;
    public string? SpreadsheetId { get; set; }
    public string Type { get; set; } = "service_account";
    public string ProjectId { get; set; } = string.Empty;
    public string PrivateKeyId { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string AuthUri { get; set; } = string.Empty;
    public string TokenUri { get; set; } = string.Empty;
    public string AuthProviderX509CertUrl { get; set; } = string.Empty;
    public string ClientX509CertUrl { get; set; } = string.Empty;
}
