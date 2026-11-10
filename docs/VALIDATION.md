# Verification of the supplied POE website

- Original site.css retained byte-for-byte. Existing page structure and theme retained.
- API and MVC build passed.
- 96 integration checks passed locally against an isolated SQLite database.
- Checks include submitting the original MVC event/category/enrolment/profile-photo forms, all role pages, image byte round trips, ownership, duplicate records, results and sign-out.
- GitHub CI passed all 96 checks against Docker, SQL Server and Azurite, including actual blob uploads/downloads and MVC forms. [Successful run](https://github.com/Sphelele20/RunWayy/actions/runs/42832535882).
- Live Azure student storage remains unverified until your account is configured.
- Local NuGet vulnerability lookup was unavailable (NU1900). No security-audit success is claimed.

## Fixes to the supplied code

- Corrected decimal/double multiplication that prevented demo-data compilation.
- Added development-only SQLite/local images so the site can run without Docker.
- Kept production SQL Server and API-owned Azure storage; added managed identity support.
- Validated actual PNG/JPEG/WebP signatures and safe local filenames.
- Enforced unique category names and result positions in the database.
- Prevented changing a result-bearing enrolment away from Confirmed or rescheduling a result-bearing event.
- Made deletion of an event and its entries/results atomic in one SaveChanges call, compatible with SQL Server retry handling.
- Added JWT revocation on logout and role changes.
- Kept event times consistent in South Africa on Windows/Linux.
- Removed production sample JWT secrets and automatic demo accounts.
- Corrected the original Azurite account key and non-root container key-directory permissions.
- Bundled Bootstrap and core scripts locally without changing the design. Google Fonts and the unobtrusive validation script retain their existing CDN sources; server validation remains active.

## Full-paper verification, 10 November 2026

- 17 xUnit tests passed, covering success/failure registration, login, event management, ownership and enrolment rules.
- 96 HTTP/MVC integration checks passed locally and in CI; these also verify authentication and role enforcement.
- Planning-package validation passed: the current 11-table ERD, EF metadata, endpoint plan and SQL script are included.
- The standalone SQL script executed successfully on a clean SQL Server database in CI.
- Both build and Docker jobs passed on commit 2c92a30c31e2f099d67edabedefff6d8917333f9.
- [Verified successful GitHub Actions run](https://github.com/Sphelele20/RunWayy/actions/runs/42832535882).
- This verifies the implementation, not historical commit counts, a pre-coding planning date, real Azure access, walkthrough videos or ARC submission. The required literal CI screenshot remains to be saved/embedded.