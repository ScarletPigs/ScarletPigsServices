using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ScarletPigsServices.Data;
using ScarletPigsServices.Data.Models;

namespace ScarletPigsServices.Api.Services.Imports;

public interface ILegacyGoogleSheetsImportService
{
    Task<LegacyGoogleSheetsImportResult> ImportAsync(CancellationToken cancellationToken);
}

public sealed record LegacyGoogleSheetsImportResult(
    string Status,
    int EventsImported,
    int EventsSkipped,
    int SettingsImported);

public sealed class LegacyGoogleSheetsImportService(
    ScarletPigsDbContext db,
    ILegacyGoogleSheetsReader reader) : ILegacyGoogleSheetsImportService
{
    public const string ImportMarkerKey = "piglet.google_sheets_import";
    public const string DateAmountKey = "piglet.schedule.date_amount";
    public const string ScheduleMessagesKey = "piglet.discord.schedule_messages";
    public const string ModlistMessagesKey = "piglet.discord.modlist_messages";
    public const string QuestionnaireMessageKey = "piglet.discord.questionnaire_message";
    public const string QuestionnaireInfoKey = "piglet.dlc_questionnaire";
    public const string EventTypeKey = "operation";
    public const string EventCapabilityKey = "manage_events";

    public async Task<LegacyGoogleSheetsImportResult> ImportAsync(
        CancellationToken cancellationToken)
    {
        var marker = await db.AppSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(
                setting => setting.Key == ImportMarkerKey,
                cancellationToken);
        if (marker is not null && IsCompleted(marker.Value.RootElement))
        {
            return new LegacyGoogleSheetsImportResult(
                "already_completed", 0, 0, 0);
        }

        var legacy = await reader.ReadAsync(cancellationToken);
        IDbContextTransaction? transaction = null;
        if (db.Database.IsRelational())
        {
            transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            var now = DateTimeOffset.UtcNow;
            await EnsureEventTypeAsync(now, cancellationToken);
            var settingsImported = await ImportSettingsAsync(
                legacy, now, cancellationToken);
            var (eventsImported, eventsSkipped) = await ImportEventsAsync(
                legacy.Operations, now, cancellationToken);

            SetSetting(
                await FindSettingAsync(ImportMarkerKey, cancellationToken),
                ImportMarkerKey,
                new
                {
                    completed = true,
                    completed_at = now,
                    version = 1
                },
                now);
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return new LegacyGoogleSheetsImportResult(
                "completed",
                eventsImported,
                eventsSkipped,
                settingsImported);
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private async Task EnsureEventTypeAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!await db.Capabilities.AnyAsync(
                capability => capability.Key == EventCapabilityKey,
                cancellationToken))
        {
            db.Capabilities.Add(new Capability
            {
                Key = EventCapabilityKey,
                Label = "Manage events",
                Description = "Create and maintain scheduled events."
            });
        }

        if (!await db.EventTypes.AnyAsync(
                eventType => eventType.Key == EventTypeKey,
                cancellationToken))
        {
            db.EventTypes.Add(new EventType
            {
                Key = EventTypeKey,
                CapabilityKey = EventCapabilityKey,
                Label = "Operation",
                FixedDurationMinutes = 180,
                FixedStartMinutes = 900,
                ForceUnlimitedSlots = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
    }

    private async Task<int> ImportSettingsAsync(
        LegacyGoogleSheetsData legacy,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var keys = new[]
        {
            DateAmountKey,
            ScheduleMessagesKey,
            ModlistMessagesKey,
            QuestionnaireMessageKey,
            QuestionnaireInfoKey
        };
        var existing = await db.AppSettings
            .Where(setting => keys.Contains(setting.Key))
            .ToDictionaryAsync(setting => setting.Key, cancellationToken);
        SetSetting(existing.GetValueOrDefault(DateAmountKey), DateAmountKey, legacy.DateAmount, now);
        SetSetting(
            existing.GetValueOrDefault(ScheduleMessagesKey),
            ScheduleMessagesKey,
            legacy.ScheduleMessages,
            now);
        SetSetting(
            existing.GetValueOrDefault(ModlistMessagesKey),
            ModlistMessagesKey,
            legacy.ModlistMessages,
            now);
        SetSetting(
            existing.GetValueOrDefault(QuestionnaireMessageKey),
            QuestionnaireMessageKey,
            legacy.QuestionnaireMessage is { } message ? message : false,
            now);
        SetSetting(
            existing.GetValueOrDefault(QuestionnaireInfoKey),
            QuestionnaireInfoKey,
            legacy.QuestionnaireInfo,
            now);
        return keys.Length;
    }

    private async Task<(int Imported, int Skipped)> ImportEventsAsync(
        IReadOnlyList<LegacyOperation> operations,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existingEvents = await db.Events
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var plan = LegacyGoogleSheetsImportPlanner.CreateEventPlan(
            operations, existingEvents, now);
        db.Events.AddRange(plan.Events);
        return (plan.Events.Count, plan.Skipped);
    }

    private async Task<AppSetting?> FindSettingAsync(
        string key,
        CancellationToken cancellationToken) =>
        await db.AppSettings.FindAsync([key], cancellationToken);

    private void SetSetting(
        AppSetting? setting,
        string key,
        object value,
        DateTimeOffset now)
    {
        var document = JsonSerializer.SerializeToDocument(value);
        if (setting is null)
        {
            db.AppSettings.Add(new AppSetting
            {
                Key = key,
                Value = document,
                UpdatedAt = now
            });
            return;
        }

        setting.Value = document;
        setting.UpdatedAt = now;
    }

    private static bool IsCompleted(JsonElement marker) =>
        marker.ValueKind == JsonValueKind.True
        || (marker.ValueKind == JsonValueKind.Object
            && marker.TryGetProperty("completed", out var completed)
            && completed.ValueKind == JsonValueKind.True);

}

public sealed record LegacyGoogleSheetsEventPlan(
    IReadOnlyList<Event> Events,
    int Skipped);

public static class LegacyGoogleSheetsImportPlanner
{
    private static readonly CultureInfo LegacyDateCulture =
        CultureInfo.GetCultureInfo("en-US");
    private static readonly TimeZoneInfo EventTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

    public static LegacyGoogleSheetsEventPlan CreateEventPlan(
        IReadOnlyList<LegacyOperation> operations,
        IReadOnlyList<Event> existingEvents,
        DateTimeOffset now)
    {
        var allEvents = existingEvents.ToList();
        var events = new List<Event>();
        var skipped = 0;
        foreach (var operation in operations)
        {
            var startsAt = ParseStartsAt(operation.Date);
            var externalId = $"piglet-google-sheets:{startsAt:yyyy-MM-dd}";
            if (allEvents.Any(existing =>
                    existing.ExternalId == externalId
                    || (existing.TypeKey == LegacyGoogleSheetsImportService.EventTypeKey
                        && TimeZoneInfo.ConvertTime(existing.StartsAt, EventTimeZone).Date
                        == startsAt.Date)))
            {
                skipped++;
                continue;
            }

            var entity = new Event
            {
                Name = operation.Name,
                Author = operation.Author,
                StartsAt = startsAt,
                TypeKey = LegacyGoogleSheetsImportService.EventTypeKey,
                DurationMinutes = 180,
                Briefing = string.IsNullOrWhiteSpace(operation.Author)
                    ? string.Empty
                    : $"Op made by {operation.Author}",
                ExternalId = externalId,
                Status = "scheduled",
                Metadata = JsonSerializer.SerializeToDocument(new
                {
                    import_source = "google_sheets",
                    legacy_sheet = operation.Source,
                    legacy_date = operation.Date
                }),
                CreatedAt = now,
                UpdatedAt = now
            };
            events.Add(entity);
            allEvents.Add(entity);
        }
        return new LegacyGoogleSheetsEventPlan(events, skipped);
    }

    private static DateTimeOffset ParseStartsAt(string value)
    {
        if (!DateOnly.TryParseExact(
                value,
                "MMM dd (yy)",
                LegacyDateCulture,
                DateTimeStyles.None,
                out var date))
        {
            throw new InvalidOperationException(
                $"Legacy operation date '{value}' is not in the expected 'MMM dd (yy)' format.");
        }
        var local = DateTime.SpecifyKind(
            date.ToDateTime(new TimeOnly(15, 0)),
            DateTimeKind.Unspecified);
        return new DateTimeOffset(local, EventTimeZone.GetUtcOffset(local));
    }
}
