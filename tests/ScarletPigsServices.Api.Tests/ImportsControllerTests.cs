using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ScarletPigsServices.Api.Services.Imports;
using Xunit;

namespace ScarletPigsServices.Api.Tests;

public sealed class ImportsControllerTests
    : IClassFixture<ImportsControllerTests.ApiFactory>
{
    private const string ApiKey = "12345678901234567890123456789012";
    private readonly ApiFactory _factory;

    public ImportsControllerTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Endpoint_RequiresApiKeyAndReturnsImportResult()
    {
        using var client = _factory.CreateClient();
        var unauthorized = await client.PostAsync(
            "/api/admin/imports/google-sheets",
            null,
            CancellationToken.None);

        client.DefaultRequestHeaders.Add("X-API-Key", ApiKey);
        var response = await client.PostAsync(
            "/api/admin/imports/google-sheets",
            null,
            CancellationToken.None);
        using var result = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(CancellationToken.None));

        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "completed",
            result.RootElement.GetProperty("status").GetString());
        Assert.Equal(
            3,
            result.RootElement.GetProperty("events_imported").GetInt32());
    }

    public sealed class ApiFactory
        : WebApplicationFactory<ScarletPigsServices.Api.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ApiKey:Key", ApiKey);
            builder.UseSetting(
                "ConnectionStrings:spdb",
                "Host=localhost;Database=test;Username=test;Password=test");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ILegacyGoogleSheetsImportService>();
                services.AddScoped<ILegacyGoogleSheetsImportService>(
                    _ => new FakeImportService());
            });
        }
    }

    private sealed class FakeImportService : ILegacyGoogleSheetsImportService
    {
        public Task<LegacyGoogleSheetsImportResult> ImportAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult(new LegacyGoogleSheetsImportResult(
                "completed", 3, 1, 5));
    }
}
