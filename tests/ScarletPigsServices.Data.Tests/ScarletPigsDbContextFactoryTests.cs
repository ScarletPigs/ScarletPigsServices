using Microsoft.EntityFrameworkCore;
using Npgsql;
using ScarletPigsServices.Data;
using Xunit;

namespace ScarletPigsServices.Data.Tests;

public sealed class ScarletPigsDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_IgnoresRuntimeConnectionEnvironment()
    {
        const string aspireReference = "{spdb.connectionString}";
        var originalConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__spdb");
        var originalLegacyConnection =
            Environment.GetEnvironmentVariable("PigletDBContext");

        try
        {
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__spdb",
                aspireReference);
            Environment.SetEnvironmentVariable(
                "PigletDBContext",
                "not-an-npgsql-connection-string");

            using var context =
                new ScarletPigsDbContextFactory().CreateDbContext([]);
            var connectionString = new NpgsqlConnectionStringBuilder(
                context.Database.GetConnectionString());

            Assert.Equal("localhost", connectionString.Host);
            Assert.Equal("spdb", connectionString.Database);
            Assert.Equal("postgres", connectionString.Username);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__spdb",
                originalConnectionString);
            Environment.SetEnvironmentVariable(
                "PigletDBContext",
                originalLegacyConnection);
        }
    }
}
