# RaceDay (RunWay) – PROG6212 POE Part 3

RaceDay is a platform for organising and joining running events. Part 3 delivers the **ASP.NET Core MVC front end**
that consumes the **REST API**, with **Azure Blob Storage** for images, everything **containerised with Docker**.

| Project | What it is |
|---|---|
| `RunWay.Api` | ASP.NET Core Web API (.NET 9). Owns the SQL Server database, ASP.NET Core Identity + JWT, business rules, and **all** Azure Blob Storage access. Swagger at `/swagger`. |
| `RunWay.Web` | ASP.NET Core MVC (.NET 9). **No database or Azure code** – it only calls the API over HTTP (`HttpClient`) and holds the user's JWT in server-side session. |

```
Browser ──> RunWay.Web (MVC) ──HTTP + JWT──> RunWay.Api ──> SQL Server
                                                   └──────> Azure Blob Storage (Azurite locally)
```

## Start here: your supplied design is preserved

See [START-HERE.md](docs/START-HERE.md) to run immediately without Docker using the original demo races and accounts. See [validation](docs/VALIDATION.md) for completed checks and [Azure student setup](docs/AZURE-STUDENT-SETUP.md) for your subscription.

## Run it with Docker (one command)

Requires Docker Desktop. From the repository root:

```bash
docker compose up --build
```

Then open:

* **Web app:** http://localhost:5000
* **API / Swagger:** http://localhost:5001/swagger

The first start takes a minute or two (SQL Server starts, the database is created and demo data is seeded).
This starts four containers: `web`, `api`, `sqlserver` and `azurite` (a local Azure Blob Storage emulator).

Stop with `Ctrl+C`; `docker compose down -v` also removes the data volumes.

### Using real Azure Blob Storage
Copy `.env.example` to `.env` and set `AZURE_STORAGE_CONNECTION_STRING` to your storage account's connection string
(the container `raceday-images` is created automatically), then run `docker compose up --build` again.
Images are stored as private blobs and streamed to the browser through the API (`GET /api/files/...`), so the MVC app never talks to Azure.

## Run it without Docker (Visual Studio)
1. Start SQL Server (LocalDB works) and Azurite (`npm i -g azurite` then `azurite-blob`), or use the student Azure instructions linked above. Keep connection strings in API user secrets/environment settings.
2. Check `ConnectionStrings:DefaultConnection` in `RunWay.Api/appsettings.json`.
3. Set both projects as startup projects (API on `http://localhost:5001`, MVC on `http://localhost:5000`) and press F5.
   `RunWay.Web/appsettings.json` → `Api:BaseUrl` already points at the API.

The API creates the database, the roles (Admin, Organiser, Participant), the admin user and demo data on first start.
(If you add EF Core migrations later, they are applied automatically instead.)

## Demo accounts

| Role | Email | Password |
|---|---|---|
| Admin | `admin@admin.com` | `Admin@123` |
| Organiser | `organiser@raceday.com` | `Organiser@123` |
| Participant | `participant@raceday.com` | `Participant@123` |

Demo data (5 events – 3 upcoming, 2 finished with published results – plus categories and enrolments) is seeded when
`Seed__DemoData=true` (default in `docker-compose.yml`). Change the passwords and the JWT key before any real deployment.

## Requirements checklist

| Requirement | Where |
|---|---|
| MVC consumes the Part 2 API, no DB access in MVC | `RunWay.Web/Services/RaceDayApiClient.cs`; `RunWay.Web.csproj` has no EF/SQL packages |
| Register / login via API, auth state + role kept across pages (session) | `AccountController`, JWT in server session + auth cookie (`Program.cs`) |
| Role-specific navigation, protected pages, logout | `_Layout.cshtml`, `[Authorize(Roles=...)]` on controllers |
| Organiser: dashboard, events CRUD + banner, categories, enrolments, results | `OrganiserController` + `Views/Organiser` |
| Participant: browse/filter events, event page, enrol with category, my enrolments, my results, profile + picture | `HomeController`, `EventsController`, `EnrolmentsController`, `ResultsController`, `ProfileController` |
| Azure Blob Storage only through the API | `RunWay.Api/Services/FileStorage.cs`, `EventsController.UploadBanner`, `ProfileController.UploadPicture`, `FilesController` |
| Docker + README run command | `RunWay.*/Dockerfile`, `docker-compose.yml` |
| RaceDay UI, Bootstrap, colour-coded statuses, friendly errors | `wwwroot/css/site.css`, `_StatusBadge.cshtml`, `RaceDayApiClient` error handling |
| GitHub Actions builds API + MVC | `.github/workflows/ci.yml` |

## Main API endpoints

| Method & route | Access |
|---|---|
| `POST /api/auth/register`, `POST /api/auth/login` | public |
| `GET /api/events` (filters: `search, eventType, from, to, minDistance, maxDistance`), `GET /api/events/{id}` | public |
| `POST/PUT/DELETE /api/events`, `POST /api/events/{id}/banner`, `GET /api/events/mine` | Organiser (owner) / Admin |
| `GET/POST /api/events/{id}/categories`, `PUT/DELETE /api/categories/{id}` | Organiser (owner) / Admin |
| `POST /api/enrolments`, `GET /api/enrolments/mine`, `PUT /api/enrolments/{id}/cancel` | Participant |
| `GET /api/events/{id}/enrolments`, `PUT /api/enrolments/{id}/status` | Organiser (owner) / Admin |
| `GET /api/events/{id}/results`, `GET /api/results/mine`, `PUT /api/results`, `DELETE /api/results/{enrolmentId}` | public / Participant / Organiser |
| `GET/PUT /api/profile`, `POST /api/profile/picture` | any signed-in user |
| `GET /api/dashboard/organiser`, `GET /api/dashboard/admin`, `GET /api/admin/users`, `PUT /api/admin/users/{id}/roles` | Organiser / Admin |
| `GET /api/files/{blobName}` | public (images only) |

## Business rules
* Enrolling records a **Pending** enrolment immediately; the organiser can **Confirm** or **Cancel** it. Colours: green = Confirmed, amber = Pending, grey = Cancelled.
* One enrolment per participant per event; enrolment closes when the event date passes.
* Results can only be entered after the event, for **Confirmed** participants, with a unique position per event.
  "My Results" shows e.g. *47th of 312 finishers*.
* Only the organiser who created an event (or an Admin) can change it.

## Submission checklist
* Push to GitHub on a clearly named branch (e.g. `part3`) with at least 20 meaningful commits.
* Add your GitHub link and the **unlisted YouTube video link** here: <!-- GitHub: ... --> <!-- YouTube: ... -->
* In the video: run `docker compose up --build`, show registration/login for each role, the MVC structure
  (controllers → `RaceDayApiClient` → API), an image upload appearing from Blob Storage, and the Docker setup.

## Question paper alignment

See the [full requirements audit](docs/REQUIREMENTS-AUDIT.md). Planning deliverables are in [docs](docs/README.md): the ERD PNG, full endpoint plan and SQL Server script with sample users/events/results.

The updated implementation has 11 tables: seven standard ASP.NET Identity tables plus Events, Categories, Enrolments and Results. The old Part 1 seven-table design used a global category/junction arrangement; the current design has event-owned distance categories. Results includes EventId to enforce unique event positions, and Identity replaces the old Users/Roles/password layout. The ERD and SQL are exported from the current EF Code First model, so their attributes and relationships agree. Use a fresh development database or reviewed migrations for older data.

Event forms support **Run, Walk and Cycle**, as required. Existing Road/Trail/FunRun demo records remain readable as running events.

Run formal unit tests with:
```sh
dotnet test RunWayy.sln
```
The 17 xUnit controller cases complement the 96 HTTP/MVC integration checks. CI executes both suites and also runs the standalone planning SQL on SQL Server.

Repository: https://github.com/Sphelele20/RunWayy/tree/part3/raceday-mvc-api

Required walkthrough videos: Part 1 planning/SSMS, Part 2 running API/unit tests, Part 3 MVC/Azure/Docker. Record your current implementation and add your actual unlisted URLs here before ARC submission. Historical per-part commit requirements and pre-coding planning cannot be inferred from a passing build; see the audit.