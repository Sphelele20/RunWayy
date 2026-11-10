# RaceDay endpoint plan

This is the reconciled endpoint specification for the current RaceDay implementation. It replaces the unrelated library example in the older planning file. It documents the system as implemented; it does not claim to have been written before the historical Part 2 submission.

Organisers create and manage their own events, categories, entries and results. Participants browse events, enter a selected category, and view their own profile, entries and results. Admin is an additional maintenance role. All data operations run through the API. API base path: /api.

## Endpoint table

Request bodies are JSON unless marked multipart. A dash means no request body. Protected requests use Authorization: Bearer plus the login token.

| HTTP method | Route | Description | Role required | Request body / fields | Expected response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Create an account and start a session | Public | firstName, lastName, email, password, gender, role (Organiser or Participant) | 200 AuthResponse; 400 invalid or duplicate account |
| POST | /api/auth/login | Verify credentials and issue a JWT | Public | email, password | 200 AuthResponse; 401 invalid/locked account |
| POST | /api/auth/logout | Revoke the user's active tokens | Any signed-in role | — | 204; 401 expired session |
| GET | /api/profile | Read own personal information | Any signed-in role | — | 200 ProfileDto; 401; 404 user missing |
| PUT | /api/profile | Update own personal information | Any signed-in role | firstName, lastName, gender, phoneNumber | 200 ProfileDto; 400; 401; 404 |
| POST | /api/profile/picture | Store own profile photo through the API | Any signed-in role | Multipart file (PNG/JPEG/WebP, <=5 MB) | 200 ProfileDto; 400 invalid image; 401; 404 |
| GET | /api/events | Browse upcoming events | Public | Query: search, eventType (Run/Walk/Cycle), from, to, minDistance, maxDistance, includePast | 200 EventDto array; 400 invalid query |
| GET | /api/events/{id} | Read event information and categories | Public | — | 200 EventDto; 404 |
| GET | /api/events/mine | List owned events, including past events | Organiser; Admin sees all | — | 200 EventDto array; 401; 403 |
| POST | /api/events | Create an upcoming event | Organiser or Admin | name, description, date, location, distanceKm, eventType | 201 EventDto and Location; 400; 401; 403 |
| PUT | /api/events/{id} | Edit an event | Owning Organiser or Admin | Same event fields | 200 EventDto; 400; 401; 403; 404; 409 cannot reschedule published results |
| DELETE | /api/events/{id} | Delete an event with its entries and results | Owning Organiser or Admin | — | 204; 401; 403; 404 |
| POST | /api/events/{id}/banner | Upload or replace an event banner | Owning Organiser or Admin | Multipart file (PNG/JPEG/WebP, <=5 MB) | 200 EventDto; 400; 401; 403; 404 |
| GET | /api/events/{eventId}/categories | View an event's distance categories | Public | — | 200 CategoryDto array; 404 |
| POST | /api/events/{eventId}/categories | Add a distance category | Owning Organiser or Admin | name, distanceKm | 200 CategoryDto; 400; 401; 403; 404; 409 duplicate name |
| PUT | /api/categories/{id} | Edit a category | Owning Organiser or Admin | name, distanceKm | 200 CategoryDto; 400; 401; 403; 404; 409 |
| DELETE | /api/categories/{id} | Delete an unused category | Owning Organiser or Admin | — | 204; 401; 403; 404; 409 category has entries |
| POST | /api/enrolments | Enter an upcoming event in one category | Participant | eventId, categoryId | 200 EnrolmentDto (Pending); 400 closed/wrong category; 401; 403; 404; 409 duplicate |
| GET | /api/enrolments/mine | Read own entries and statuses | Participant | — | 200 EnrolmentDto array; 401; 403 |
| PUT | /api/enrolments/{id}/cancel | Cancel own entry before the event | Owning Participant | — | 204; 400 event passed; 401; 403; 404 |
| GET | /api/events/{eventId}/enrolments | View the event's participant list | Owning Organiser or Admin | — | 200 EnrolmentDto array; 401; 403; 404 |
| PUT | /api/enrolments/{id}/status | Confirm or cancel an entry | Owning Organiser or Admin | status (Pending/Confirmed/Cancelled) | 204; 400 invalid status; 401; 403; 404; 409 published result exists |
| GET | /api/events/{eventId}/results | Read published event results | Public | — | 200 ResultDto array (empty until published) |
| GET | /api/results/mine | Read own race history and finishing totals | Participant | — | 200 ResultDto array; 401; 403 |
| PUT | /api/results | Publish/update a confirmed participant's result | Owning Organiser or Admin | enrolmentId, finishTime (HH:mm:ss), position | 204; 400 event not concluded/unconfirmed/invalid time; 401; 403; 404; 409 position taken |
| DELETE | /api/results/{enrolmentId} | Withdraw a published result | Owning Organiser or Admin | — | 204; 401; 403; 404 |
| GET | /api/files/{name} | Stream an event/profile image from API storage | Public | name starts events/ or profiles/ | 200 image bytes; 404 |
| GET | /api/dashboard/organiser | Read own event, entry and pending totals | Organiser | — | 200 dashboard summary; 401; 403 |
| GET | /api/dashboard/admin | Read system totals | Admin | — | 200 admin summary; 401; 403 |
| GET | /api/admin/users | List users and their roles | Admin | — | 200 user array; 401; 403 |
| GET | /api/admin/users/{id} | Read one user's role assignment | Admin | — | 200 user; 401; 403; 404 |
| PUT | /api/admin/users/{id}/roles | Assign existing roles and invalidate old sessions | Admin | roles array | 204; 400; 401; 403; 404 |

## Request and response examples

Registration:
```json
{"firstName":"Lerato","lastName":"Dlamini","email":"lerato@example.com","password":"Example@123","gender":"Female","role":"Participant"}
```

Authentication response:
```json
{"token":"<JWT>","expiresAt":"2026-11-10T15:00:00Z","userId":"<id>","email":"lerato@example.com","fullName":"Lerato Dlamini","profileImage":null,"roles":["Participant"]}
```

Event:
```json
{"name":"Durban Coastal Cycle","description":"A timed coastal cycle event.","date":"2027-02-14T06:00:00","location":"Durban Beachfront","distanceKm":25,"eventType":"Cycle"}
```

Enrolment:
```json
{"eventId":3,"categoryId":4}
```

Result:
```json
{"enrolmentId":1,"finishTime":"00:47:12","position":1}
```

## Contracts and rules

EventDto includes id, event details, organiser identity, bannerImage, enrolmentCount, isPast and categories. EnrolmentDto includes participant/event/category identity, status and any published time/position. ResultDto includes event name/date, category, finish time, position and total finishers. ProfileDto never exposes password hashes.

Passwords are Identity hashes; no plaintext passwords are returned. JWTs expire and are revoked on logout/role change. MVC keeps the JWT in server-side session. Non-public routes enforce authentication; modifying routes enforce role and event ownership. One enrolment per participant/event, one category name per event, one result per enrolment, and one finishing position per event are enforced. Categories implement the permitted distance-based option. All event wall times use South Africa (UTC+2).

Swagger at /swagger supplies executable requests, field schemas, operation summaries and error responses. The integration suite tests both authenticated and unauthenticated HTTP requests; xUnit controller tests cover domain outcomes in isolation from HTTP.
