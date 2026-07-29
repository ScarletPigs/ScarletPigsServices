using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using ScarletPigsServices.Api.Controllers;
using Swashbuckle.AspNetCore.Swagger;

namespace ScarletPigsServices.Api.Tests;

public sealed class OcapOpenApiTests
{
    [Fact]
    public void Swagger_ContainsEveryExposedOcapRoute()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services
            .AddControllers()
            .AddApplicationPart(typeof(OcapProxyController).Assembly);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Scarlet Pigs API",
                Version = "v1"
            });
            options.IncludeXmlComments(
                Path.Combine(AppContext.BaseDirectory, "ScarletPigsServices.Api.xml"));
        });

        using var app = builder.Build();
        var swagger = app.Services
            .GetRequiredService<ISwaggerProvider>()
            .GetSwagger("v1");

        var expectedPaths = new[]
        {
            "/api/ocap/api/healthcheck",
            "/api/ocap/api/version",
            "/api/ocap/api/v1/operations",
            "/api/ocap/api/v1/operations/{id}",
            "/api/ocap/api/v1/operations/{id}/marker-blacklist",
            "/api/ocap/api/v1/worlds",
            "/api/ocap/api/v1/customize",
            "/api/ocap/data/{path}",
            "/api/ocap/images/markers/{name}/{color}",
            "/api/ocap/images/markers/magicons/{name}",
            "/api/ocap/images/maps/fonts/{fontstack}/{range}",
            "/api/ocap/images/maps/sprites/{name}",
            "/api/ocap/images/maps/{path}"
        };

        Assert.All(expectedPaths, path => Assert.Contains(path, swagger.Paths.Keys));
        Assert.DoesNotContain(
            swagger.Paths.Keys,
            path => path.Equals(
                "/api/ocap/{path}",
                StringComparison.OrdinalIgnoreCase));

        var operations = swagger.Paths["/api/ocap/api/v1/operations"]
            .Operations[OperationType.Get];
        Assert.Equal("Lists OCAP recordings.", operations.Summary);
        Assert.Equal(
            ["name", "older", "newer", "tag"],
            operations.Parameters.Select(parameter => parameter.Name));
        Assert.Contains("application/json", operations.Responses["200"].Content.Keys);
        Assert.Equal(
            "OcapOperation",
            operations.Responses["200"]
                .Content["application/json"]
                .Schema.Items.Reference.Id);
    }

    [Theory]
    [InlineData("recording/manifest.pb", true)]
    [InlineData("maps/altis/map.json", true)]
    [InlineData("", false)]
    [InlineData("../api/v1/operations", false)]
    [InlineData("data/../db/operations.sqlite", false)]
    [InlineData("/recording/manifest.pb", false)]
    [InlineData("//recording/manifest.pb", false)]
    [InlineData("recording\\manifest.pb", false)]
    public void AssetPathSafety_RejectsTraversalAndRootedPaths(string path, bool expected)
    {
        Assert.Equal(expected, OcapAssetPath.IsSafe(path));
    }
}
