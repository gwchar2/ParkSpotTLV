using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkSpotTLV.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VehicleNameUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parking_rules");

            migrationBuilder.DropColumn(
                name: "parking_hours",
                table: "street_segments");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "vehicles",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "name",
                table: "vehicles");

            migrationBuilder.AddColumn<int>(
                name: "parking_hours",
                table: "street_segments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "parking_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    street_segment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    max_duration_minutes = table.Column<int>(type: "integer", nullable: true, defaultValue: -1),
                    note = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    parking_type = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    style_priority = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parking_rules", x => x.id);
                    table.CheckConstraint("ck_parkingrule_dayofweek_range", "day_of_week BETWEEN 0 AND 6");
                    table.CheckConstraint("ck_parkingrule_time_order", "start_time < end_time");
                    table.ForeignKey(
                        name: "fk_parking_rules_street_segments_street_segment_id",
                        column: x => x.street_segment_id,
                        principalTable: "street_segments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_parking_rules_street_segment_id",
                table: "parking_rules",
                column: "street_segment_id");

            migrationBuilder.CreateIndex(
                name: "ix_parking_rules_style_priority",
                table: "parking_rules",
                column: "style_priority");
        }
    }
}
