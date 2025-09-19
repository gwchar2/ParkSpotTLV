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
                    ZonePermit = table.Column<int>(type: "integer", nullable: false),
                    Geom = table.Column<MultiPolygon>(type: "geometry(MultiPolygon,4326)", nullable: false)
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
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    HasDisabledPermit = table.Column<bool>(type: "boolean", nullable: false),
                    ZonePermit = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicles", x => x.Id);
                    table.CheckConstraint("ck_vehicle_zone_permit", "\"ZonePermit\" >= 0 AND \"ZonePermit\" <= 10");
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
                    Geom = table.Column<MultiLineString>(type: "geometry(MultiLineString,4326)", nullable: false),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: true),
                    CarsOnly = table.Column<bool>(type: "boolean", nullable: false),
                    ParkingType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ParkingHours = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "gist_segments_geometry",
                table: "street_segments",
                column: "Geom")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_segments_zone_id",
                table: "street_segments",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_street_segments_CarsOnly",
                table: "street_segments",
                column: "CarsOnly");

            migrationBuilder.CreateIndex(
                name: "ux_users_username",
                table: "users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_owner_id",
                table: "vehicles",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "gist_zones_geometry",
                table: "zones",
                column: "Geom")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ux_zones_zone_permit",
                table: "zones",
                column: "ZonePermit",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
