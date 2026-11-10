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
| Docker build/runtime | Passed in GitHub Actions: both images built, Compose started, API/MVC health and sign-in page checks passed; not run locally |
| Live Azure Blob Storage | Not run: student subscription and storage identity must be configured |
| SQL Server provider | Configured, not tested; test suite uses SQLite |
| ERD Word diagram | Not visually verified: connector returned empty large-binary content; direct download failed TLS |

The test runner starts both applications, uses temporary test accounts/data and verifies API responses plus authenticated MVC pages. It backdates only its isolated test event to verify result publishing after an event. It does not claim live Azure or Docker success.

[GitHub Actions verification](https://github.com/Sphelele20/RunWayy/actions/runs/42819995470) passed for both build-and-test and containers jobs on the implementation commit.

Run the Azure verification in AZURE-STUDENT-SETUP.md before submitting. Record your own demonstration and actual YouTube URL.
