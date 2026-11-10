# RaceDay MVC (Part 3)

ASP.NET Core 8 MVC front end for the RaceDay platform. It talks to the Part 2 REST API for **all** data and file
handling. It has no database access, no business logic and no Azure SDK.

## Run with Docker (single command)

The Part 2 API must already be running and reachable. Replace the URL with wherever your API is hosted.

```bash
docker build -t raceday-mvc . && docker run --rm -p 8080:8080 --add-host=host.docker.internal:host-gateway -e Api__BaseUrl=http://host.docker.internal:5000/ raceday-mvc
```

Then open http://localhost:8080.

- `Api__BaseUrl` is the only setting the container needs. Use `http://host.docker.internal:<port>/` for an API running on
  your own machine, or the public URL of a hosted API.
- The `--add-host` flag makes `host.docker.internal` work on Linux. It is harmless on Windows and macOS.

## Run locally (no Docker)

1. Open `appsettings.json` and set `Api:BaseUrl` to your API's address (keep the trailing slash).
2. `dotnet run`
3. Open the URL printed in the terminal.

The container and the local run behave the same. Only the base URL differs, and it comes from configuration in both cases.

## How it works

| Requirement | Where |
|---|---|
| MVC consumes the API only | `Services/RaceDayApi.cs` (typed `HttpClient`) |
| Session auth + role | `Infrastructure/SessionKeys.cs`, `SessionAuthorizeAttribute.cs`, `BearerTokenHandler.cs` |
| Separate role interfaces | `_OrganiserLayout.cshtml` (sidebar console), `_ParticipantLayout.cshtml` (top nav) |
| Protected pages | `[SessionAuthorize(Role = ...)]` on `OrganiserController` and `ParticipantController` |
| Logout | `POST /Account/Logout` clears the session and redirects to Login |
| Friendly errors | `ApiExceptionFilter`, `/Home/Error`, per-form validation messages |
| Status colours | `.rd-status-confirmed` (green), `.rd-status-pending` (amber) in `wwwroot/css/site.css` |

### Pages

**Organiser:** Dashboard, My events (create, edit, delete, banner upload), Categories, Enrolments, Results entry.
**Participant:** Find events (filters), Event details + enrol, My enrolments, My results, Profile (picture upload).

## Connecting to your Part 2 API

Every URL is in one place: `ApiRoutes` in `Services/RaceDayApi.cs`. Every JSON shape is in `Models/ApiModels.cs`
(property matching is case-insensitive). If your API differs, change those two files and nothing else.

Expected endpoints:

| Purpose | Method + route |
|---|---|
| Register / login | `POST api/auth/register`, `POST api/auth/login` (returns token, userId, fullName, email, role) |
| Events | `GET api/events`, `GET api/events/mine`, `GET/PUT/DELETE api/events/{id}`, `POST api/events` (multipart, field `Banner`) |
| Categories | `POST api/events/{id}/categories`, `DELETE api/events/{id}/categories/{categoryId}` |
| Enrolments | `POST api/enrolments`, `GET api/enrolments/mine`, `GET api/events/{id}/enrolments` |
| Results | `POST api/events/{id}/results`, `GET api/results/mine` |
| Profile | `GET api/profile`, `PUT api/profile` (multipart, field `ProfilePicture`) |

Azure Blob Storage is added to the API, not to this project. See `docs/PART2_API_CHANGES.md`.

## Configuration

| Key | Purpose |
|---|---|
| `Api:BaseUrl` (env `Api__BaseUrl`) | Base address of the Part 2 API |

No secrets live in this project. Azure connection strings belong to the API (user-secrets or environment variables).
