# Connection to DB on docker

Project uses 'secret data' to connect to database. Run in project root:
```bash
echo $env:DB_CONNECTION 
```

Creating migrations list or updating the table through project:
```bash
dotnet ef migrations add InitialCreate --project ParkSpotTLV.Infrastructure --startup-project ParkSpotTLV.Api
dotnet ef migrations list --project ParkSpotTLV.Infrastructure --startup-project ParkSpotTLV.Api
dotnet ef database update --project ParkSpotTLV.Infrastructure --startup-project ParkSpotTLV.Api
```

In docker terminal, load parkspot-db:
```bash
docker exec -it parkspot-db psql -U admin -d parkspot_dev
```

To list tables:
```bash
\dt
```

To inspect tables:
```bash
\d+ users
\d+ vehicles
\d+ zones
\d+ street_segments
```

# Entity Classes
They are your entity definitions. 
EF Core uses them to generate the tables/columns/relations/indexes in Postgres.
Each class → one table. Each property → one column. Nav properties → foreign keys.


## Types

### User
```cs
Guid Id
string Username
string PasswordHash
ICollection<Vehicle> Vehicles
```

### Vehicle
```cs
Guid Id
Guid OwnerID
User Owner
VehicleType Type
bool HasDisabledPermit
int ZonePermit
```

### Street Segment
```cs
Guid Id
string? Name
MultiLineString Geom
Guid? ZoneId
Zone? Zone
ParkingType ParkingType
ParkingHours ParkingHours
```

### Zone
```cs
Guid Id
MultiPolygon Geom
```



## The first time we create the DB
EF compares entity classes + DbContext mapping to what exists in the DB and generates a migration file.

This file is just a recipe (create tables, columns, FKs, GiST indexes, postgis extension, SRIDs, constraints).

EF CLI calls your design-time factory to build a DbContext without starting the API.

The factory uses the connection string, configures Npgsql + NetTopologySuite (the “builder” style—options are built step by step).

EF opens a connection to Postgres and executes any pending migrations.

Postgres now physically has your tables, columns, constraints, and spatial indexes.

## Design-time (EF CLI → factory):

The factory follows a builder pattern: it builds the DbContext options piece by piece (choose provider Npgsql → enable NetTopologySuite → set migrations assembly → return configured options).

This lets the EF CLI create the DbContext without running your whole web app—crucial for reliable migrations.

## Runtime (API → WebApplicationBuilder):

On app start, your API’s WebApplicationBuilder composes the app:

reads config & secrets → builds the DbContext options → registers DbContext in DI → app.Build() finishes the pipeline.

The API is now ready to open DbContext instances on demand for requests.




