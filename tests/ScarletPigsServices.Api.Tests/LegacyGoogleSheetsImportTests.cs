using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ScarletPigsServices.Api.Services.Imports;
using ScarletPigsServices.Data;
using ScarletPigsServices.Data.Models;
using Xunit;

namespace ScarletPigsServices.Api.Tests;

public sealed class LegacyGoogleSheetsImportTests
{
    [Fact]
    public void WorksheetResolver_UsesNamesRegardlessOfTabOrder()
    {
        var result = LegacyGoogleSheetsWorksheetResolver.Resolve(
            ["DLC Info", "Other Sheet", "Old Ops", "Active Schedule"]);

        Assert.Equal("Active Schedule", result.ActiveSchedule);
        Assert.Equal("DLC Info", result.DlcInfo);
        Assert.Equal("Old Ops", result.OldOps);
    }

    [Fact]
    public void WorksheetResolver_ReportsMissingRequiredSheet()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => LegacyGoogleSheetsWorksheetResolver.Resolve(
                ["Active Schedule", "DLC Info"]));

        Assert.Contains("Old Ops", exception.Message);
    }

    [Fact]
    public void Parser_ReadsSettingsAndBothOperationWorksheets()
    {
        IReadOnlyList<IReadOnlyList<string>> schedule =
        [
            ["Date", "Name", "Author", "", "", "", "Setting"],
            ["Aug 02 (26)", "Current op", "Alice", "", "", "", "10"],
            ["", "", "", "", "", "", """{"servers":[]}"""],
            ["", "", "", "", "", "", """{"servers":[]}"""],
            ["", "", "", "", "", "", ""]
        ];
        IReadOnlyList<IReadOnlyList<string>> archive =
        [
            ["Date", "Name", "Author"],
            ["Jul 26 (26)", "Archived op", "Bob"]
        ];

        var result = LegacyGoogleSheetsParser.Parse(
            schedule,
            archive,
            [["DLC", "Count", "Emoji"]]);

        Assert.Equal(10, result.DateAmount);
        Assert.Equal(2, result.Operations.Count);
        Assert.Equal("schedule", result.Operations[0].Source);
        Assert.Equal("archive", result.Operations[1].Source);
        Assert.Null(result.QuestionnaireMessage);
    }

    [Fact]
    public void Planner_CreatesEventsAndSkipsDatesAlreadyInTheApi()
    {
        var existing = new Event
        {
            Name = "Existing",
            StartsAt = new DateTimeOffset(2026, 8, 2, 15, 0, 0, TimeSpan.FromHours(2)),
            TypeKey = LegacyGoogleSheetsImportService.EventTypeKey
        };

        var plan = LegacyGoogleSheetsImportPlanner.CreateEventPlan(
            [
                new LegacyOperation("Aug 02 (26)", "Duplicate", "Alice", "schedule"),
                new LegacyOperation("Jul 26 (26)", "Archived op", "Bob", "archive")
            ],
            [existing],
            DateTimeOffset.Parse("2026-07-28T10:00:00Z"));

        Assert.Equal(1, plan.Skipped);
        var imported = Assert.Single(plan.Events);
        Assert.Equal("Archived op", imported.Name);
        Assert.Equal("Bob", imported.Author);
        Assert.Equal("piglet-google-sheets:2026-07-26", imported.ExternalId);
        Assert.Equal(TimeSpan.Zero, imported.StartsAt.Offset);
        Assert.Equal(
            new DateTimeOffset(2026, 7, 26, 13, 0, 0, TimeSpan.Zero),
            imported.StartsAt);
        Assert.Equal(
            new DateTime(2026, 7, 26, 15, 0, 0),
            TimeZoneInfo.ConvertTime(
                imported.StartsAt,
                TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen")).DateTime);
    }

    [Fact]
    public async Task Service_RunsRelationalTransactionInsideExecutionStrategy()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var modelOptions = new DbContextOptionsBuilder<TestModelContext>()
            .UseSqlite(connection)
            .Options;
        await using var modelContext = new TestModelContext(modelOptions);
        await modelContext.Database.EnsureCreatedAsync();
        var options = new DbContextOptionsBuilder<ScarletPigsDbContext>()
            .UseSqlite(connection)
            .UseModel(modelContext.Model)
            .ReplaceService<IExecutionStrategyFactory, RetryingExecutionStrategyFactory>()
            .Options;
        await using var db = new ScarletPigsDbContext(options);
        var reader = new FakeReader(new LegacyGoogleSheetsData(
            10,
            JsonSerializer.SerializeToElement(new { servers = Array.Empty<object>() }),
            JsonSerializer.SerializeToElement(new { servers = Array.Empty<object>() }),
            null,
            [],
            [new LegacyOperation("Aug 02 (26)", "Current op", "Alice", "schedule")]));
        var service = new LegacyGoogleSheetsImportService(db, reader);

        var result = await service.ImportAsync(CancellationToken.None);

        Assert.Equal("completed", result.Status);
        Assert.Equal(1, result.EventsImported);
        Assert.True(await db.AppSettings.AnyAsync(
            setting => setting.Key == LegacyGoogleSheetsImportService.ImportMarkerKey));
    }

    private sealed class FakeReader(LegacyGoogleSheetsData data)
        : ILegacyGoogleSheetsReader
    {
        public Task<LegacyGoogleSheetsData> ReadAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult(data);
    }

    private sealed class RetryingExecutionStrategyFactory(
        ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create() =>
            new RetryingExecutionStrategy(dependencies);
    }

    private sealed class RetryingExecutionStrategy(
        ExecutionStrategyDependencies dependencies)
        : ExecutionStrategy(dependencies, 1, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception exception) => false;
    }

    private sealed class TestModelContext(
        DbContextOptions<TestModelContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<AppSetting>(entity =>
            {
                entity.HasKey(item => item.Key);
                entity.Property(item => item.Value).HasConversion(
                    document => document.RootElement.GetRawText(),
                    json => JsonDocument.Parse(json, default(JsonDocumentOptions)));
            });
            builder.Entity<Capability>(entity => entity.HasKey(item => item.Key));
            builder.Entity<EventType>(entity => entity.HasKey(item => item.Key));
            builder.Entity<Event>(entity =>
            {
                entity.HasKey(item => item.Id);
                entity.Property(item => item.Metadata).HasConversion(
                    document => document.RootElement.GetRawText(),
                    json => JsonDocument.Parse(json, default(JsonDocumentOptions)));
            });
        }
    }
}
