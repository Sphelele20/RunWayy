# Audit against the supplied question paper

The screenshots supplied on 10 November 2026 cover the Part 1 planning requirements, Part 2 API requirements, and previously supplied Part 3 MVC requirements. This audit does not assign marks or claim that unprovided pages were checked.

## Part 1

| Requirement | Evidence and remaining limit |
|---|---|
| At least six entities, attributes, PK/FK and all relationship cardinalities | RaceDay_ERD.png and its SVG source show all 11 implementation tables; schema.json is exported directly from EF metadata |
| ERD and SQL agree; explain differences in README | RaceDay_Schema.sql is generated from the same EF model; README explains changes from the old seven-table plan |
| Complete RaceDay API plan with method, route, purpose, role, body and expected response | RaceDay_Endpoint_Plan.md covers all current API routes; unrelated library example is replaced |
| SQL CREATE/keys/constraints and realistic sample data | Standalone SQL creates 11 tables and samples for two organisers, two participants, three events, categories, entries and results |
| Clean SQL Server execution | CI runs sqlcmd with stop-on-error in a separate empty RaceDayPlanningDb; see latest passing CI result |
| Planning files in /docs and structure validation workflow | scripts/validate-planning.ps1 runs in CI |
| 20 meaningful commits for Part 1 | Original RaceDay default branch had five commits when checked; the per-part history requirement is not satisfied by inventing/backdating commits |
| Planning completed before API coding | Current plan is an honest reconciliation; historical pre-coding planning cannot be retroactively proved |
| README green-build screenshot and YouTube link | Successful CI is linked in README; a literal screenshot still needs to be saved and embedded. A valid Part 1 walkthrough remains the student's responsibility |

## Part 2

| Requirement | Evidence and remaining limit |
|---|---|
| ASP.NET Core API + EF Code First + SQL Server | RunWay.Api; Docker tests execute against real SQL Server |
| Role-selected registration, hashed passwords, session/access control | Identity + role validation + JWT security stamps; MVC server session; HTTP role/ownership checks |
| Profile, events, categories, enrolments and results | All implemented and listed in endpoint plan |
| Run, walk and cycle event types | Run/Walk/Cycle added to API validation and existing event forms/filter menus; legacy running subtypes remain readable |
| Age or distance categories | Distance-based categories implemented, including selected-category enrolment |
| Swagger with clear descriptions, input and responses | XML documentation plus operation summaries, role guidance, schemas and status descriptions |
| Unit tests for success/failure auth, events and enrolment | 17 xUnit cases, plus 96 real-process integration checks that verify HTTP authentication/role enforcement |
| CI builds and runs tests on every push | dotnet build, dotnet test, smoke tests and Docker SQL/Azurite checks |
| 20 meaningful commits for this part | RunWayy default branch had 21 total commits when checked, but a count alone does not prove 20 meaningful API-development commits; historical Part 2 ZIP was MVC Identity rather than the required API |
| README build screenshot and API YouTube demo | Successful CI is linked in README; the required literal screenshot and current API video still need to be supplied |

## Part 3

MVC has no database/Azure SDK access. It calls the API for login, profiles, events, categories, entries, results and images. Organiser/participant menus, dashboards, form validation, status colours and the original black/orange design are retained. Docker builds and runtime workflows are tested in CI. Real student-subscription Azure uploads still require account setup and verification.

## Submission items still requiring the student

- Save a screenshot of the linked successful GitHub Actions run and embed it in README (the live status link alone does not meet the screenshot requirement).
- Demonstrate NEW profile/banner uploads against real Azure Blob Storage using the student subscription.
- Record the required unlisted YouTube walkthroughs. The existing Part 2 submission document contains an older video link, but its relevance to the current REST API has not been verified; it is not presented as a new demonstration.
- Provide actual video URLs in the README and submit the appropriate repository/video links in ARC.
- Discuss the planning/history shortfall with the lecturer if the original Part 1/2 submissions cannot be amended. Do not fabricate timestamps, videos or a historical commit trail.

The strongest evidence is the running application, passing tests and matching planning/schema package. Those do not automatically fulfil historical or presentation/submission requirements.
