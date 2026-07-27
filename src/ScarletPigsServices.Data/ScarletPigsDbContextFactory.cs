using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ScarletPigsServices.Data.Models;

namespace ScarletPigsServices.Data;

public sealed class ScarletPigsDbContextFactory : IDesignTimeDbContextFactory<ScarletPigsDbContext>
{
    public ScarletPigsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ScarletPigsDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=spdb;Username=postgres;Password=postgres",
                npgsql => npgsql
                    .MapEnum<ModSide>("mod_side")
                    .MapEnum<OverrideMode>("override_mode"))
            .Options;

        return new ScarletPigsDbContext(options);
    }
}
