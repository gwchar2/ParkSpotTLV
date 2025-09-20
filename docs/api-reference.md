# ParkSpotTLV – API Reference & Backend Guide

A single, developer-focused reference for the ParkSpotTLV backend: how to run it, the HTTP endpoints ("commands"), structured logging of requests/errors, and how we talk to the Postgres/PostGIS database.

---

## 1) Quick start

- **Prereqs**
  - .NET SDK 9.0+
  - Docker Desktop (for local DB)
  - Node/Android tooling only required for the mobile app (not for API)
- **Start the stack (DB + API)**
  - Docker only: `docker compose up -d` `docker compose logs -f api`
  - API only: `dotnet run -p ParkSpotTLV.Api`
- **Migrate DB**
  - Add migration: `dotnet ef migrations add Init --project ParkSpotTLV.Infrastructure --startup-project ParkSpotTLV.Api`
  - Apply: `dotnet ef database update --project ParkSpotTLV.Infrastructure --startup-project ParkSpotTLV.Api`
- **Open API docs**
  - Scalar UI (OpenAPI): 
    If without Docker: `https://localhost:7164/scalar` 
    If with Docker: `https://localhost:8080/scalar`
- **Logs in Seq**
  - Seq URL: `http://localhost:5341/` → live requests/errors and filters (see §4)

---

## 2) Environments & configuration

- **appsettings.Development.json** – local defaults, verbose logging, Seq enabled
- **appsettings.json** – shared defaults
- **Environment variables**
  - `ConnectionStrings__Default` – Postgres connection string
    If connection is not visible in files, use: `dotnet user-secrets list --project ParkSpotTLV.Api/ParkSpotTLV.Api.csproj`
  - `Serilog__WriteTo__1__Args__serverUrl` – Seq server URL (if used)
  - `ASPNETCORE_ENVIRONMENT` – `Development` | `Staging` | `Production`

---

## 3) API surface (HTTP “commands”)

> Paths and contract names reflect the current project layout. 

### Health
- **GET** `/health`  
  - Returns `200 OK` when API is up (liveness).
  - **Name**: `Health`.
  - **Response**: `{ status: "ok", version: "<semver>", uptimeSec: <number>, nowUtc: "<iso>" }`
  
### Ready
- **GET** `/ready`  
  - Readiness probe: checks DB connectivity and **PostGIS** extension.
  - **200 OK** when DB reachable **and** PostGIS present; otherwise **503** with body describing which check failed.

### Version
- **GET** `/version`  
  - Returns `{ version: "<semver>" }` — same build version used by `/health`.

### Zones
- **GET** `/zones`  
  - Returns a lightweight list of parking zones.  
- **GET** `/zones/{id}`  
  - Single zone (polygon/multipolygon).

### Street Segments
- **GET** `/street-segments`  
  - Example: `/street-segments?bbox=34.7706,32.0650,34.7820,32.0740&parkingType=free`  
- **GET** `/street-segments/{id}`  
  - Returns one segment, including attributes: `parking_type`, `hours`, etc.

### Vehicles
- **GET** `/users/{userId}/vehicles`  
- **POST** `/users/{userId}/vehicles`  
  - Body: `{ "plate": "12-345-67", "brand": "…" }`
- **DELETE** `/users/{userId}/vehicles/{vehicleId}`

### Users (Not yet implemented)
- **POST** `/users/register`  
  - Body: `{ "username": "...", "password": "..." }`
- **POST** `/users/login`  
  - Body: `{ "username": "...", "password": "..." }`  
  - Returns token (if/when auth is wired).

### Examples (curl)
- Health:  
  `curl -k https://localhost:7164/health`
- Segments in bbox:  
  `curl -k "https://localhost:7164/street-segments?bbox=34.7706,32.0650,34.7820,32.0740&limit=50"`
- Create vehicle:  
  `curl -k -X POST https://localhost:7164/users/{userId}/vehicles -H "Content-Type: application/json" -d '{"plate":"12-345-67","brand":"Toyota"}'`

---

## 4) Request, trace & error logging (Serilog + Seq)

### What we log
- **HTTP request/response log**
  - Method, Path, StatusCode, ElapsedMs, ResponseLength
  - RequestId/TraceId, ClientIP (when available)
  - UserId (if authenticated), RouteName (e.g., `Health`)
  - Request body and response body are **summarized** by default; full bodies are opt-in to avoid PII/log bloat
- **W3C access log**  – standardized fields for proxy/debugging
- **Errors**
  - Unhandled exceptions → logged at **Error** with stack trace and correlation IDs
  - Known domain/validation errors → logged at **Information/Warning** with a structured `error.code` and `error.details`

### Middlewares & correlation
- **TracingMiddleware** – attaches `TraceId`/`SpanId` (or reuses incoming `traceparent`), enriches Serilog scope
- **RequestLoggingMiddleware** – one log line per request with key dimensions
- **Exception handler** – problem-details JSON for errors

### Viewing logs in Seq
- URL: `http://localhost:5341/#/events?range=1h`
- Suggested signals/queries:  
  - `@Level = 'Error'`  
  - `StatusCode >= 500`  
  - `Path like '/street-segments%'`  
  - `TraceId = '...'` to follow a single request

### Sample log (structured)
```json
{
  "@t": "2025-09-18T09:42:22.245Z",
  "@l": "Information",
  "MessageTemplate": "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.000} ms",
  "RequestMethod": "GET",
  "RequestPath": "/street-segments",
  "StatusCode": 200,
  "Elapsed": 23.7,
  "TraceId": "00-5c1f...-01",
  "RouteName": "StreetSegmentsList",
  "UserId": null
}
```

---

## 5) Error model (Problem Details)

- **Content-Type**: `application/problem+json`
- **Shape**
```json
{
  "type": "https://parkspottlv/errors/validation-failed",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "traceId": "00-5c1f...-01",
  "errors": { "bbox": ["Invalid coordinate order."] }
}
```
- **Common codes**
  - `validation-failed` – bad input (e.g., bbox format)
  - `not-found` – missing entity id
  - `conflict` – duplicate resource

---

## 6) Database integration (EF Core + PostGIS)

### Stack
- **Postgres** with **PostGIS** extension for geospatial support
- **EF Core** with **Npgsql** and **NetTopologySuite** for mapping `Point`, `LineString`, `Polygon`

### Entities (current high-level)
- **User** – `Id`, `Username`, `PasswordHash`, `Vehicles[]`
- **Vehicle** – `Id`, `OwnerId`, `Owner`, `Type`, `PlateNumber`, `Permits[]`
- **Permit** - `Id`, `VehicleId`, `Vehicle`, `Type`, `ZoneId`, `Zone`, `ValidTo`, `IsActive`
- **Zone** – `Id`, `Code`, `Name`, `Geom (MultiPolygon, SRID 4326)`, `LastUpdated`, `Segments[]`
- **StreetSegment** – `Id`, `Name`, `Geom (LineString, SRID 4326)`, `ZoneId`, `Zone`, `CarsOnly`, `ParkingType`, `ParkingHours`

### Schema & spatial indexes
- SRID: **4326** (WGS84) for all geometries
- Indexes: GIST on `Geom` for `Zone` and `StreetSegment`

### Migrations
- Create migration:  
  `dotnet ef migrations add AddStreetSegments --project ParkSpotTLV.Infrastructure --startup-project ParkSpotTLV.Api`
- Apply:  
  `dotnet ef database update --project ParkSpotTLV.Infrastructure --startup-project ParkSpotTLV.Api`

### Seeding (local)
- Minimal `Zone` + ~50 `StreetSegment` examples are loaded by an idempotent seeder (runs on empty DB).  
- Seed files live under `db/Seed/` (e.g., GeoJSON).  
- Loader reads files from repo paths and inserts into DB running in Docker.

### Typical queries
- **BBOX filter** (SQL):
```sql
SELECT id, parking_type
FROM street_segments
WHERE ST_Intersects(
  geom,
  ST_MakeEnvelope(:west, :south, :east, :north, 4326)
);
```
- **EF LINQ**
```csharp
var bbox = NtsGeometryServices.Instance.CreateGeometryFactory(4326)
  .ToGeometry(new Envelope(west, east, south, north));
var q = db.StreetSegments
  .Where(s => s.Geom.Intersects(bbox));
```

### Transactions & reliability
- Short, single-request transactions for write endpoints
- Command timeout ~30s (adjust per env)
- Retries for transient Npgsql errors (policy-based, if configured)

---

## 7) OpenAPI/Scalar

- Served at `/scalar` with the generated OpenAPI document
- Use it to try endpoints, inspect schemas, and copy curl snippets
- Not a log viewer (use Seq for live logs)

---

## 8) Versioning & compatibility

- Semantic versioning for the API package/release tags (`vMAJOR.MINOR.PATCH[-pre]`)
- Backwards-compatible changes add fields/endpoints; breaking changes bump **MAJOR** and deprecate old routes

---

## 9) Troubleshooting

- **"No project was found" with `dotnet ef`** – run from repo root or pass `--project` and `--startup-project`
- **Android SDK/MAUI build errors** – unrelated to API; ensure mobile tooling is installed or build only the API
- **DB starts empty after compose up** – re-run migrations, confirm volume mount, ensure seeder path is correct
- **Seq not showing logs** – verify `Serilog:WriteTo` with the Seq sink and `serverUrl` are set; check firewall/port 5341

---

## 10) Changelog (backend)

- Day 2 – Logging & tracing: Serilog + Request/Response logging, W3C log, Seq dashboard, Scalar UI wired
- Day 3 – Database wiring: Postgres + PostGIS, EF Core context/migrations, GIST indexes, seed of zones & segments

---

### Appendix – Example appsettings (snippets)

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=parkspot;Username=postgres;Password=postgres"
  },
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.Seq" ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": { "Microsoft": "Warning", "System": "Warning" }
    },
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "Seq", "Args": { "serverUrl": "http://localhost:5341" } }
    ],
    "Enrich": [ "FromLogContext" ]
  }
}
```

