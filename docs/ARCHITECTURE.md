# Architecture

Browser -> MVC controllers/views -> REST API -> Identity/EF Core -> SQLite or SQL Server.
Browser image requests -> MVC media proxy -> API -> local development files or private Azure blobs.

The shared Contracts project contains DTOs and validation only. The MVC app never reads a database, Azure SDK or storage key. The API is the authority for role and event ownership checks. Each authenticated request loads the user role associated with the bearer token.

## Sign-in lifecycle

The API verifies passwords using ASP.NET Core Identity, applies account lockout after five failed attempts, issues a random 256-bit opaque token valid for two hours and stores only its SHA-256 hash. MVC saves the token and role in server-side session and sets an HTTP-only authentication cookie containing identity/role claims. Cookie validation rejects a principal whose session has disappeared. Both expire after two hours. MVC sign-out revokes the API token and clears its own session/cookie. Invalid API sessions redirect to sign-in.

The current in-memory session store supports one MVC instance. Restarting it signs users out. Use a distributed session cache and shared encrypted data-protection keys before scaling beyond one instance. Container keys persist in a named volume. No refresh-token flow is provided.

## Persistence

SQLite is the development/Compose default. SQL Server can be selected with `Database__Provider=SqlServer` and `ConnectionStrings__RaceDay` pointing to a new database. SQL Server is configured but has not been exercised by this delivery's tests. Never run the original Part 1 script over the new Identity schema.

The API uses `EnsureCreated` only for first creation. It is not a migration strategy. Before extending a database with real data:
1. Back up and test with a copy.
2. Add a baseline and reviewed EF migrations for the chosen provider.
3. Apply migrations as an explicit deployment step rather than mixing `EnsureCreated` and migrations on the same database.
4. Map/import old accounts deliberately; passwords and role spelling are not compatible with a blind SQL copy.

Unique indexes enforce one participant entry per event and one category name per event. Category membership is verified before enrolment. Event deletion removes its enrolments and categories in a transaction. A result requires an owned, completed event and a valid existing enrolment. Position is a positive integer; tied positions are allowed. No capacity/payment workflow is part of this rubric.

## API errors and dates

DTO validation produces HTTP 400. Missing sessions return 401; wrong roles/ownership return 403; missing records return 404; duplicate or referenced records return 409. MVC preserves form input on local validation errors and displays friendly API/unavailable messages. All event timestamps are interpreted as UTC and labelled that way in the UI.

## Deployment

Terminate HTTPS at a trusted hosting proxy and configure forwarded headers for that deployment. Restrict the API to the web service/internal network where practical. Keep secrets in API environment settings or Key Vault. Configure managed identity for Blob Storage, encrypted persistent key storage and a distributed session cache as needed. The coursework Compose configuration publishes the API on loopback for inspection and uses local persistence by default.
