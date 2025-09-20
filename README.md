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
    - [Option A — Docker (DB + API)](#option-a--docker-db--api)
    - [Option B — Docker DB + API from source](#option-b--docker-db--api-from-source)
  - [Database management](#database-management)
  - [API UI](#api-ui)
- [Mobile App](#mobile-app)
- [Docs](#docs)
- [GitHub Actions](#github-actions)
- [Project Structure](#project-structure)

---


# Backend

## Running the database

## Option A — Docker (DB + API)

```bash
docker compose up -d --build
curl http://localhost:8080/ready     # expect: green
```

Once the containers are up, check readiness at http://localhost:8080/ready
You should see a green response.

If readiness is not green after Docker finishes:

1) Ensure EF Tools know how to reach the DB (host can reach the container on localhost:5432)
```bash
$env:DB_CONNECTION = 'Host=localhost;Port=5432;Database=parkspot_dev;Username=admin;Password=admin'
```

2) Apply migrations
```bash
dotnet ef database update --project .\ParkSpotTLV.Infrastructure --startup-project .\ParkSpotTLV.Api
```

3) Restart API to let the seeder run
```bash
docker compose restart api
```

4) Check readiness
```bash
curl http://localhost:8080/ready
```


## Option B — Docker DB + API from source

Run only the database in Docker with 
```bash
docker compose up -d db
```

Set the connection string environment variable to:
```bash
Host=localhost;Port=5432;Database=parkspot_dev;Username=admin;Password=admin
```

Apply migrations with:
```bash
dotnet ef database update --project ./ParkSpotTLV.Infrastructure --startup-project ./ParkSpotTLV.Api
```

Finally, run the API directly from source with:
```bash
dotnet run --project ./ParkSpotTLV.Api
```

The API will now be available at http://localhost:8080
.

## Database management

To view the current secrets used by the API to connect to the database, run:
```bash
dotnet user-secrets list --project .\ParkSpotTLV.Api
```

To create migrations or update tables, first build the project, then add & apply migrations. 
```bash
dotnet build
dotnet ef migrations add InitialCreate --project ParkSpotTLV.Infrastructure --startup-project ParkSpotTLV.Api
dotnet ef database update --project ParkSpotTLV.Infrastructure --startup-project ParkSpotTLV.Api
```

You can also list existing migrations with:
```bash
dotnet ef migrations list --project ParkSpotTLV.Infrastructure --startup-project ParkSpotTLV.Api
```

To inspect the database directly inside the Docker container, open a psql session with:
```bash
docker exec -it parkspot-db psql -U admin -d parkspot_dev
```

Inside psql you can list all tables with:
```bash
\dt
```

Inside psql you can inspect individual tables with:
```bash
\d+ users
\d+ vehicles
\d+ zones
\d+ street_segments
```

If you need to drop and rebuild the database, use:
```bash
dotnet ef database drop --project ParkSpotTLV.Infrastructure --startup-project ParkSpotTLV.Api
dotnet ef database update --project ParkSpotTLV.Infrastructure --startup-project ParkSpotTLV.Api
```

## API UI
You can access the Scalar OpenAPI UI at http://localhost:8080/scalar

To check service readiness (including database and PostGIS), use http://localhost:8080/ready

For a simple liveness check, use http://localhost:8080/health


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
