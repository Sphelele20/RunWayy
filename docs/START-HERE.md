# Your supplied RaceDay website

This version uses POE_PART3.zip as its base. The black/orange CSS, page layout, event cards, navigation and forms are preserved. The changes complete and verify the functionality behind those pages.

## Run now without Docker

Install the .NET 9 SDK, open a terminal in this folder, and run:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project RunWay.Api --no-launch-profile --urls http://localhost:5001
```

Open a second terminal in this folder:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project RunWay.Web --no-launch-profile --urls http://localhost:5000
```

Open http://localhost:5000. The development configuration uses an API-owned SQLite database and development-only local image storage. It seeds the original five example races, categories, entries and past results. No database or Azure access was added to MVC.

| Role | Email | Password |
|---|---|---|
| Organiser | organiser@raceday.com | Organiser@123 |
| Participant | participant@raceday.com | Participant@123 |
| Admin | admin@admin.com | Admin@123 |

These are demo-only accounts. Production does not seed them unless explicitly configured with Seed:DemoData=true. The Compose configuration is explicitly a local demonstration with demo accounts and Azurite.

All event form times use South African time (UTC+2) on both Windows and Linux.

## Tests

```powershell
dotnet build RunWayy.sln
dotnet run --project tests/RunWay.Smoke -- .
```

96 checks cover seeded races, filters, role permissions, ownership, event/category forms, enrolment/cancellation, results, uploads, profile, admin views, session sign-out and CSRF. The default runner uses a fresh temporary database and kills only the processes it starts.

## Docker

```powershell
docker compose up --build
```

This runs the original SQL Server + Azurite + API + MVC architecture. See DOCKER-WINDOWS.md for your Virtual Machine Platform error. The Azurite account key has been corrected to the official emulator key, and API/MVC key directories are writable by the non-root container users.

## Azure student account

Follow AZURE-STUDENT-SETUP.md. The student email authenticates you to Azure; it is not a storage connection string or RaceDay login. A real Azure upload must be verified under your subscription before recording your final video.

## Before extending an existing database

This version adds integrity constraints and an EventId on results. Use a fresh development database for this copy. Do not reuse or delete an older database automatically. Back up old data and create/test EF migrations before deploying schema changes. EnsureCreated initializes a new database but cannot upgrade an existing one.
