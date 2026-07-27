using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using ScarletPigsServices.Data.Models;

#nullable disable

namespace ScarletPigsServices.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:mod_side", "both,server_only,client_only")
                .Annotation("Npgsql:Enum:override_mode", "grant,revoke");

            migrationBuilder.CreateTable(
                name: "admin_audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    action = table.Column<string>(type: "text", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    detail = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    target_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_audit", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    key = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    value = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_settings", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "banner_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    file_name = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    height = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    storage_path = table.Column<string>(type: "text", nullable: false),
                    thumb_path = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    width = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_banner_images", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "capabilities",
                columns: table => new
                {
                    key = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    label = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_capabilities", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "discord_roles",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    color = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    name = table.Column<string>(type: "text", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discord_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "highlight_videos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    position = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    title = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    url = table.Column<string>(type: "text", nullable: false),
                    video_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_highlight_videos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "modlists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    command_line = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    dlc_app_ids = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "ARRAY[]::text[]"),
                    is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modlists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mods",
                columns: table => new
                {
                    steam_id = table.Column<string>(type: "text", nullable: false),
                    command_line_name = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    command_line_name_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    display_name = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    last_synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    side = table.Column<ModSide>(type: "mod_side", nullable: false, defaultValue: ModSide.Both),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    time_updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mods", x => x.steam_id);
                });

            migrationBuilder.CreateTable(
                name: "profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    discord_id = table.Column<string>(type: "text", nullable: false),
                    discord_username = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    guild_joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_banned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_guild_member = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_role_sync_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_types",
                columns: table => new
                {
                    key = table.Column<string>(type: "text", nullable: false),
                    capability_key = table.Column<string>(type: "text", nullable: false),
                    color = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    fixed_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    fixed_start_minutes = table.Column<int>(type: "integer", nullable: true),
                    force_unlimited_slots = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_types", x => x.key);
                    table.ForeignKey(
                        name: "FK_event_types_capabilities_capability_key",
                        column: x => x.capability_key,
                        principalTable: "capabilities",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_capabilities",
                columns: table => new
                {
                    capability_key = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_capabilities", x => new { x.role_id, x.capability_key });
                    table.ForeignKey(
                        name: "FK_role_capabilities_capabilities_capability_key",
                        column: x => x.capability_key,
                        principalTable: "capabilities",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_capability_overrides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    capability_key = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    mode = table.Column<OverrideMode>(type: "override_mode", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_capability_overrides", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_capability_overrides_capabilities_capability_key",
                        column: x => x.capability_key,
                        principalTable: "capabilities",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "modlist_mods",
                columns: table => new
                {
                    modlist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    steam_id = table.Column<string>(type: "text", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modlist_mods", x => new { x.modlist_id, x.steam_id });
                    table.ForeignKey(
                        name: "FK_modlist_mods_modlists_modlist_id",
                        column: x => x.modlist_id,
                        principalTable: "modlists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_modlist_mods_mods_steam_id",
                        column: x => x.steam_id,
                        principalTable: "mods",
                        principalColumn: "steam_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mission_uploads",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    file_name = table.Column<string>(type: "text", nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    storage_path = table.Column<string>(type: "text", nullable: true),
                    target_path = table.Column<string>(type: "text", nullable: true),
                    transfer_message = table.Column<string>(type: "text", nullable: true),
                    transfer_status = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    validation_message = table.Column<string>(type: "text", nullable: true),
                    validation_status = table.Column<string>(type: "text", nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_uploads", x => x.id);
                    table.ForeignKey(
                        name: "FK_mission_uploads_profiles_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_overrides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    mode = table.Column<OverrideMode>(type: "override_mode", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    role_id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_overrides", x => x.id);
                    table.ForeignKey(
                        name: "FK_role_overrides_profiles_user_id",
                        column: x => x.user_id,
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_discord_roles",
                columns: table => new
                {
                    role_id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_discord_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_user_discord_roles_profiles_user_id",
                        column: x => x.user_id,
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    aar_url = table.Column<string>(type: "text", nullable: true),
                    attendance_count = table.Column<int>(type: "integer", nullable: true),
                    author = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    briefing = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    external_id = table.Column<string>(type: "text", nullable: true),
                    faction = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    modlist_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modlist_url = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    slots = table.Column<int>(type: "integer", nullable: true),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    type_key = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_events_event_types_type_key",
                        column: x => x.type_key,
                        principalTable: "event_types",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_events_modlists_modlist_id",
                        column: x => x.modlist_id,
                        principalTable: "modlists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_event_types_capability_key",
                table: "event_types",
                column: "capability_key");

            migrationBuilder.CreateIndex(
                name: "IX_events_modlist_id",
                table: "events",
                column: "modlist_id");

            migrationBuilder.CreateIndex(
                name: "IX_events_type_key",
                table: "events",
                column: "type_key");

            migrationBuilder.CreateIndex(
                name: "IX_mission_uploads_uploaded_by",
                table: "mission_uploads",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "IX_modlist_mods_steam_id",
                table: "modlist_mods",
                column: "steam_id");

            migrationBuilder.CreateIndex(
                name: "IX_profiles_discord_id",
                table: "profiles",
                column: "discord_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_capabilities_capability_key",
                table: "role_capabilities",
                column: "capability_key");

            migrationBuilder.CreateIndex(
                name: "IX_role_overrides_user_id",
                table: "role_overrides",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_capability_overrides_capability_key",
                table: "user_capability_overrides",
                column: "capability_key");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_audit");

            migrationBuilder.DropTable(
                name: "app_settings");

            migrationBuilder.DropTable(
                name: "banner_images");

            migrationBuilder.DropTable(
                name: "discord_roles");

            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.DropTable(
                name: "highlight_videos");

            migrationBuilder.DropTable(
                name: "mission_uploads");

            migrationBuilder.DropTable(
                name: "modlist_mods");

            migrationBuilder.DropTable(
                name: "role_capabilities");

            migrationBuilder.DropTable(
                name: "role_overrides");

            migrationBuilder.DropTable(
                name: "user_capability_overrides");

            migrationBuilder.DropTable(
                name: "user_discord_roles");

            migrationBuilder.DropTable(
                name: "event_types");

            migrationBuilder.DropTable(
                name: "modlists");

            migrationBuilder.DropTable(
                name: "mods");

            migrationBuilder.DropTable(
                name: "profiles");

            migrationBuilder.DropTable(
                name: "capabilities");
        }
    }
}
