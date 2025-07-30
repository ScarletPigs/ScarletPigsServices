using Microsoft.EntityFrameworkCore;
using ScarletPigsServices.Data;
using ScarletPigsServices.Data.Events;
using System.Diagnostics;

namespace ScarletPigsServices.MigrationService;

public class Worker(IServiceProvider serviceProvider, IHostApplicationLifetime hostApplicationLifetime, IHostEnvironment env) : BackgroundService
{
    public const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource _activitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = _activitySource.StartActivity("Migrating database", ActivityKind.Client);

        try 
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ScarletPigsDbContext>();

            await RunMigrationsAsync(dbContext, stoppingToken);

            if (env.IsDevelopment())
            {
                await RunSeedAsync(dbContext, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }
        finally
        {
            hostApplicationLifetime.StopApplication();
        }

    }

    private async Task RunMigrationsAsync(ScarletPigsDbContext dbContext, CancellationToken stoppingToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        
        await strategy.ExecuteAsync(async () =>
        {
            await dbContext.Database.MigrateAsync();
        });
    }

    private async Task RunSeedAsync(ScarletPigsDbContext dbContext, CancellationToken stoppingToken)
    {

        // EventType seeding data
        List<EventType> eventTypes = new()
        {
            new EventType { Name = "Piglet Birth", Description = "A piglet is born." },
            new EventType { Name = "Piglet Weaning", Description = "A piglet is weaned from its mother." },
            new EventType { Name = "Piglet Vaccination", Description = "A piglet receives a vaccination." },
            new EventType { Name = "Piglet Sale", Description = "A piglet is sold." }
        };

        // Event seeding data
        List<Event> events = new()
        {
            new Event
            {
                Name = "Piglet Born",
                CreatorDiscordUsername = "ScarletPig",
                Author = "ScarletPig",
                Description = "A new piglet has been born on the farm.",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(1)
            },
            new Event
            {
                Name = "Piglet Weaned",
                CreatorDiscordUsername = "ScarletPig",
                Author = "ScarletPig",
                Description = "A piglet has been weaned from its mother.",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                StartTime = DateTime.UtcNow.AddDays(7),
                EndTime = DateTime.UtcNow.AddDays(7).AddHours(1)
            },
            new Event
            {
                Name = "Piglet Vaccinated",
                CreatorDiscordUsername = "ScarletPig",
                Author = "ScarletPig",
                Description = "A piglet has received its vaccination.",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                StartTime = DateTime.UtcNow.AddDays(14),
                EndTime = DateTime.UtcNow.AddDays(14).AddHours(1)
            }
        };




        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await dbContext.Database.BeginTransactionAsync(stoppingToken);

            if (!await dbContext.EventTypes.AnyAsync(stoppingToken))
            {
                dbContext.EventTypes.AddRange(eventTypes);
                await dbContext.SaveChangesAsync(stoppingToken);
            }

            if (!await dbContext.Events.AnyAsync(stoppingToken))
            {
                dbContext.Events.AddRange(events);
                await dbContext.SaveChangesAsync(stoppingToken);
            }

            await transaction.CommitAsync(stoppingToken);

        });
    }
}
