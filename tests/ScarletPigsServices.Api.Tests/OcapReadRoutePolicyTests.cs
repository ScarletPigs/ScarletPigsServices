using ScarletPigsServices.Api.Controllers;

namespace ScarletPigsServices.Api.Tests;

public sealed class OcapReadRoutePolicyTests
{
    [Theory]
    [InlineData("api/healthcheck")]
    [InlineData("api/v1/operations")]
    [InlineData("api/v1/operations/42")]
    [InlineData("data/recording/manifest.pb")]
    [InlineData("images/maps/altis/map.json")]
    public void IsAllowed_AcceptsSupportedReadRoutes(string path)
    {
        Assert.True(OcapReadRoutePolicy.IsAllowed(path));
    }

    [Theory]
    [InlineData("")]
    [InlineData("api/v1/auth/steam")]
    [InlineData("api/v1/stream")]
    [InlineData("../api/v1/operations")]
    [InlineData("data/../db/operations.sqlite")]
    [InlineData("/data/recording/manifest.pb")]
    [InlineData("//data/recording/manifest.pb")]
    public void IsAllowed_RejectsUnsupportedOrUnsafeRoutes(string path)
    {
        Assert.False(OcapReadRoutePolicy.IsAllowed(path));
    }
}
