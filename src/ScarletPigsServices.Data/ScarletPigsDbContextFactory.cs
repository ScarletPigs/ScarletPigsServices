using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ScarletPigsServices.Data.Models;

namespace ScarletPigsServices.Data;

public sealed class ScarletPigsDbContextFactory : IDesignTimeDbContextFactory<ScarletPigsDbContext>
{
    // Aspire supplies unresolved resource expressions while generating bundles.
    // The deployed bundle receives the real connection string when it runs.
    private const string DesignTimeConnectionString =
        "Host=localhost;Database=spdb;Username=postgres;Password=postgres";

    public ScarletPigsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ScarletPigsDbContext>()
            .UseNpgsql(
                DesignTimeConnectionString,
                npgsql => npgsql
                    .MapEnum<ModSide>("mod_side")
                    .MapEnum<OverrideMode>("override_mode"))
            .Options;

        return new ScarletPigsDbContext(options);
    }
}
