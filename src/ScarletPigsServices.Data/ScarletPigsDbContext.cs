using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ScarletPigsServices.Data.Models;

namespace ScarletPigsServices.Data;

public sealed class ScarletPigsDbContext(DbContextOptions<ScarletPigsDbContext> options) : DbContext(options)
{
    public DbSet<AdminAuditEntry> AdminAudit => Set<AdminAuditEntry>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<BannerImage> BannerImages => Set<BannerImage>();
    public DbSet<Capability> Capabilities => Set<Capability>();
    public DbSet<DiscordRole> DiscordRoles => Set<DiscordRole>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<HighlightVideo> HighlightVideos => Set<HighlightVideo>();
    public DbSet<MissionAttendance> MissionAttendance => Set<MissionAttendance>();
    public DbSet<MissionUpload> MissionUploads => Set<MissionUpload>();
    public DbSet<ModListMod> ModListMods => Set<ModListMod>();
    public DbSet<ModList> ModLists => Set<ModList>();
    public DbSet<Mod> Mods => Set<Mod>();
    public DbSet<ProfileNameHistory> ProfileNameHistory => Set<ProfileNameHistory>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<RoleCapability> RoleCapabilities => Set<RoleCapability>();
    public DbSet<RoleOverride> RoleOverrides => Set<RoleOverride>();
    public DbSet<SteamDlcOwnership> SteamDlcOwnership => Set<SteamDlcOwnership>();
    public DbSet<UserCapabilityOverride> UserCapabilityOverrides => Set<UserCapabilityOverride>();
    public DbSet<UserDiscordRole> UserDiscordRoles => Set<UserDiscordRole>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasPostgresEnum<ModSide>(name: "mod_side");
        builder.HasPostgresEnum<OverrideMode>(name: "override_mode");

        builder.Entity<AdminAuditEntry>(entity =>
        {
            entity.ToTable("admin_audit");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(item => item.Action).IsRequired();
            entity.Property(item => item.Detail).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
            entity.Property(item => item.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        builder.Entity<AppSetting>(entity =>
        {
            entity.ToTable("app_settings");
            entity.HasKey(item => item.Key);
            entity.Property(item => item.Key).IsRequired();
            entity.Property(item => item.Value).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
            entity.Property(item => item.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        builder.Entity<BannerImage>(entity =>
        {
            entity.ToTable("banner_images");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(item => item.FileName).HasDefaultValue(string.Empty);
            entity.Property(item => item.Height).HasDefaultValue(0);
            entity.Property(item => item.IsDefault).HasDefaultValue(false);
            entity.Property(item => item.SizeBytes).HasDefaultValue(0L);
            entity.Property(item => item.StoragePath).IsRequired();
            entity.Property(item => item.ThumbPath).IsRequired();
            entity.Property(item => item.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(item => item.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(item => item.Width).HasDefaultValue(0);
        });

        builder.Entity<Capability>(entity =>
        {
            entity.ToTable("capabilities");
            entity.HasKey(item => item.Key);
            entity.Property(item => item.Key).IsRequired();
            entity.Property(item => item.Label).IsRequired();
            entity.Property(item => item.SortOrder).HasDefaultValue(0);
        });

        builder.Entity<DiscordRole>(entity =>
        {
            entity.ToTable("discord_roles");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).IsRequired();
            entity.Property(item => item.Color).HasDefaultValue(0);
            entity.Property(item => item.Name).IsRequired();
            entity.Property(item => item.Position).HasDefaultValue(0);
            entity.Property(item => item.SyncedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        builder.Entity<EventType>(entity =>
        {
            entity.ToTable("event_types");
            entity.HasKey(item => item.Key);
            entity.Property(item => item.Key).IsRequired();
            entity.Property(item => item.CapabilityKey).IsRequired();
            entity.Property(item => item.Color).HasDefaultValue(string.Empty);
            entity.Property(item => item.ForceUnlimitedSlots).HasDefaultValue(false);
            entity.Property(item => item.Label).IsRequired();
            entity.Property(item => item.SortOrder).HasDefaultValue(0);
            entity.Property(item => item.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(item => item.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne<Capability>()
                .WithMany()
                .HasForeignKey(item => item.CapabilityKey)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Event>(entity =>
        {
            entity.ToTable("events");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(item => item.Author).HasDefaultValue(string.Empty);
            entity.Property(item => item.DurationMinutes).HasDefaultValue(0);
            entity.Property(item => item.Metadata).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
            entity.Property(item => item.ModListId).HasColumnName("modlist_id");
            entity.Property(item => item.ModListUrl).HasColumnName("modlist_url");
            entity.Property(item => item.Name).IsRequired();
            entity.Property(item => item.StartsAt).IsRequired();
            entity.Property(item => item.Status).HasDefaultValue(string.Empty);
            entity.Property(item => item.TypeKey).IsRequired();
            entity.Property(item => item.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(item => item.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne<ModList>()
                .WithMany()
                .HasForeignKey(item => item.ModListId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<EventType>()
                .WithMany()
                .HasForeignKey(item => item.TypeKey)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<HighlightVideo>(entity =>
        {
            entity.ToTable("highlight_videos");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(item => item.Title).IsRequired();
            entity.Property(item => item.Url).IsRequired();
            entity.Property(item => item.VideoId).IsRequired();
            entity.Property(item => item.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(item => item.Position).HasDefaultValue(0);
            entity.Property(item => item.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        builder.Entity<MissionAttendance>(entity =>
        {
            entity.ToTable("mission_attendance");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(item => item.MissionName).IsRequired();
            entity.Property(item => item.RecordedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(item => item.SessionDate).IsRequired();
            entity.Property(item => item.SteamId).IsRequired();
            // Repeated posts during one session must collapse to a single row.
            entity.HasIndex(item => new { item.SteamId, item.MissionName, item.SessionDate }).IsUnique();
            entity.HasIndex(item => item.MissionName);
            entity.HasIndex(item => item.RecordedAt);
        });

        builder.Entity<MissionUpload>(entity =>
        {
            entity.ToTable("mission_uploads");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(item => item.FileName).IsRequired();
            entity.Property(item => item.FileSize).HasDefaultValue(0L);
            entity.Property(item => item.TransferStatus).HasDefaultValue(string.Empty);
            entity.Property(item => item.UploadedBy).IsRequired();
            entity.Property(item => item.ValidationStatus).HasDefaultValue(string.Empty);
            entity.Property(item => item.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(item => item.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne<Profile>()
                .WithMany()
                .HasForeignKey(item => item.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ModListMod>(entity =>
        {
            entity.ToTable("modlist_mods");
            entity.HasKey(item => new { item.ModListId, item.SteamId });
            entity.Property(item => item.ModListId).HasColumnName("modlist_id");
            entity.Property(item => item.Position).HasDefaultValue(0);
            entity.HasOne<ModList>()
                .WithMany()
                .HasForeignKey(item => item.ModListId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Mod>()
                .WithMany()
                .HasForeignKey(item => item.SteamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ModList>(entity =>
        {
            entity.ToTable("modlists");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(item => item.CommandLine).HasDefaultValue(string.Empty);
            entity.Property(item => item.DlcAppIds).HasDefaultValueSql("ARRAY[]::text[]");
            entity.Property(item => item.IsPublic).HasDefaultValue(false);
            entity.Property(item => item.Name).IsRequired();
            entity.Property(item => item.Position).HasDefaultValue(0);
            entity.Property(item => item.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(item => item.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        builder.Entity<Mod>(entity =>
        {
            entity.ToTable("mods");
            entity.HasKey(item => item.SteamId);
            entity.Property(item => item.SteamId).IsRequired();
            entity.Property(item => item.CommandLineName).HasDefaultValue(string.Empty);
            entity.Property(item => item.CommandLineNameLocked).HasDefaultValue(false);
            entity.Property(item => item.DisplayName).HasDefaultValue(string.Empty);
            entity.Property(item => item.Side)
                .HasColumnType("mod_side")
                .HasSentinel((ModSide)(-1))
                .HasDefaultValue(ModSide.Both);
            entity.Property(item => item.SizeBytes).HasDefaultValue(0L);
            entity.Property(item => item.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(item => item.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        builder.Entity<Profile>(entity =>
        {
            entity.ToTable("profiles");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(item => item.DiscordId).IsRequired();
            entity.Property(item => item.DiscordUsername).IsRequired();
            entity.Property(item => item.IsBanned).HasDefaultValue(false);
            entity.Property(item => item.IsGuildMember).HasDefaultValue(false);
            entity.Property(item => item.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(item => item.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(item => item.DiscordId).IsUnique();
        });

        builder.Entity<ProfileNameHistory>(entity =>
        {
            entity.ToTable("profile_name_history");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(item => item.ProfileName).IsRequired();
            entity.Property(item => item.RecordedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(item => item.SteamId).IsRequired();
            entity.HasIndex(item => new { item.SteamId, item.RecordedAt });
        });

        builder.Entity<RoleCapability>(entity =>
        {
            entity.ToTable("role_capabilities");
            entity.HasKey(item => new { item.RoleId, item.CapabilityKey });
            entity.HasOne<Capability>()
                .WithMany()
                .HasForeignKey(item => item.CapabilityKey)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RoleOverride>(entity =>
        {
            entity.ToTable("role_overrides");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(item => item.Mode).HasColumnType("override_mode");
            entity.Property(item => item.RoleId).IsRequired();
            entity.Property(item => item.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne<Profile>()
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SteamDlcOwnership>(entity =>
        {
            entity.ToTable("steam_dlc_ownership");
            entity.HasKey(item => new { item.SteamId, item.DlcId });
            entity.Property(item => item.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(item => item.DlcId);
        });

        builder.Entity<UserCapabilityOverride>(entity =>
        {
            entity.ToTable("user_capability_overrides");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(item => item.Mode).HasColumnType("override_mode");
            entity.Property(item => item.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(item => item.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne<Capability>()
                .WithMany()
                .HasForeignKey(item => item.CapabilityKey)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserDiscordRole>(entity =>
        {
            entity.ToTable("user_discord_roles");
            entity.HasKey(item => new { item.UserId, item.RoleId });
            entity.HasOne<Profile>()
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(JsonNamingPolicy.SnakeCaseLower.ConvertName(property.Name));
            }
        }

        // "Modlist" is one word in the frontend/database contract.
        builder.Entity<Event>().Property(item => item.ModListId).HasColumnName("modlist_id");
        builder.Entity<Event>().Property(item => item.ModListUrl).HasColumnName("modlist_url");
        builder.Entity<ModListMod>().Property(item => item.ModListId).HasColumnName("modlist_id");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyTimestamps()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                var createdAt = entry.Properties.FirstOrDefault(property => property.Metadata.Name == "CreatedAt");
                if (createdAt is not null && createdAt.CurrentValue is null)
                {
                    createdAt.CurrentValue = now;
                }
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                var updatedAt = entry.Properties.FirstOrDefault(property => property.Metadata.Name == "UpdatedAt");
                if (updatedAt is not null)
                {
                    updatedAt.CurrentValue = now;
                }
            }
        }
    }
}
