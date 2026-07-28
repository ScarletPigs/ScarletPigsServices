using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ScarletPigsServices.Data.Models;

namespace ScarletPigsServices.Data;

public sealed class ScarletPigsDbContextFactory : IDesignTimeDbContextFactory<ScarletPigsDbContext>
{
    public ScarletPigsDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__spdb")
            ?? Environment.GetEnvironmentVariable("PigletDBContext")
            ?? "Host=localhost;Database=spdb;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<ScarletPigsDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql
                    .MapEnum<ModSide>("mod_side")
                    .MapEnum<OverrideMode>("override_mode"))
            .Options;

        return new ScarletPigsDbContext(options);
    }
}
