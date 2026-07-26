using FluentFTP;
using Microsoft.Extensions.Options;
using System.Net;

namespace ScarletPigsServices.Website.Data.Services.FTP;

public interface IHavocFtpConnection : IDisposable
{
    Task<IReadOnlyList<string>> GetFolderNamesAsync(CancellationToken cancellationToken = default);

    Task UploadFileAsync(string remotePath, Stream fileContent, CancellationToken cancellationToken = default);
}

public sealed class HavocFtpConnection : IHavocFtpConnection
{
    private readonly HavocFtpOptions _options;
    private AsyncFtpClient? _client;

    public HavocFtpConnection(IOptions<HavocFtpOptions> options)
    {
        _options = options.Value;
        ValidateOptions(_options);
    }

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

    public async Task UploadFileAsync(string remotePath, Stream fileContent, CancellationToken cancellationToken = default)
    {
        var client = await ConnectAsync(cancellationToken);

        if (fileContent.CanSeek)
        {
            fileContent.Position = 0;
        }

        var status = await client.UploadStream(
            fileContent,
            remotePath,
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

    private static void ValidateOptions(HavocFtpOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Host))
        {
            throw new InvalidOperationException("HAVOC_SERVER_FTP_HOST is not configured.");
        }

        if (options.Port <= 0)
        {
            throw new InvalidOperationException("HAVOC_SERVER_FTP_PORT must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(options.Username))
        {
            throw new InvalidOperationException("HAVOC_FTP_USER is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            throw new InvalidOperationException("HAVOC_FTP_PASSWORD is not configured.");
        }
    }
}