using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScarletPigsServices.Data.Migrations;

[DbContext(typeof(ScarletPigsDbContext))]
[Migration("20260728164000_SeedPigletApiData")]
public partial class SeedPigletApiData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            INSERT INTO capabilities (key, description, label, sort_order)
            VALUES (
                'manage_events',
                'Create and maintain scheduled events.',
                'Manage events',
                0
            )
            ON CONFLICT (key) DO NOTHING;

            INSERT INTO event_types (
                key,
                capability_key,
                color,
                fixed_duration_minutes,
                fixed_start_minutes,
                force_unlimited_slots,
                label,
                sort_order
            )
            VALUES (
                'operation',
                'manage_events',
                '',
                180,
                900,
                TRUE,
                'Operation',
                0
            )
            ON CONFLICT (key) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM event_types
            WHERE key = 'operation'
              AND NOT EXISTS (
                  SELECT 1
                  FROM events
                  WHERE type_key = 'operation'
              );

            DELETE FROM capabilities
            WHERE key = 'manage_events'
              AND NOT EXISTS (
                  SELECT 1
                  FROM event_types
                  WHERE capability_key = 'manage_events'
              );
            """);
    }
}
