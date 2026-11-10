# API routes

Prefix: `/api`. Send `Authorization: Bearer <token>` for protected routes. Register/login return `{ token, expiresAt, user }`. Development Swagger: `/swagger`. Health: `/health`.

| Method | Route | Role / purpose |
|---|---|---|
| POST | /auth/register | Public: email, password, firstName, lastName, gender, role (Organiser or Participant) |
| POST | /auth/login | Public: email, password |
| POST | /auth/logout | Authenticated: revoke current bearer token |
| GET / PUT | /profile | Authenticated: own profile; PUT firstName, lastName, gender, phoneNumber |
| POST | /profile/image | Authenticated: multipart file |
| GET | /events | Public; search, type, upcoming=true; mine=true requires Organiser |
| GET | /events/{id} | Public event and categories |
| POST | /events | Organiser: name, description, location, date, distance, eventType |
| PUT / DELETE | /events/{id} | Owning organiser |
| POST | /events/{id}/banner | Owning organiser: multipart file |
| POST | /events/{id}/categories | Owning organiser: name, description |
| DELETE | /events/{id}/categories/{categoryId} | Owning organiser; only if no entries |
| POST | /events/{id}/enrolments | Participant: categoryId |
| GET | /events/{id}/enrolments | Owning organiser: start list |
| PUT | /events/{id}/enrolments/{enrolmentId}/result | Owning organiser: finishTime (HH:mm:ss), position; event must have passed |
| GET | /me/enrolments | Participant: own entries including published results |
| GET | /media/{random-name.png-or-jpg} | Public image bytes from API-owned storage |

Example event:
```json
{"name":"City 10K","description":"A road race for all abilities.","location":"Johannesburg","date":"2027-02-14T05:00:00Z","distance":10,"eventType":"Running"}
```

Event types: Running, Walking, Cycling. Gender: Male, Female, Other, PreferNotToSay.
Upload limits: 2 MB PNG or JPEG. Multipart field name: `file`. Images return as web-relative `/Media/{name}` URLs, intentionally routed through the MVC proxy.
