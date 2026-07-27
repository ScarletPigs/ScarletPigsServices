using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Ridder.Hosting.Dokploy.Models;
using Ridder.Hosting.Dokploy.Services;
using Xunit;

namespace Ridder.Hosting.Dokploy.Tests;

public sealed class ContainerRegistryDigestResolverTests
{
    private const string Digest = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public async Task ResolveAsync_DoesNotSendConfiguredCredentialsToAnExternalRegistry()
    {
        using var handler = new RecordingHandler(
            CreateBearerChallenge("https://ghcr.io/token"),
            CreateTokenResponse("public-token"),
            CreateManifestResponse());
        using var client = new HttpClient(handler);
        var settings = CreateSettings("docker.io", "docker-user", "docker-password");

        var result = await ContainerRegistryDigestResolver.ResolveAsync(
            "ghcr.io/ocap2/web:2.1.1",
            settings,
            client,
            CancellationToken.None);

        Assert.Equal($"ghcr.io/ocap2/web:2.1.1@{Digest}", result);
        Assert.Collection(
            handler.Requests,
            request =>
            {
                Assert.Equal(HttpMethod.Head, request.Method);
                Assert.Equal("ghcr.io", request.Uri.Host);
                Assert.Null(request.Authorization);
            },
            request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("ghcr.io", request.Uri.Host);
                Assert.Null(request.Authorization);
            },
            request =>
            {
                Assert.Equal(HttpMethod.Head, request.Method);
                Assert.Equal("ghcr.io", request.Uri.Host);
                Assert.Equal("Bearer", request.Authorization?.Scheme);
                Assert.Equal("public-token", request.Authorization?.Parameter);
            });
    }

    [Fact]
    public async Task ResolveAsync_SendsCredentialsToTheConfiguredRegistry()
    {
        using var handler = new RecordingHandler(CreateManifestResponse());
        using var client = new HttpClient(handler);
        var settings = CreateSettings("docker.io", "docker-user", "docker-password");

        var result = await ContainerRegistryDigestResolver.ResolveAsync(
            "postgres:18.3",
            settings,
            client,
            CancellationToken.None);

        Assert.Equal($"postgres:18.3@{Digest}", result);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("registry-1.docker.io", request.Uri.Host);
        Assert.Equal("Basic", request.Authorization?.Scheme);
        Assert.Equal(
            Convert.ToBase64String(Encoding.UTF8.GetBytes("docker-user:docker-password")),
            request.Authorization?.Parameter);
    }

    private static DokployResolvedRegistrySettings CreateSettings(
        string registryUrl,
        string username,
        string password)
    {
        return new DokployResolvedRegistrySettings(
            DokployRegistryMode.Hosted,
            registryUrl,
            username,
            password,
            "cloud");
    }

    private static HttpResponseMessage CreateBearerChallenge(string realm)
    {
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.Add(
            new AuthenticationHeaderValue(
                "Bearer",
                $"realm=\"{realm}\",service=\"ghcr.io\",scope=\"repository:ocap2/web:pull\""));
        return response;
    }

    private static HttpResponseMessage CreateTokenResponse(string token)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($"{{\"token\":\"{token}\"}}", Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage CreateManifestResponse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.TryAddWithoutValidation("Docker-Content-Digest", Digest);
        return response;
    }

    private sealed class RecordingHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public List<RecordedRequest> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri ?? throw new InvalidOperationException("Request URI was not set."),
                request.Headers.Authorization));

            return Task.FromResult(_responses.Dequeue());
        }
    }

    private sealed record RecordedRequest(
        HttpMethod Method,
        Uri Uri,
        AuthenticationHeaderValue? Authorization);
}
