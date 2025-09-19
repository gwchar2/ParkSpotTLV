using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace ParkSpotTLV.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "zones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Geom = table.Column<MultiPolygon>(type: "geometry(MultiPolygon,4326)", nullable: false),
                    LastUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_zones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    PlateNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vehicles_users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "street_segments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Geom = table.Column<LineString>(type: "geometry(LineString,4326)", nullable: false),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: true),
                    CarsOnly = table.Column<bool>(type: "boolean", nullable: false),
                    ParkingType = table.Column<int>(type: "integer", nullable: false),
                    ParkingHours = table.Column<int>(type: "integer", nullable: false),
                    FromNodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToNodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Side = table.Column<int>(type: "integer", nullable: false),
                    LengthMeters = table.Column<double>(type: "double precision", nullable: true),
                    StylePriority = table.Column<int>(type: "integer", nullable: false),
                    LastUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_street_segments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_street_segments_zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "permits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: true),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_permits_vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_permits_zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "parking_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StreetSegmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ParkingType = table.Column<int>(type: "integer", nullable: false),
                    MaxDurationMinutes = table.Column<int>(type: "integer", nullable: true, defaultValue: -1),
                    Note = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parking_rules", x => x.Id);
                    table.CheckConstraint("ck_parkingrule_dayofweek_range", "\"DayOfWeek\" BETWEEN 0 AND 6");
                    table.CheckConstraint("ck_parkingrule_time_order", "\"StartTime\" < \"EndTime\"");
                    table.ForeignKey(
                        name: "FK_parking_rules_street_segments_StreetSegmentId",
                        column: x => x.StreetSegmentId,
                        principalTable: "street_segments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_parking_rules_StreetSegmentId",
                table: "parking_rules",
                column: "StreetSegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_permits_VehicleId_ZoneId_Type",
                table: "permits",
                columns: new[] { "VehicleId", "ZoneId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_permits_ZoneId",
                table: "permits",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_street_segments_Geom",
                table: "street_segments",
                column: "Geom")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_street_segments_StylePriority",
                table: "street_segments",
                column: "StylePriority");

            migrationBuilder.CreateIndex(
                name: "IX_street_segments_ZoneId",
                table: "street_segments",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Username",
                table: "users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_OwnerId",
                table: "vehicles",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_PlateNumber",
                table: "vehicles",
                column: "PlateNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_zones_Code",
                table: "zones",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_zones_Geom",
                table: "zones",
                column: "Geom")
                .Annotation("Npgsql:IndexMethod", "GIST");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parking_rules");

            migrationBuilder.DropTable(
                name: "permits");

            migrationBuilder.DropTable(
                name: "street_segments");

            migrationBuilder.DropTable(
                name: "vehicles");

            migrationBuilder.DropTable(
                name: "zones");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
