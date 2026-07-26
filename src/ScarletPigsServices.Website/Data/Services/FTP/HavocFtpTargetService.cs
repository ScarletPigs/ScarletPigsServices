using Microsoft.Extensions.Configuration;

namespace ScarletPigsServices.Website.Data.Services.FTP;

public interface IHavocFtpTargetService
{
    bool IsConfigured(string targetName);

    Task<IReadOnlyList<string>> GetFolderNamesAsync(string targetName, CancellationToken cancellationToken = default);
}

public sealed class HavocFtpTargetService : IHavocFtpTargetService
{
    private readonly IConfiguration _configuration;

    public HavocFtpTargetService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsConfigured(string targetName)
    {
        return TryGetOptions(targetName, out _);
    }

    public async Task<IReadOnlyList<string>> GetFolderNamesAsync(string targetName, CancellationToken cancellationToken = default)
    {
        if (!TryGetOptions(targetName, out var options))
        {
            throw new InvalidOperationException($"FTP target '{targetName}' is not configured.");
        }

        using var connection = new HavocFtpConnection(options);
        return await connection.GetFolderNamesAsync(cancellationToken);
    }

    private bool TryGetOptions(string targetName, out HavocFtpOptions options)
    {
        var prefix = targetName.Equals("headless", StringComparison.OrdinalIgnoreCase)
            ? "HAVOC_HEADLESS_FTP"
            : "HAVOC_SERVER_FTP";

        var host = _configuration[$"{prefix}_HOST"];
        var portValue = _configuration[$"{prefix}_PORT"];
        var rootPath = _configuration[$"{prefix}_ROOT"];

        var user = _configuration[$"{prefix}_USER"]
            ?? _configuration["HAVOC_FTP_USER"];
        var password = _configuration[$"{prefix}_PASSWORD"]
            ?? _configuration["HAVOC_FTP_PASSWORD"];

        if (string.IsNullOrWhiteSpace(host))
        {
            options = new HavocFtpOptions();
            return false;
        }

        options = new HavocFtpOptions
        {
            Host = host,
            Username = user ?? string.Empty,
            Password = password ?? string.Empty,
            RootPath = string.IsNullOrWhiteSpace(rootPath) ? "/" : rootPath
        };

        if (int.TryParse(portValue, out var port))
        {
            options.Port = port;
        }

        return true;
    }
}