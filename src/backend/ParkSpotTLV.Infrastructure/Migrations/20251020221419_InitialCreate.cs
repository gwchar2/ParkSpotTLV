using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                name: "parking_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    segment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    reason = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    parking_type = table.Column<int>(type: "integer", nullable: false),
                    zone_code = table.Column<int>(type: "integer", nullable: true),
                    tariff = table.Column<int>(type: "integer", nullable: false),
                    is_pay_now = table.Column<bool>(type: "boolean", nullable: false),
                    is_pay_later = table.Column<bool>(type: "boolean", nullable: false),
                    next_change_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    started_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    stopped_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    planned_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    parking_budget_used = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    paid_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parking_sessions", x => x.id);
                    table.CheckConstraint("ck_parking_sessions_paid_minutes", "\"paid_minutes\" >= 0");
                    table.CheckConstraint("ck_parking_sessions_parking_budget_used", "\"parking_budget_used\" >= 0");
                });

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

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "zones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<int>(type: "integer", nullable: false),
                    taarif = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    geom = table.Column<MultiPolygon>(type: "geometry(MultiPolygon,4326)", nullable: false),
                    last_updated_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_zones", x => x.id);
                    table.UniqueConstraint("ak_zones_code", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicles", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicles_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "street_segments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    geom = table.Column<LineString>(type: "geometry(LineString,4326)", nullable: false),
                    osm_id = table.Column<string>(type: "text", nullable: false),
                    name_english = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    name_hebrew = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    zone_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parking_type = table.Column<int>(type: "integer", nullable: false),
                    side = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_street_segments", x => x.id);
                    table.ForeignKey(
                        name: "fk_street_segments_zones_zone_id",
                        column: x => x.zone_id,
                        principalTable: "zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "daily_budgets",
                columns: table => new
                {
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    anchor_date = table.Column<DateOnly>(type: "date", nullable: false),
                    minutes_used = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_daily_budgets", x => new { x.vehicle_id, x.anchor_date });
                    table.ForeignKey(
                        name: "fk_daily_budgets_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "permits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    zone_code = table.Column<int>(type: "integer", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    last_updated_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permits", x => x.id);
                    table.ForeignKey(
                        name: "fk_permits_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_permits_zones_zone_code",
                        column: x => x.zone_code,
                        principalTable: "zones",
                        principalColumn: "code",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_ps_due",
                table: "parking_sessions",
                column: "planned_end_utc",
                filter: "\"stopped_utc\" IS NULL AND \"status\" = 1");

            migrationBuilder.CreateIndex(
                name: "ix_parking_sessions_started_utc",
                table: "parking_sessions",
                column: "started_utc");

            migrationBuilder.CreateIndex(
                name: "IX_parking_sessions_vehicle_active",
                table: "parking_sessions",
                column: "vehicle_id",
                unique: true,
                filter: "\"stopped_utc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_parking_sessions_vehicle_id_status",
                table: "parking_sessions",
                columns: new[] { "vehicle_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_permits_vehicle_id",
                table: "permits",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ix_permits_zone_code",
                table: "permits",
                column: "zone_code");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_expires_at_utc",
                table: "refresh_tokens",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_replaced_by_token_hash",
                table: "refresh_tokens",
                column: "replaced_by_token_hash");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_revoked_at_utc",
                table: "refresh_tokens",
                column: "revoked_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_street_segments_geom",
                table: "street_segments",
                column: "geom")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "ix_street_segments_osm_id",
                table: "street_segments",
                column: "osm_id");

            migrationBuilder.CreateIndex(
                name: "ix_street_segments_zone_id",
                table: "street_segments",
                column: "zone_id");

            migrationBuilder.CreateIndex(
                name: "ix_tariff_windows_tariff_day_of_week",
                table: "tariff_windows",
                columns: new[] { "tariff", "day_of_week" });

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_owner_id",
                table: "vehicles",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_owner_id_name",
                table: "vehicles",
                columns: new[] { "owner_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_zones_code",
                table: "zones",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_zones_geom",
                table: "zones",
                column: "geom")
                .Annotation("Npgsql:IndexMethod", "GIST");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_budgets");

            migrationBuilder.DropTable(
                name: "parking_sessions");

            migrationBuilder.DropTable(
                name: "permits");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "street_segments");

            migrationBuilder.DropTable(
                name: "tariff_windows");

            migrationBuilder.DropTable(
                name: "vehicles");

            migrationBuilder.DropTable(
                name: "zones");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
