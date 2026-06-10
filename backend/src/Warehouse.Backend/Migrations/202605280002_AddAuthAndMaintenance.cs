using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Backend.Migrations;

public partial class AddAuthAndMaintenance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "acknowledged_at",
            table: "alerts",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "acknowledged_by",
            table: "alerts",
            type: "character varying(120)",
            maxLength: 120,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "acknowledgement_note",
            table: "alerts",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

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

        migrationBuilder.CreateIndex(
            name: "ix_maintenance_records_machine_id_created_at",
            table: "maintenance_records",
            columns: new[] { "machine_id", "created_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "maintenance_records");

        migrationBuilder.DropColumn(name: "acknowledged_at", table: "alerts");
        migrationBuilder.DropColumn(name: "acknowledged_by", table: "alerts");
        migrationBuilder.DropColumn(name: "acknowledgement_note", table: "alerts");
    }
}
