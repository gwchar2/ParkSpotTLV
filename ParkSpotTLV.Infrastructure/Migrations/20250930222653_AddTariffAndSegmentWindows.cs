using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkSpotTLV.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTariffAndSegmentWindows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "street_segment_rule_windows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    street_segment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    days = table.Column<int>(type: "integer", nullable: false),
                    is_all_day = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    start_local_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    end_local_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    applies_to_side = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_street_segment_rule_windows", x => x.id);
                    table.ForeignKey(
                        name: "fk_street_segment_rule_windows_street_segments_street_segment_",
                        column: x => x.street_segment_id,
                        principalTable: "street_segments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tariff_group_windows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    taarif = table.Column<int>(type: "integer", nullable: false),
                    days = table.Column<int>(type: "integer", nullable: false),
                    is_all_day = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    start_local_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    end_local_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tariff_group_windows", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_street_segment_rule_windows_street_segment_id_enabled_prior",
                table: "street_segment_rule_windows",
                columns: new[] { "street_segment_id", "enabled", "priority" });

            migrationBuilder.CreateIndex(
                name: "ix_tariff_group_windows_taarif_enabled_priority",
                table: "tariff_group_windows",
                columns: new[] { "taarif", "enabled", "priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "street_segment_rule_windows");

            migrationBuilder.DropTable(
                name: "tariff_group_windows");
        }
    }
}
