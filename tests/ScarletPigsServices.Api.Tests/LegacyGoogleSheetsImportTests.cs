using ScarletPigsServices.Api.Services.Imports;
using ScarletPigsServices.Data.Models;
using Xunit;

namespace ScarletPigsServices.Api.Tests;

public sealed class LegacyGoogleSheetsImportTests
{
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
        Assert.Equal(TimeSpan.FromHours(2), imported.StartsAt.Offset);
    }
}
