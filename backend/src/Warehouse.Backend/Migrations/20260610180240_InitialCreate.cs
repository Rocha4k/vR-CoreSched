using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    is_acknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    acknowledged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    acknowledged_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    acknowledgement_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alerts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "app_users",
                columns: table => new
                {
                    username = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    full_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    password_salt = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_users", x => x.username);
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
                name: "maintenance_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_id = table.Column<string>(type: "text", nullable: false),
                    alert_id = table.Column<string>(type: "text", nullable: true),
                    title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_maintenance_records", x => x.id);
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
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(120)", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_app_users_username",
                        column: x => x.username,
                        principalTable: "app_users",
                        principalColumn: "username",
                        onDelete: ReferentialAction.Cascade);
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
                name: "ix_maintenance_records_machine_id_created_at",
                table: "maintenance_records",
                columns: new[] { "machine_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_expires_at",
                table: "refresh_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_username_token_hash",
                table: "refresh_tokens",
                columns: new[] { "username", "token_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_telemetry_events_machine_id_timestamp",
                table: "telemetry_events",
                columns: new[] { "machine_id", "timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerts");

            migrationBuilder.DropTable(
                name: "consumption_aggregates");

            migrationBuilder.DropTable(
                name: "floorplan_pins");

            migrationBuilder.DropTable(
                name: "lighting_devices");

            migrationBuilder.DropTable(
                name: "machines");

            migrationBuilder.DropTable(
                name: "maintenance_records");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "rules");

            migrationBuilder.DropTable(
                name: "telemetry_events");

            migrationBuilder.DropTable(
                name: "zones");

            migrationBuilder.DropTable(
                name: "floorplans");

            migrationBuilder.DropTable(
                name: "app_users");
        }
    }
}
