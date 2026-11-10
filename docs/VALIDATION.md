# Validation status

Checked on 10 November 2026.

| Check | Result |
|---|---|
| .NET 9 Debug build: API, MVC, Contracts, Smoke | Passed |
| .NET 9 Release build | Passed |
| Real-process integration checks | 52 passed in Debug and Release |
| Browser rendering | Upcoming-events page visually inspected at the available narrow viewport |
| Local PNG/JPEG banner/profile upload | Passed; fake/oversized uploads rejected |
| Role/ownership/duplicate enrolment/results/CSRF/logout checks | Passed |
| NuGet advisory audit | Unavailable: host could not reach NuGet advisory service (NU1900) |
| Docker build/runtime | Not run locally: Docker is not installed/available in this environment; CI job supplied |
| Live Azure Blob Storage | Not run: student subscription and storage identity must be configured |
| SQL Server provider | Configured, not tested; test suite uses SQLite |
| ERD Word diagram | Not visually verified: connector returned empty large-binary content; direct download failed TLS |

The test runner starts both applications, uses temporary test accounts/data and verifies API responses plus authenticated MVC pages. It backdates only its isolated test event to verify result publishing after an event. It does not claim live Azure or Docker success.

Run the Azure verification in AZURE-STUDENT-SETUP.md and confirm the GitHub container job before submitting. Record your own demonstration and actual YouTube URL.
