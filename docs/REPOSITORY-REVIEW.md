# Repository review and Part 3 mapping

Reviewed RunWayy master at `96eedadd575b2bc169cb96b565f541922d3dafbc` and the RaceDay main tree at `3a66965e83efe09ca6d3fa06d61ead6d5de2805e`.

## What was present

RunWayy contained 91 source/configuration files: one .NET 9 MVC project with SQL Server-backed ASP.NET Identity, scaffolded account pages, user-role administration, a local profile image service and a mostly empty home screen. It did not contain a REST API, events, categories, enrolment or results functionality, Docker files or a CI workflow. Several files had literal quote characters and Add prefixes; README and solution names had trailing dots. These cannot be checked out normally on Windows. All original file contents are preserved in `legacy/Part2` under corrected names, with original paths and blob SHAs in `docs/original-paths.json`.

The custom login handler redirected every successful sign-in to the admin dashboard. Its subsequent organiser test used the misspelling `Ongarniser`. Registration assigned Participant only, images were saved directly by MVC, and the app seeded a known admin password. Those flows are replaced in the Part 3 solution; legacy code is reference material, not a project dependency.

RaceDay contained a README, a SQL script, an ERD Word document and an API Word document. The README references docs paths that do not exist. The actual SQL contains Roles, Users, Events, Categories, EventCategories, Enrollments and Results, plus constraints and sample rows. The API document was extracted and read: despite its RaceDay title, it describes book loans (`/api/books/{id}/borrow`), so it cannot define the event API. The GitHub connector returned empty content for the binary ERD; direct binary download was blocked by host TLS credentials. Its diagram could not be visually verified. The implementation therefore uses the SQL entity relationships and the supplied Part 3 screenshots as its evidence.

## Part 3 coverage

| Requirement | Implementation |
|---|---|
| MVC consumes REST API only | Typed server-side HttpClient; no EF, SQL or Azure dependencies in Web |
| API register/login, session, roles | Identity password hashing/lockout in API; opaque bearer tokens hashed in DB; MVC session and cookie |
| Organiser dashboard | Own event count, total entries, upcoming count, event table |
| Event management and banner | Create, edit, delete, view; date, distance, type, location, description; API upload |
| Category management | Event-owned categories, add/remove; enrolled categories cannot be removed |
| Start list and results | Own-event enrolment list; publish/update finish time and position after event date |
| Participant screens | Filter upcoming events, details/categories, immediate confirmed entry, own entries/results/profile |
| Azure Blob Storage | API image provider; private blob container with API media proxy |
| Docker | Multistage non-root images and persistent Compose services |
| UI | Locally bundled Bootstrap, responsive RaceDay theme, role menus, labelled forms, textual status badges |
| CI | Build and integration job plus container build/start checks |

## Design choices

British `Organiser` is the canonical API role, matching RunWayy. Part 1 uses `Organizer`; no silent account/schema import is attempted. Categories belong to one event rather than using a global category/junction table. A result is stored on its unique enrolment, preserving the one-result-per-entry rule without a separate result table. The API owns all database access and event rules.

Registration allows the two coursework roles. Admin management from the earlier app is preserved in legacy but is outside the required Part 3 two-role interface. Images are optional; banners can be added during creation or editing. Files are constrained to PNG/JPEG signatures and 2 MB, saved under random names, and served as image content. Replacing/deleting records currently leaves old image objects for later retention cleanup.

## Submission boundaries

The screenshot's commit-count requirement is a rubric item, not permission to fabricate history. Preserve real implementation commits and continue making meaningful commits as you develop. The submission itself, ARC upload and YouTube video remain the student's actions. Live Azure, Docker runtime and CI status must be verified separately where marked in VALIDATION.md.
