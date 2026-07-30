using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddHotPathIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_telemetry_events_timestamp",
                table: "telemetry_events",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_records_alert_id",
                table: "maintenance_records",
                column: "alert_id");

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_records_created_at",
                table: "maintenance_records",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_consumption_aggregates_period_start",
                table: "consumption_aggregates",
                column: "period_start");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_start_time",
                table: "alerts",
                column: "start_time");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_telemetry_events_timestamp",
                table: "telemetry_events");

            migrationBuilder.DropIndex(
                name: "ix_maintenance_records_alert_id",
                table: "maintenance_records");

            migrationBuilder.DropIndex(
                name: "ix_maintenance_records_created_at",
                table: "maintenance_records");

            migrationBuilder.DropIndex(
                name: "ix_consumption_aggregates_period_start",
                table: "consumption_aggregates");

            migrationBuilder.DropIndex(
                name: "ix_alerts_start_time",
                table: "alerts");
        }
    }
}
