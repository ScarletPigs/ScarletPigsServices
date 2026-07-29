using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScarletPigsServices.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAddonPlayerTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mission_attendance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    mission_name = table.Column<string>(type: "text", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    session_date = table.Column<DateOnly>(type: "date", nullable: false),
                    steam_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_attendance", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "profile_name_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    profile_name = table.Column<string>(type: "text", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    steam_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile_name_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "steam_dlc_ownership",
                columns: table => new
                {
                    steam_id = table.Column<long>(type: "bigint", nullable: false),
                    dlc_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_steam_dlc_ownership", x => new { x.steam_id, x.dlc_id });
                });

            migrationBuilder.CreateIndex(
                name: "IX_mission_attendance_mission_name",
                table: "mission_attendance",
                column: "mission_name");

            migrationBuilder.CreateIndex(
                name: "IX_mission_attendance_recorded_at",
                table: "mission_attendance",
                column: "recorded_at");

            migrationBuilder.CreateIndex(
                name: "IX_mission_attendance_steam_id_mission_name_session_date",
                table: "mission_attendance",
                columns: new[] { "steam_id", "mission_name", "session_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_profile_name_history_steam_id_recorded_at",
                table: "profile_name_history",
                columns: new[] { "steam_id", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "IX_steam_dlc_ownership_dlc_id",
                table: "steam_dlc_ownership",
                column: "dlc_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mission_attendance");

            migrationBuilder.DropTable(
                name: "profile_name_history");

            migrationBuilder.DropTable(
                name: "steam_dlc_ownership");
        }
    }
}
