# RunWayy · RaceDay Part 3

RaceDay is an ASP.NET Core MVC frontend backed by a REST API for running, walking and cycling events. It continues the RunWayy coursework and keeps the earlier MVC Identity source in `legacy/Part2`.

## Start without Docker

Install the .NET 9 SDK. From this folder:

```powershell
dotnet build RunWayy.sln
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project src/RunWay.Api --no-launch-profile --urls http://localhost:5100
```

In a second PowerShell terminal:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project src/RunWay.Web --no-launch-profile --urls http://localhost:5200
```

Visit **http://localhost:5200**. Register an Organiser to create an event, add categories and upload a banner. Register a separate Participant to enter it and update a profile. There are no seeded passwords. API Swagger is at **http://localhost:5100/swagger** in Development.

On Windows you can also run `./scripts/start-local.ps1`. Stop it with Ctrl+C. Local development uses an API-owned SQLite database and local images; no SQL Server, Azure account or Docker is needed to begin. Dates entered and displayed are **UTC** (subtract two hours from South African time).

## Docker: one command

```sh
docker compose up --build
```

Open **http://localhost:5200**. Both services run as a non-root user. Named volumes persist the API database/images and MVC cookie keys. The default container configuration explicitly permits local images for parity with local development. To demonstrate Azure Blob Storage, follow [Azure setup](docs/AZURE-STUDENT-SETUP.md) and change the storage settings; local storage is not evidence of Azure integration.

If Docker reports that Virtual Machine Platform is disabled, follow [the Windows Docker guide](docs/DOCKER-WINDOWS.md). You can keep developing with the commands above while resolving it.

## Structure

- `src/RunWay.Web`: MVC controllers, views, role navigation, session and API client. No database access or Azure SDK.
- `src/RunWay.Api`: Identity accounts, bearer sessions, EF Core persistence, ownership and business rules, image storage.
- `src/RunWay.Contracts`: shared request/response shapes and input validation.
- `tests/RunWay.Smoke`: executable integration checks against real API and MVC processes.
- `legacy/Part2`: preserved earlier source with Windows-compatible filenames; excluded from the solution.
- `docs`: architecture, review, API routes, Azure/Docker setup and submission checklist.

## Checks

```sh
dotnet build RunWayy.sln
dotnet run --project tests/RunWay.Smoke -- .
```

The test runner starts both applications on free loopback ports with a fresh temporary database. It checks roles, ownership, validation, duplicate enrolment, results, uploads, MVC sign-in and anti-forgery protection. It changes only its temporary database date to exercise a completed event. It does not touch your normal database.

GitHub Actions builds both applications and the contracts, runs integration checks, then builds and starts both containers. See [validation status](docs/VALIDATION.md) for what has actually been verified in this delivery.

## Extending the project

Read [architecture and migration notes](docs/ARCHITECTURE.md) before changing storage or authentication. Existing Part 2 accounts are not automatically imported. The initial API schema is separate from the old RunWayDB and RaceDay SQL script. Use a new empty database for first startup; `EnsureCreated` does not upgrade an existing schema. Back up data and introduce reviewed EF migrations before changing a deployed schema.

This project retains the repository's .NET 9 target. Plan and test a move to a supported SDK/runtime before long-term deployment. Use HTTPS at your hosting proxy for any public deployment; local HTTP addresses here are for development.
