using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScarletPigsServices.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventAarLookupAttempt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "aar_lookup_last_attempt_at",
                table: "events",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "aar_lookup_last_attempt_at",
                table: "events");
        }
    }
}
