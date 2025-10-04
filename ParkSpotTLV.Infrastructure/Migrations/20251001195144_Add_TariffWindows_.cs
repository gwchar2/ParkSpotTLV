using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ParkSpotTLV.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_TariffWindows_ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "street_segment_rule_windows");

            migrationBuilder.DropTable(
                name: "tariff_group_windows");

            migrationBuilder.DropColumn(
                name: "privileged_parking",
                table: "street_segments");

            migrationBuilder.CreateTable(
                name: "tariff_windows",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tariff = table.Column<int>(type: "integer", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    start_local = table.Column<TimeOnly>(type: "time", nullable: false),
                    end_local = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tariff_windows", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tariff_windows_tariff_day_of_week",
                table: "tariff_windows",
                columns: new[] { "tariff", "day_of_week" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tariff_windows");

            migrationBuilder.AddColumn<bool>(
                name: "privileged_parking",
                table: "street_segments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "street_segment_rule_windows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    street_segment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    applies_to_side = table.Column<int>(type: "integer", nullable: false),
                    days = table.Column<int>(type: "integer", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    end_local_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    is_all_day = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    start_local_time = table.Column<TimeOnly>(type: "time", nullable: true)
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
                    days = table.Column<int>(type: "integer", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    end_local_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    is_all_day = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    start_local_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    taarif = table.Column<int>(type: "integer", nullable: false)
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
    }
}
