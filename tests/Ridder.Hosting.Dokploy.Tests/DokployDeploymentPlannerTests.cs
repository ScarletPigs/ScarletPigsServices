using Aspire.Hosting.ApplicationModel;
using Ridder.Hosting.Dokploy.Services;
using Xunit;

namespace Ridder.Hosting.Dokploy.Tests;

public sealed class DokployDeploymentPlannerTests
{
    [Fact]
    public void CreateBatches_OrdersWaitDependencies()
    {
        var database = new ContainerResource("database");
        var migrations = new ContainerResource("migrations");
        var api = new ContainerResource("api");
        WaitFor(migrations, database, WaitType.WaitUntilHealthy);
        WaitFor(api, migrations, WaitType.WaitForCompletion);

        var batches = DokployDeploymentPlanner.CreateBatches([api, migrations, database]);

        Assert.Collection(
            batches,
            batch => Assert.Equal(["database"], GetResourceNames(batch)),
            batch => Assert.Equal(["migrations"], GetResourceNames(batch)),
            batch => Assert.Equal(["api"], GetResourceNames(batch)));
    }

    [Fact]
    public void CreateBatches_GroupsIndependentResources()
    {
        var database = new ContainerResource("database");
        var cache = new ContainerResource("cache");
        var migrations = new ContainerResource("migrations");
        WaitFor(migrations, database, WaitType.WaitUntilHealthy);

        var batches = DokployDeploymentPlanner.CreateBatches([migrations, cache, database]);

        Assert.Collection(
            batches,
            batch => Assert.Equal(["cache", "database"], GetResourceNames(batch)),
            batch => Assert.Equal(["migrations"], GetResourceNames(batch)));
    }

    [Fact]
    public void CreateBatches_IgnoresWaitsForResourcesOutsideTheDeployment()
    {
        var externalDependency = new ContainerResource("external");
        var api = new ContainerResource("api");
        WaitFor(api, externalDependency, WaitType.WaitUntilHealthy);

        var batches = DokployDeploymentPlanner.CreateBatches([api]);

        var batch = Assert.Single(batches);
        Assert.Equal(["api"], GetResourceNames(batch));
    }

    [Fact]
    public void CreateBatches_RejectsDependencyCycles()
    {
        var api = new ContainerResource("api");
        var database = new ContainerResource("database");
        WaitFor(api, database, WaitType.WaitUntilHealthy);
        WaitFor(database, api, WaitType.WaitUntilHealthy);

        var exception = Assert.Throws<InvalidOperationException>(
            () => DokployDeploymentPlanner.CreateBatches([api, database]));

        Assert.Contains("api waits for [database]", exception.Message);
        Assert.Contains("database waits for [api]", exception.Message);
    }

    private static void WaitFor(
        IComputeResource resource,
        IResource dependency,
        WaitType waitType)
    {
        resource.Annotations.Add(new WaitAnnotation(dependency, waitType));
    }

    private static string[] GetResourceNames(IEnumerable<IComputeResource> resources)
    {
        return resources.Select(resource => resource.Name).ToArray();
    }
}
