using System.Net;
using System.Text;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Ridder.Hosting.Dokploy.Models;
using Ridder.Hosting.Dokploy.Services;
using Xunit;

namespace Ridder.Hosting.Dokploy.Tests;

public sealed class DokployApplicationMountTests
{
    [Fact]
    public async Task ConfigureStatefulRolloutPolicyAsync_UsesStopFirstForVolumeBackedResource()
    {
        using var handler = new RecordingHandler(_ => JsonResponse("{}"));
        using var client = CreateClient(handler);
        var service = new DokployApplicationService(client, new DokployProjectService(client));

        await service.ConfigureStatefulRolloutPolicyAsync(
            new DokployApplication { Id = "db-app", Name = "dbserver", AppName = "dbserver-generated" },
            CreatePostgresResource(),
            CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/application.update", request.Uri.AbsolutePath);
        using var document = JsonDocument.Parse(request.Body!);
        var root = document.RootElement;
        var updateConfig = root.GetProperty("updateConfigSwarm");
        var rollbackConfig = root.GetProperty("rollbackConfigSwarm");
        Assert.Equal(1, updateConfig.GetProperty("Parallelism").GetInt32());
        Assert.Equal("rollback", updateConfig.GetProperty("FailureAction").GetString());
        Assert.Equal("stop-first", updateConfig.GetProperty("Order").GetString());
        Assert.Equal(1, rollbackConfig.GetProperty("Parallelism").GetInt32());
        Assert.Equal("pause", rollbackConfig.GetProperty("FailureAction").GetString());
        Assert.Equal("stop-first", rollbackConfig.GetProperty("Order").GetString());
    }

    [Fact]
    public async Task ConfigureStatefulRolloutPolicyAsync_DoesNotChangeStatelessResource()
    {
        using var handler = new RecordingHandler(_ => JsonResponse("{}"));
        using var client = CreateClient(handler);
        var service = new DokployApplicationService(client, new DokployProjectService(client));

        await service.ConfigureStatefulRolloutPolicyAsync(
            new DokployApplication { Id = "api-app", Name = "api", AppName = "api-generated" },
            new ContainerResource("api", "api:latest"),
            CancellationToken.None);

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task EnsureApplicationMountsAsync_UsesPersistedMountsAndRemovesDuplicateTarget()
    {
        using var handler = new RecordingHandler(request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return JsonResponse(
                    """
                    [
                      {
                        "mountId": "mount-to-keep",
                        "type": "volume",
                        "volumeName": "scarletpigs-postgres-data",
                        "mountPath": "/var/lib/postgresql",
                        "serviceType": "application",
                        "applicationId": "db-app"
                      },
                      {
                        "mountId": "duplicate-mount",
                        "type": "volume",
                        "volumeName": "scarletpigs-postgres-data",
                        "mountPath": "/var/lib/postgresql/",
                        "serviceType": "application",
                        "applicationId": "db-app"
                      }
                    ]
                    """);
            }

            return JsonResponse("""{"mountId":"duplicate-mount"}""");
        });
        using var client = CreateClient(handler);
        var service = new DokployApplicationService(client, new DokployProjectService(client));
        var resource = CreatePostgresResource();

        await service.EnsureApplicationMountsAsync(
            new DokployApplication { Id = "db-app", Name = "dbserver", AppName = "dbserver-generated" },
            resource);

        Assert.Collection(
            handler.Requests,
            request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal(
                    "/api/mounts.listByServiceId?serviceId=db-app&serviceType=application",
                    request.Uri.PathAndQuery);
            },
            request =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("/api/mounts.remove", request.Uri.AbsolutePath);
                Assert.Contains("\"mountId\":\"duplicate-mount\"", request.Body, StringComparison.Ordinal);
            });
    }

    [Fact]
    public async Task EnsureApplicationMountsAsync_DoesNotCreateAnExistingPersistedMount()
    {
        using var handler = new RecordingHandler(_ =>
            JsonResponse(
                """
                [
                  {
                    "mountId": "existing-mount",
                    "type": "volume",
                    "volumeName": "scarletpigs-postgres-data",
                    "mountPath": "/var/lib/postgresql",
                    "serviceType": "application",
                    "applicationId": "db-app"
                  }
                ]
                """));
        using var client = CreateClient(handler);
        var service = new DokployApplicationService(client, new DokployProjectService(client));

        await service.EnsureApplicationMountsAsync(
            new DokployApplication { Id = "db-app", Name = "dbserver", AppName = "dbserver-generated" },
            CreatePostgresResource());

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(
            "/api/mounts.listByServiceId?serviceId=db-app&serviceType=application",
            request.Uri.PathAndQuery);
    }

    private static ContainerResource CreatePostgresResource()
    {
        var resource = new ContainerResource("dbserver", "postgres:18.3");
        resource.Annotations.Add(
            new ContainerMountAnnotation(
                "scarletpigs-postgres-data",
                "/var/lib/postgresql",
                ContainerMountType.Volume,
                isReadOnly: false));
        return resource;
    }

    private static DokployApiClient CreateClient(HttpMessageHandler handler)
    {
        return new DokployApiClient(
            "test-api-key",
            "https://dokploy.test",
            new TestHostEnvironment(),
            NullLogger.Instance,
            new DokployResolvedRegistrySettings(
                DokployRegistryMode.Hosted,
                "docker.io",
                "registry-user",
                "registry-password",
                "cloud"),
            handler);
    }

    private static HttpResponseMessage JsonResponse(string content)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(
                new RecordedRequest(
                    request.Method,
                    request.RequestUri ?? throw new InvalidOperationException("Request URI was not set."),
                    request.Content is null
                        ? null
                        : await request.Content.ReadAsStringAsync(cancellationToken)));

            return responseFactory(request);
        }
    }

    private sealed record RecordedRequest(HttpMethod Method, Uri Uri, string? Body);

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "Ridder.Hosting.Dokploy.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
