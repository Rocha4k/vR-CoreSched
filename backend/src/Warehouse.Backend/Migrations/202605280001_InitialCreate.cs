using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Backend.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "floorplans",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                canvas_width = table.Column<int>(type: "integer", nullable: false),
                canvas_height = table.Column<int>(type: "integer", nullable: false),
                texture_key = table.Column<string>(type: "text", nullable: false),
                boundary_points_json = table.Column<string>(type: "text", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_floorplans", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "machines",
            columns: table => new
            {
                machine_id = table.Column<string>(type: "text", nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                zone_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                is_online = table.Column<bool>(type: "boolean", nullable: false),
                last_seen = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                temperature_c = table.Column<decimal>(type: "numeric", nullable: false),
                vibration_ms2 = table.Column<decimal>(type: "numeric", nullable: false),
                rpm = table.Column<int>(type: "integer", nullable: false),
                energy_kwh = table.Column<decimal>(type: "numeric", nullable: false),
                severity = table.Column<string>(type: "text", nullable: false),
                location_x = table.Column<decimal>(type: "numeric", nullable: false),
                location_y = table.Column<decimal>(type: "numeric", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_machines", x => x.machine_id);
            });

        migrationBuilder.CreateTable(
            name: "zones",
            columns: table => new
            {
                zone_id = table.Column<string>(type: "text", nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                description = table.Column<string>(type: "text", nullable: false),
                color = table.Column<string>(type: "text", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_zones", x => x.zone_id);
            });

        migrationBuilder.CreateTable(
            name: "lighting_devices",
            columns: table => new
            {
                device_id = table.Column<string>(type: "text", nullable: false),
                zone_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                is_on = table.Column<bool>(type: "boolean", nullable: false),
                last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                last_command_source = table.Column<string>(type: "text", nullable: false),
                location_x = table.Column<decimal>(type: "numeric", nullable: false),
                location_y = table.Column<decimal>(type: "numeric", nullable: false),
                is_visible = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_lighting_devices", x => x.device_id);
            });

        migrationBuilder.CreateTable(
            name: "rules",
            columns: table => new
            {
                id = table.Column<string>(type: "text", nullable: false),
                code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                target_type = table.Column<string>(type: "text", nullable: false),
                target_id = table.Column<string>(type: "text", nullable: true),
                severity = table.Column<string>(type: "text", nullable: false),
                temperature_threshold = table.Column<decimal>(type: "numeric", nullable: false),
                vibration_threshold = table.Column<decimal>(type: "numeric", nullable: false),
                duration_seconds = table.Column<int>(type: "integer", nullable: false),
                cooldown_seconds = table.Column<int>(type: "integer", nullable: false),
                is_enabled = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_rules", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "telemetry_events",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                machine_id = table.Column<string>(type: "text", nullable: false),
                timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                temperature_c = table.Column<decimal>(type: "numeric", nullable: false),
                vibration_ms2 = table.Column<decimal>(type: "numeric", nullable: false),
                rpm = table.Column<int>(type: "integer", nullable: false),
                energy_kwh = table.Column<decimal>(type: "numeric", nullable: false),
                source = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_telemetry_events", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "alerts",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                machine_id = table.Column<string>(type: "text", nullable: false),
                severity = table.Column<string>(type: "text", nullable: false),
                rule_code = table.Column<string>(type: "text", nullable: false),
                message = table.Column<string>(type: "text", nullable: false),
                start_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                end_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                is_acknowledged = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_alerts", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "consumption_aggregates",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                scope_type = table.Column<string>(type: "text", nullable: false),
                scope_id = table.Column<string>(type: "text", nullable: false),
                period_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                period_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                average_kwh = table.Column<decimal>(type: "numeric", nullable: false),
                total_kwh = table.Column<decimal>(type: "numeric", nullable: false),
                cost_euro = table.Column<decimal>(type: "numeric", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_consumption_aggregates", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "floorplan_pins",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false),
                floorplan_layout_id = table.Column<int>(type: "integer", nullable: false),
                device_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                device_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                x = table.Column<decimal>(type: "numeric", nullable: false),
                y = table.Column<decimal>(type: "numeric", nullable: false),
                is_visible = table.Column<bool>(type: "boolean", nullable: false),
                zone_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_floorplan_pins", x => x.id);
                table.ForeignKey(
                    name: "fk_floorplan_pins_floorplans_floorplan_layout_id",
                    column: x => x.floorplan_layout_id,
                    principalTable: "floorplans",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_alerts_machine_id_start_time",
            table: "alerts",
            columns: new[] { "machine_id", "start_time" });

        migrationBuilder.CreateIndex(
            name: "ix_consumption_aggregates_scope_type_scope_id_period_start",
            table: "consumption_aggregates",
            columns: new[] { "scope_type", "scope_id", "period_start" });

        migrationBuilder.CreateIndex(
            name: "ix_floorplan_pins_floorplan_layout_id",
            table: "floorplan_pins",
            column: "floorplan_layout_id");

        migrationBuilder.CreateIndex(
            name: "ix_telemetry_events_machine_id_timestamp",
            table: "telemetry_events",
            columns: new[] { "machine_id", "timestamp" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "alerts");
        migrationBuilder.DropTable(name: "consumption_aggregates");
        migrationBuilder.DropTable(name: "floorplan_pins");
        migrationBuilder.DropTable(name: "lighting_devices");
        migrationBuilder.DropTable(name: "machines");
        migrationBuilder.DropTable(name: "rules");
        migrationBuilder.DropTable(name: "telemetry_events");
        migrationBuilder.DropTable(name: "zones");
        migrationBuilder.DropTable(name: "floorplans");
    }
}
