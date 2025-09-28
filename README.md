# ParkSpotTLV

<!-- Status & releases -->
[![Build](https://img.shields.io/github/actions/workflow/status/gwchar2/ParkSpotTLV/ci.yml?branch=main&label=Build)](https://github.com/gwchar2/ParkSpotTLV/actions/workflows/ci.yml)
[![Latest release](https://img.shields.io/github/v/release/gwchar2/ParkSpotTLV?include_prereleases&sort=semver)](https://github.com/gwchar2/ParkSpotTLV/releases)
[![Release date](https://img.shields.io/github/release-date-pre/gwchar2/ParkSpotTLV)](https://github.com/gwchar2/ParkSpotTLV/releases/latest)


[Changelog](./CHANGELOG.md)
Use special commit messages to properly decide the next version & store logs:
```git
feat: → minor feature
fix: → patch fix
feat! or BREAKING CHANGE: → major
```

Workflow Example:
```git
git commit -m "feat(db): "Added feature X"
git push
git commit -m "fix(db): "Fixed bug in feature Y"
git push
```

Final output on CHANGELOG.MD after Pull Request:
```git
Features:
db: Added feature X (link_to_commit)
Bug Fixes
db: Fixed bug in feature Y (link_to_commit)
```

## 📑 Table of Contents

- [Backend](#backend)
  - [Running the database](#running-the-database)
    - [Option A - Quick start (DB + API in Docker)](#option-a--docker-db--api)
    - [Option B — DB in Docker, API from source](#option-b--docker-db--api-from-source)
  - [Database management](#database-management)
  - [Port reference](#api-ui)
- [Mobile App](#mobile-app)
- [Docs](#docs)
- [GitHub Actions](#github-actions)
- [Project Structure](#project-structure)

---
# Backend

Run all commands from the repository root unless noted otherwise.

## Option A - Quick start (DB + API in Docker)

- Start the stack
```bash
docker compose up -d --build
```
- Check readiness
```bash
curl http://localhost:8080/ready
```
- Expected: an OK/green response

If readiness is not OK:
- Make sure EF tools on your host point to the DB on port 5433
```powershell
# PowerShell (Windows)
$env:ConnectionStrings__DefaultConnection = 'Host=localhost;Port=5433;Database=parkspot_dev;Username=admin;Password=admin'
```
```bash
# Bash (macOS/Linux)
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5433;Database=parkspot_dev;Username=admin;Password=admin'
```
- Apply migrations
```bash
dotnet ef database update --project ./ParkSpotTLV.Infrastructure --startup-project ./ParkSpotTLV.Api
```
- Restart the API to run the seeder
```bash
docker compose restart api
```
- Recheck readiness
```bash
curl http://localhost:8080/ready
```

Notes:
- Inside Docker, the API connects to Postgres at `Host=parkspot_db;Port=5432`.
- From your host, tools connect at `Host=localhost;Port=5433`.

## Option B — DB in Docker, API from source

- Start only the database
```bash
docker compose up -d db
```
- Point your local run to the DB on host port 5433
```powershell
# Windows
$env:ConnectionStrings__DefaultConnection = 'Host=localhost;Port=5433;Database=parkspot_dev;Username=admin;Password=admin'
```
```bash
# macOS/Linux
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5433;Database=parkspot_dev;Username=admin;Password=admin'
```
- Apply migrations
```bash
dotnet ef database update --project ./ParkSpotTLV.Infrastructure --startup-project ./ParkSpotTLV.Api
```
- Run the API from source
```bash
dotnet run --project ./ParkSpotTLV.Api
```
- Browse http://localhost:8080

## Database management

- See which secrets the API uses locally
```bash
dotnet user-secrets list --project ./ParkSpotTLV.Api
```

- Completely new table
```
Delete the contents of Migrations/ folder in ParkSpotTLV.Infrastructure
docker compose down -v --remove-orphans
docker compose up -d db
dotnet ef migrations add InitialCreate -p ./ParkSpotTLV.Infrastructure -s ./ParkSpotTLV.Api
dotnet ef database update -p ./ParkSpotTLV.Infrastructure -s ./ParkSpotTLV.Api
F5
Verify
```

- New tables OR table changes
```bash
docker start parkspot_db or docker compose up -d db
dotnet ef migrations add UpdateSeed_20250927(or some other name) -p ./ParkSpotTLV.Infrastructure -s ./ParkSpotTLV.Api
dotnet ef database update --project .\ParkSpotTLV.Infrastructure --startup-project .\ParkSpotTLV.Api
Restart DB+API
Verify
```
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI5ZTg3YTJiNi1hNTMwLTRiZTAtYTdlZS1lMTk0NWViODI5MTMiLCJuYW1lIjoidGVzdDEiLCJqdGkiOiJjZTVhZjczMC00MTcwLTRkZTItOTYwNy1jYWU0ZGZhZmYzZjAiLCJpYXQiOjE3NTkwOTM0NDMsIm5iZiI6MTc1OTA5MzQ0MywiZXhwIjoxNzU5MDk0MDQzLCJpc3MiOiJQYXJrU3BvdFRMViIsImF1ZCI6IlBhcmtTcG90VExWLkFwcCJ9.JOrSo5a7jUkxD0GV67GuQWeIApTD4Wj4Hqq2EZ6Cjck",
  "accessTokenExpiresAt": "2025-09-28T21:14:03.4221808+00:00",
  "refreshToken": "yQ0FC0bQR-Ui0jDBlTFN_RIRttF1r5RNSil6x4FbMgM",
  "refreshTokenExpiresAt": "2025-10-12T21:04:03.3397706+00:00",
  "tokenType": "Bearer"
}
- Updating information in the DB
```bash
docker start parkspot_db
docker exec -it parkspot_db psql -U admin -d parkspot_dev  -c "TRUNCATE TABLE street_segments, zones RESTART IDENTITY CASCADE;"
dotnet ef database update --project .\ParkSpotTLV.Infrastructure --startup-project .\ParkSpotTLV.Api
Restart DB+API
Verify
```

- Inspect the DB inside the container
```bash
docker exec -it parkspot_db psql -U admin -d parkspot_dev
SELECT * FROM "__EFMigrationsHistory";
SELECT COUNT(*) AS zones FROM zones;
SELECT COUNT(*) AS segments FROM street_segments;
SELECT id, code, name FROM zones ORDER BY code LIMIT 10;
SELECT DISTINCT ON (z.code)
       z.code        AS zone_code,
       s.id          AS segment_id,
       s.name_english
FROM street_segments s
JOIN zones z ON s.zone_id = z.id
ORDER BY z.code, s.name_english NULLS LAST;

```

## Port reference

- Host to DB: `localhost:5433`
- API container to DB container: `parkspot_db:5432`



# Mobile App

# Docs

# GitHub Actions

This project includes multiple GitHub Actions workflows under .github/workflows:

ci.yml — builds the solution and runs tests on every push or pull request.

release.yml — automates semantic-release to create GitHub releases based on commit messages.

attach-apk-on-release.yml — builds the MAUI Android app and attaches the generated APK file to each release.

Each workflow runs automatically when triggered, but you can also start them manually from the GitHub Actions tab if needed.


# Project Structure

```
ParkSpotTLV/
├── .github/workflows/				# CI/CD pipelines (build, release, APK attach)
│   ├── ci.yml
│   ├── release.yml
│   └── attach-apk-on-release.yml
│
├── BackEnd/
│   ├── ParkSpotTLV.Api/			# ASP.NET Core minimal API host
│   │   ├── Endpoints/				# Endpoints
│   │   ├── Errors/					# Error middleware
│   │   ├── Http/					# Middleware & Request & Response log configurations
│   │   └── Logs/					# Logs
│   │
│   ├── ParkSpotTLV.Infrastructure/
│   │   ├── Config/					# EF Core entity configurations (Fluent API)
│   │   ├── db/Seed/				# GeoJSON + JSON seed data
│   │   ├── Entities/				# Domain entities (User, Vehicle, Zone, StreetSegment)
│   │   ├── Migrations/				# EF migrations
│   │   ├── Seeding/				# Seeder logic
│   │   ├── AppDbContext.cs			# EF Core DbContext
│   │   └── AppDbContextFactory.cs # Factory for design-time tools
│   │
│   └── ParkSpotTLV.Core/			# Core domain logic (no infra)
│       ├── Dependencies/			# DI helpers
│       ├── Models/					# Shared domain models (Car, etc.)
│       └── Services/				# Core services (Auth, CarService)
│
├── MobileApp/
│   ├── ParkSpotTLV.App/			# .NET MAUI client app
│   │   ├── Pages/					# XAML pages (Login, SignUp, Cars, Map, etc.)
│   │   ├── Resources/				# Fonts, Images, Styles
│   │   ├── Services/				# Client-side services (Auth, API calls)
│   │   ├── App.xaml				# App bootstrap & shell
│   │   ├── AppShell.xaml			# Navigation structure
│   │   └── MauiProgram.cs			# MAUI DI container
│
├── Tests/							# Placeholder for backend tests
│   └── (currently minimal)
│
├── docker-compose/					# Docker configs for local DB/API
│   ├── docker-compose.yml
│   ├── docker-compose.override.yml
│   └── .release-please-*.json		# Release automation configs
│
└── Docs/							# Project documentation
    ├── CHANGELOG.md
    ├── api-reference.md			# (to be added)
    └── git-workflow.md
```
