using ScarletPigsServices.Data;
using ScarletPigsServices.MigrationService;
using ScarletPigsServices.ServiceReferences;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<ScarletPigsDbContext>(ServiceRefs.DB);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
