using FluentFTP;
using Microsoft.Extensions.Configuration;
using ScarletPigsServices.Data.Files;
using System.Net;

namespace ScarletPigsServices.Api.Services.Files;

public interface IHavocFileService
{
    Task<HavocFoldersResponse> GetFoldersAsync(string targetName, CancellationToken cancellationToken = default);

    Task<MissionUploadResponse> UploadMissionAsync(string targetName, string folder, string fileName, Stream fileContent, CancellationToken cancellationToken = default);
}

public sealed class HavocFileService : IHavocFileService
{
    private static readonly IReadOnlySet<string> MissionUploadRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "UnitOrganizer",
        "MissionMaker"
    };

    private readonly IConfiguration _configuration;

    public HavocFileService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HavocFoldersResponse> GetFoldersAsync(string targetName, CancellationToken cancellationToken = default)
    {
        if (!TryGetOptions(targetName, out var options))
        {
            return new HavocFoldersResponse
            {
                TargetName = NormalizeTargetName(targetName),
                IsConfigured = false,
                Folders = Array.Empty<string>()
            };
        }

        using var connection = new HavocFtpConnection(options);
        return new HavocFoldersResponse
        {
            TargetName = NormalizeTargetName(targetName),
            IsConfigured = true,
            Folders = await connection.GetFolderNamesAsync(cancellationToken)
        };
    }

    public async Task<MissionUploadResponse> UploadMissionAsync(string targetName, string folder, string fileName, Stream fileContent, CancellationToken cancellationToken = default)
    {
        if (!TryGetOptions(targetName, out var options))
        {
            throw new InvalidOperationException($"FTP target '{targetName}' is not configured.");
        }

        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName) || !safeFileName.EndsWith(".pbo", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only .pbo mission files are allowed.");
        }

        var normalizedFolder = NormalizeFolder(folder);
        var relativeRemotePath = BuildRelativeRemotePath(normalizedFolder, safeFileName);

        using var connection = new HavocFtpConnection(options);
        await connection.UploadFileAsync(relativeRemotePath, fileContent, cancellationToken);

        return new MissionUploadResponse
        {
            TargetName = NormalizeTargetName(targetName),
            Folder = normalizedFolder,
            FileName = safeFileName,
            RemotePath = relativeRemotePath
        };
    }

    private bool TryGetOptions(string targetName, out HavocFtpOptions options)
    {
        var prefix = NormalizeTargetName(targetName).Equals("headless", StringComparison.OrdinalIgnoreCase)
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

    private static string NormalizeTargetName(string? targetName)
    {
        return string.IsNullOrWhiteSpace(targetName) ? "server" : targetName.Trim();
    }

    private static string NormalizeFolder(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || folder == "/")
        {
            return "/";
        }

        var segments = folder
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Any(segment => segment is "." or ".."))
        {
            throw new InvalidOperationException("Folder path contains unsupported navigation segments.");
        }

        return segments.Length == 0 ? "/" : $"/{string.Join('/', segments)}";
    }

    private static string BuildRelativeRemotePath(string folder, string fileName)
    {
        return folder == "/"
            ? $"/{fileName}"
            : $"{folder}/{fileName}";
    }

    private sealed class HavocFtpConnection : IDisposable
    {
        private readonly HavocFtpOptions _options;
        private AsyncFtpClient? _client;

        public HavocFtpConnection(HavocFtpOptions options)
        {
            _options = options;
            ValidateOptions(_options);
        }

        public async Task<IReadOnlyList<string>> GetFolderNamesAsync(CancellationToken cancellationToken = default)
        {
            var client = await ConnectAsync(cancellationToken);
            var listing = await client.GetListing(GetRootPath(), cancellationToken);

            return listing
                .Where(item => item.Type == FtpObjectType.Directory)
                .Select(item => item.Name)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public async Task UploadFileAsync(string relativeRemotePath, Stream fileContent, CancellationToken cancellationToken = default)
        {
            var client = await ConnectAsync(cancellationToken);

            if (fileContent.CanSeek)
            {
                fileContent.Position = 0;
            }

            var status = await client.UploadStream(
                fileContent,
                CombineWithRootPath(relativeRemotePath),
                token: cancellationToken,
                createRemoteDir: true,
                existsMode: FtpRemoteExists.Overwrite);

            if (status is not (FtpStatus.Success or FtpStatus.Skipped))
            {
                throw new InvalidOperationException($"FTP upload failed with status '{status}'.");
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }

        private async Task<AsyncFtpClient> ConnectAsync(CancellationToken cancellationToken)
        {
            if (_client == null || _client.IsDisposed)
            {
                _client = new AsyncFtpClient
                {
                    Host = _options.Host,
                    Port = _options.Port,
                    Credentials = new NetworkCredential(_options.Username, _options.Password),
                    Config = new FtpConfig
                    {
                        ValidateAnyCertificate = true,
                        ConnectTimeout = 30000,
                        ReadTimeout = 30000
                    }
                };
            }

            if (!await _client.IsStillConnected(10000, cancellationToken))
            {
                await _client.Connect(cancellationToken);
            }

            return _client;
        }

        private string GetRootPath()
        {
            var rootPath = _options.RootPath?.Trim();
            return string.IsNullOrWhiteSpace(rootPath) ? "/" : rootPath.Replace('\\', '/');
        }

        private string CombineWithRootPath(string relativeRemotePath)
        {
            var rootPath = GetRootPath().TrimEnd('/');
            var normalizedRelativePath = string.IsNullOrWhiteSpace(relativeRemotePath)
                ? string.Empty
                : "/" + relativeRemotePath.Trim().TrimStart('/');

            return string.IsNullOrWhiteSpace(rootPath) || rootPath == "/"
                ? normalizedRelativePath == string.Empty ? "/" : normalizedRelativePath
                : $"{rootPath}{normalizedRelativePath}";
        }

        private static void ValidateOptions(HavocFtpOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Host))
            {
                throw new InvalidOperationException("HAVOC FTP host is not configured.");
            }

            if (options.Port <= 0)
            {
                throw new InvalidOperationException("HAVOC FTP port must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(options.Username))
            {
                throw new InvalidOperationException("HAVOC FTP user is not configured.");
            }

            if (string.IsNullOrWhiteSpace(options.Password))
            {
                throw new InvalidOperationException("HAVOC FTP password is not configured.");
            }
        }
    }
}