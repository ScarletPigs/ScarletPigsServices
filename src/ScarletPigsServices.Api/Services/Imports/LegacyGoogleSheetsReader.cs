using System.Globalization;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Microsoft.Extensions.Options;

namespace ScarletPigsServices.Api.Services.Imports;

public interface ILegacyGoogleSheetsReader
{
    Task<LegacyGoogleSheetsData> ReadAsync(CancellationToken cancellationToken);
}

public sealed record LegacyOperation(string Date, string Name, string Author, string Source);

public sealed record LegacyGoogleSheetsData(
    int DateAmount,
    JsonElement ScheduleMessages,
    JsonElement ModlistMessages,
    JsonElement? QuestionnaireMessage,
    IReadOnlyList<IReadOnlyList<string>> QuestionnaireInfo,
    IReadOnlyList<LegacyOperation> Operations);

public sealed class LegacyGoogleSheetsReader(
    IOptions<GoogleSheetsImportOptions> options) : ILegacyGoogleSheetsReader
{
    private const string SpreadsheetMimeType = "application/vnd.google-apps.spreadsheet";
    private readonly GoogleSheetsImportOptions _options = options.Value;

    public async Task<LegacyGoogleSheetsData> ReadAsync(CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var credentialJson = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["type"] = _options.Type,
            ["project_id"] = _options.ProjectId,
            ["private_key_id"] = _options.PrivateKeyId,
            ["private_key"] = _options.PrivateKey.Replace("\\n", "\n", StringComparison.Ordinal),
            ["client_email"] = _options.ClientEmail,
            ["client_id"] = _options.ClientId,
            ["auth_uri"] = _options.AuthUri,
            ["token_uri"] = _options.TokenUri,
            ["auth_provider_x509_cert_url"] = _options.AuthProviderX509CertUrl,
            ["client_x509_cert_url"] = _options.ClientX509CertUrl
        });
        var credential = CredentialFactory
            .FromJson(credentialJson, _options.Type)
            .CreateScoped(SheetsService.Scope.SpreadsheetsReadonly, DriveService.Scope.DriveReadonly);
        var initializer = new BaseClientService.Initializer
        {
            ApplicationName = "Scarlet Pigs legacy data import",
            HttpClientInitializer = credential
        };

        using var drive = new DriveService(initializer);
        using var sheets = new SheetsService(initializer);
        var spreadsheetId = !string.IsNullOrWhiteSpace(_options.SpreadsheetId)
            ? _options.SpreadsheetId
            : await FindSpreadsheetIdAsync(drive, cancellationToken);

        var spreadsheet = await sheets.Spreadsheets
            .Get(spreadsheetId)
            .ExecuteAsync(cancellationToken);
        var worksheetTitles = LegacyGoogleSheetsWorksheetResolver.Resolve(
            spreadsheet.Sheets.Select(sheet => sheet.Properties.Title));

        var scheduleRows = await ReadWorksheetAsync(
            sheets, spreadsheetId, worksheetTitles.ActiveSchedule, cancellationToken);
        var archiveRows = await ReadWorksheetAsync(
            sheets, spreadsheetId, worksheetTitles.OldOps, cancellationToken);
        var questionnaireRows = await ReadWorksheetAsync(
            sheets, spreadsheetId, worksheetTitles.DlcInfo, cancellationToken);
        return LegacyGoogleSheetsParser.Parse(
            scheduleRows, archiveRows, questionnaireRows);
    }

    private async Task<string> FindSpreadsheetIdAsync(
        DriveService drive,
        CancellationToken cancellationToken)
    {
        var escapedName = _options.SpreadsheetName.Replace("'", "\\'", StringComparison.Ordinal);
        var request = drive.Files.List();
        request.Q =
            $"name = '{escapedName}' and mimeType = '{SpreadsheetMimeType}' and trashed = false";
        request.PageSize = 2;
        request.Spaces = "drive";
        request.IncludeItemsFromAllDrives = true;
        request.SupportsAllDrives = true;
        var response = await request.ExecuteAsync(cancellationToken);
        return response.Files.Count switch
        {
            0 => throw new InvalidOperationException(
                $"No Google Sheets workbook named '{_options.SpreadsheetName}' was found."),
            > 1 => throw new InvalidOperationException(
                $"More than one Google Sheets workbook named '{_options.SpreadsheetName}' was found. Configure GoogleSheetsImport:SpreadsheetId."),
            _ => response.Files[0].Id
        };
    }

    private static async Task<IReadOnlyList<IReadOnlyList<string>>> ReadWorksheetAsync(
        SheetsService sheets,
        string spreadsheetId,
        string worksheetTitle,
        CancellationToken cancellationToken)
    {
        var escapedTitle = worksheetTitle.Replace("'", "''", StringComparison.Ordinal);
        var response = await sheets.Spreadsheets.Values
            .Get(spreadsheetId, $"'{escapedTitle}'")
            .ExecuteAsync(cancellationToken);
        return response.Values?
            .Select(row => (IReadOnlyList<string>)row
                .Select(value => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)
                .ToArray())
            .ToArray()
            ?? [];
    }

    private void EnsureConfigured()
    {
        var missing = new Dictionary<string, string>
        {
            [nameof(_options.Type)] = _options.Type,
            [nameof(_options.ProjectId)] = _options.ProjectId,
            [nameof(_options.PrivateKeyId)] = _options.PrivateKeyId,
            [nameof(_options.PrivateKey)] = _options.PrivateKey,
            [nameof(_options.ClientEmail)] = _options.ClientEmail,
            [nameof(_options.ClientId)] = _options.ClientId,
            [nameof(_options.AuthUri)] = _options.AuthUri,
            [nameof(_options.TokenUri)] = _options.TokenUri,
            [nameof(_options.AuthProviderX509CertUrl)] = _options.AuthProviderX509CertUrl,
            [nameof(_options.ClientX509CertUrl)] = _options.ClientX509CertUrl
        }
        .Where(item => string.IsNullOrWhiteSpace(item.Value))
        .Select(item => item.Key)
        .ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                $"Google Sheets import configuration is missing: {string.Join(", ", missing)}.");
        }
        if (string.IsNullOrWhiteSpace(_options.SpreadsheetId)
            && string.IsNullOrWhiteSpace(_options.SpreadsheetName))
        {
            throw new InvalidOperationException(
                "GoogleSheetsImport:SpreadsheetName or SpreadsheetId must be configured.");
        }
    }
}

internal sealed record LegacyGoogleSheetsWorksheetTitles(
    string ActiveSchedule,
    string DlcInfo,
    string OldOps);

internal static class LegacyGoogleSheetsWorksheetResolver
{
    public const string ActiveSchedule = "Active Schedule";
    public const string DlcInfo = "DLC Info";
    public const string OldOps = "Old Ops";

    public static LegacyGoogleSheetsWorksheetTitles Resolve(
        IEnumerable<string> worksheetTitles)
    {
        var available = worksheetTitles.ToHashSet(StringComparer.Ordinal);
        var missing = new[] { ActiveSchedule, DlcInfo, OldOps }
            .Where(title => !available.Contains(title))
            .ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                $"The legacy workbook is missing required worksheets: {string.Join(", ", missing)}.");
        }

        return new LegacyGoogleSheetsWorksheetTitles(
            ActiveSchedule,
            DlcInfo,
            OldOps);
    }
}

public static class LegacyGoogleSheetsParser
{
    public static LegacyGoogleSheetsData Parse(
        IReadOnlyList<IReadOnlyList<string>> scheduleRows,
        IReadOnlyList<IReadOnlyList<string>> archiveRows,
        IReadOnlyList<IReadOnlyList<string>> questionnaireRows)
    {
        var dateAmountText = Cell(scheduleRows, 2, 7);
        if (!int.TryParse(dateAmountText, CultureInfo.InvariantCulture, out var dateAmount))
        {
            throw new InvalidOperationException(
                $"The legacy schedule date amount '{dateAmountText}' is not an integer.");
        }

        return new LegacyGoogleSheetsData(
            dateAmount,
            ParseObject(Cell(scheduleRows, 3, 7), """{"servers":[]}"""),
            ParseObject(Cell(scheduleRows, 4, 7), """{"servers":[]}"""),
            ParseOptionalObject(Cell(scheduleRows, 5, 7)),
            questionnaireRows,
            [
                .. ReadOperations(scheduleRows, "schedule"),
                .. ReadOperations(archiveRows, "archive")
            ]);
    }

    private static JsonElement ParseObject(string value, string defaultJson)
    {
        using var document = JsonDocument.Parse(
            string.IsNullOrWhiteSpace(value) ? defaultJson : value);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                "A legacy settings cell did not contain a JSON object.");
        }
        return document.RootElement.Clone();
    }

    private static JsonElement? ParseOptionalObject(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : ParseObject(value, "{}");

    private static IEnumerable<LegacyOperation> ReadOperations(
        IReadOnlyList<IReadOnlyList<string>> rows,
        string source)
    {
        foreach (var row in rows)
        {
            var date = Column(row, 0).Trim();
            var name = Column(row, 1).Trim();
            var author = Column(row, 2).Trim();
            if (string.IsNullOrWhiteSpace(date)
                || date.Equals("Date", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }
            yield return new LegacyOperation(date, name, author, source);
        }
    }

    private static string Cell(
        IReadOnlyList<IReadOnlyList<string>> rows,
        int row,
        int column) =>
        row <= rows.Count && column <= rows[row - 1].Count
            ? rows[row - 1][column - 1]
            : string.Empty;

    private static string Column(IReadOnlyList<string> row, int column) =>
        column < row.Count ? row[column] : string.Empty;
}
