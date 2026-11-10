# RaceDay API (Part 2)

![Part 2 API build and tests](https://github.com/UlikhayaMazibuko/RaceDay-PROG6212-ST10468748/actions/workflows/part2-api-ci.yml/badge.svg)

**Module:** Programming 2B (PROG6212)
**Student:** Ulikhaya A. Mazibuko (ST10468748)
**Part:** 2 of 3

---

## About this part

Part 2 implements the RaceDay RESTful API in C# with ASP.NET Core Web API. It is
built directly against the planning documents in Part 1: the database schema
matches the Part 1 ERD and SQL script, and the 29 endpoints match the Part 1 API
endpoint plan.

The API lets organisers create and manage running, walking and cycling events,
define the categories (distances) within each event, view who has entered and
capture results. Participants can browse events, enter a category, manage their
entries and view their personal race history.

## Technology

| Area | Choice |
|---|---|
| Framework | ASP.NET Core Web API on .NET 10 |
| Data access | Entity Framework Core 10 (Code-First) with SQL Server |
| Authentication | Server side session with an HttpOnly cookie |
| Password storage | ASP.NET Core `PasswordHasher` (PBKDF2 with a random salt) |
| API documentation | Swagger (Swashbuckle) with XML comments on every endpoint |
| Unit testing | xUnit with the EF Core InMemory provider |
| CI | GitHub Actions |

## Project structure

```
Part2/
├── Part2.slnx                  Solution containing both projects
├── RaceDay.Api/
│   ├── Controllers/            The 29 endpoints, grouped into 7 controllers
│   ├── Data/                   RaceDayDbContext with keys, constraints and seed data
│   ├── Dtos/                   Request and response shapes, kept separate from entities
│   ├── Migrations/             EF Core InitialCreate migration
│   ├── Models/                 The 7 entities from the Part 1 ERD
│   ├── Security/               Session keys, current user helpers and the RequireRole filter
│   └── Program.cs              Service registration and the request pipeline
└── RaceDay.Api.Tests/          36 unit tests
```

## Setup and running the API

### Prerequisites

- Visual Studio 2026 with the ASP.NET and web development workload
- .NET 10 SDK
- SQL Server Express (tested on SQL Server 2025 Express)

### Steps

1. Clone the repository and open `Part2/Part2.slnx` in Visual Studio.
2. Open `RaceDay.Api/appsettings.json` and check the `RaceDayDb` connection
   string. It points to `localhost\SQLEXPRESS01`. Change the server name if your
   SQL Server instance has a different name.
   ```json
   "ConnectionStrings": {
     "RaceDayDb": "Server=localhost\\SQLEXPRESS01;Database=RaceDayApiDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
   }
   ```
3. Create the database from the migration. Open
   **Tools > NuGet Package Manager > Package Manager Console**, set the default
   project to `RaceDay.Api`, and run:
   ```powershell
   Update-Database
   ```
   This creates the `RaceDayApiDb` database with all seven tables, and seeds the
   two roles (Organiser and Participant) and the three disciplines (Running,
   Walking and Cycling).
4. Set `RaceDay.Api` as the startup project and press **F5**.
5. Open `https://localhost:7015/swagger` in the browser.

The API database (`RaceDayApiDb`) is separate from the Part 1 script database
(`RaceDayDb`), so both can exist on the same machine.

### Trying it out in Swagger

1. `POST /api/auth/register` to create an account. Use `roleId` 1 for an
   Organiser or 2 for a Participant.
2. `POST /api/auth/login` with the same email and password. The session cookie
   is stored by the browser and sent automatically with every later request
   from the Swagger page.
3. Call the protected endpoints. `GET /api/auth/session` shows who is currently
   logged in.
4. `POST /api/auth/logout` ends the session.

To test both roles, register one organiser and one participant, and log in as
each in turn.

## Roles and access

| Role | What the role can do |
|---|---|
| **Organiser** | Create, update and delete their own events. Add, update and delete categories on their own events. View the start list (all enrolments) for their own events and update enrolment status. Capture and correct results after an event has taken place. |
| **Participant** | Browse published events and their categories. Enter a category, view their own enrolments and withdraw before race day. View their own results across all events. |
| **Not logged in** | Register, log in, browse published events, categories and disciplines, and view published event results. |

### How authentication and authorisation work

- **Password hashing.** Passwords are never stored in plain text. On
  registration the password is hashed with ASP.NET Core's `PasswordHasher`,
  which uses PBKDF2 with a random salt for each user. On login the submitted
  password is verified against the stored hash.
- **Session based authentication.** A successful login stores the user's id and
  role name in server side session state. The browser only holds a random
  session id in an HttpOnly cookie (`.RaceDay.Session`), which JavaScript cannot
  read. The session expires after 30 minutes of inactivity.
- **Role checks.** Protected endpoints carry a custom `[RequireRole]` filter. It
  returns **401 Unauthorized** when nobody is logged in and **403 Forbidden**
  when the logged in user has the wrong role.
- **Ownership checks.** Inside the controllers, an organiser can only change
  events, categories, enrolments and results that belong to their own events,
  and a participant can only see their own enrolments and results. Breaking
  either rule returns **403 Forbidden**.

## Endpoints

Every endpoint is described in Swagger with its purpose, input and possible
responses.

| # | Method | Route | Access |
|---|---|---|---|
| 1 | POST | `/api/auth/register` | Public |
| 2 | POST | `/api/auth/login` | Public |
| 3 | POST | `/api/auth/logout` | Logged in |
| 4 | GET | `/api/auth/session` | Logged in |
| 5 | GET | `/api/users/me` | Logged in |
| 6 | PUT | `/api/users/me` | Logged in |
| 7 | PUT | `/api/users/me/password` | Logged in |
| 8 | GET | `/api/disciplines` | Public |
| 9 | GET | `/api/events` | Public |
| 10 | GET | `/api/events/{id}` | Public |
| 11 | GET | `/api/events/mine` | Organiser |
| 12 | POST | `/api/events` | Organiser |
| 13 | PUT | `/api/events/{id}` | Organiser (owner) |
| 14 | DELETE | `/api/events/{id}` | Organiser (owner) |
| 15 | GET | `/api/events/{eventId}/categories` | Public |
| 16 | GET | `/api/categories/{id}` | Public |
| 17 | POST | `/api/events/{eventId}/categories` | Organiser (owner) |
| 18 | PUT | `/api/categories/{id}` | Organiser (owner) |
| 19 | DELETE | `/api/categories/{id}` | Organiser (owner) |
| 20 | POST | `/api/categories/{categoryId}/enrolments` | Participant |
| 21 | GET | `/api/enrolments/mine` | Participant |
| 22 | GET | `/api/enrolments/{id}` | Participant (own) or Organiser (owner) |
| 23 | GET | `/api/events/{eventId}/enrolments` | Organiser (owner) |
| 24 | PUT | `/api/enrolments/{id}/status` | Organiser (owner) |
| 25 | DELETE | `/api/enrolments/{id}` | Participant (own) |
| 26 | POST | `/api/enrolments/{enrolmentId}/result` | Organiser (owner) |
| 27 | PUT | `/api/results/{id}` | Organiser (owner) |
| 28 | GET | `/api/results/mine` | Participant |
| 29 | GET | `/api/events/{eventId}/results` | Public |

### Business rules enforced by the API

- An enrolment is refused with **409 Conflict** if the event is not published,
  entries have closed, the participant has already entered that category, the
  category is full, or the participant is under the category's minimum age on
  race day.
- Each enrolment receives a unique race number in the form `C{categoryId}-0001`.
- An event or category that already has enrolments cannot be deleted
  (**409 Conflict**). The organiser sets the event status to Cancelled instead.
- Results can only be captured after the event date. A `Finished` result needs
  a finish time and both positions, while `DNF`, `DNS` and `DQ` need none.

## Design decision: distance lives on categories

The brief describes events as having a distance. In this design the distance is
stored on each **event category** rather than on the event itself, because a
single real world event usually offers several distances (for example a 5 km
fun run and a 21.1 km half marathon on the same morning). Storing one distance
on the event would force organisers to create a separate event for each
distance.

To keep the distance easy to see, the event list response includes a
`distancesKm` field listing every distance offered, and the event detail
response lists each category with its `distanceKm`. This follows the Part 1 ERD,
where `DistanceKm` is a column of `EventCategories`.

## Unit tests

The `RaceDay.Api.Tests` project holds 36 xUnit tests that cover both successful
and failing cases. Each test runs against its own fresh EF Core InMemory
database and a fake in-memory session, so the tests never touch SQL Server and
can run on the CI server.

| Test class | Tests | What it covers |
|---|---|---|
| `AuthControllerTests` | 8 | Registration (password stored as a hash), duplicate email, invalid role, login storing the session, wrong password, unknown email, logout clearing the session |
| `RoleAccessTests` | 11 | Unauthenticated requests rejected with 401, participants rejected from organiser endpoints and organisers rejected from participant endpoints with 403, correct roles allowed through |
| `EventsControllerTests` | 9 | Organisers creating, updating and deleting events, date validation, ownership (403), missing events (404), deleting an event with enrolments (409), drafts hidden from the public list |
| `EnrolmentsControllerTests` | 8 | Participants enrolling and the enrolment being recorded against the right participant, category and event, duplicate, full, closed and under age entries (409), participants only seeing their own enrolments |

To run them in Visual Studio, open **Test > Test Explorer** and click
**Run All**. To run them from a terminal in the `Part2` folder:

```powershell
dotnet test Part2.slnx
```

## CI/CD

The workflow at `.github/workflows/part2-api-ci.yml` runs on every push and pull
request. It installs the .NET 10 SDK on a fresh Ubuntu machine, restores the
NuGet packages, builds both projects in Release mode and runs all the unit
tests. The build fails if any test fails.

The Part 1 workflow (`validate-docs.yml`) still runs alongside it.

**Green build screenshot:**

<!-- CI screenshot goes here -->
