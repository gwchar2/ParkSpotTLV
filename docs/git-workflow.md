## Morning: 
Update local main, rebase your branch (Every day when you start)
```bash
git fetch origin                 # get remote updates
git switch main
git pull --ff-only               # fast-forward your local main
```

Start a task (new branch)
```
git switch -c <branch_name>
```
...edit code…

## During the day: 
Commit in small, meaningful chunks; push whenever:
> You reached a checkpoint,
> You want CI to run,
> You want your teammate to see progress,
> You just want a safe remote backup.

```git
git add -A
git commit -m "feat: draw colored segments on map"
```
First push of branch: `git push -u origin <branch_name>`
Else: `git push`


Keep your branch in sync with latest main
```git
git fetch origin
git merge origin/main
```
Resolve any conflicts if needed.

## Open a PR (recommended)
On GitHub: compare <branch_name> → into main.
After review & merge, delete the branch (GitHub offers a button).

## Start the next task
```git
git switch main
git pull --ff-only
git switch -c  <branch_name>
Quick sanity checks
git status       # what changed locally
git branch -vv   # what branch you’re on + upstream
git remote -v    # where “origin” points
```



- Completely new table
```
docker compose down
If database tables change:
Delete the contents of Migrations/ folder in ParkSpotTLV.Infrastructure
docker compose down -v --remove-orphans
dotnet ef migrations add InitialCreate -p ./ParkSpotTLV.Infrastructure -s ./ParkSpotTLV.Api
F5 // docker compose up -d db
dotnet ef database update -p ./ParkSpotTLV.Infrastructure -s ./ParkSpotTLV.Api
Re-F5 // docker compose up -d api
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
SELECT * FROM daily_budgets LIMIT 10;

SELECT DISTINCT ON (z.code)
       z.code        AS zone_code,
       s.id          AS segment_id,
       s.name_english
FROM street_segments s
JOIN zones z ON s.zone_id = z.id
ORDER BY z.code, s.name_english NULLS LAST;

```