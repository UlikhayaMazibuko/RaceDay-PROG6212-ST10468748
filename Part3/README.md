# RaceDay Web (Part 3)

**Module:** Programming 2B (PROG6212)
**Student:** Ulikhaya A. Mazibuko (ST10468748)
**Part:** 3 of 3

---

## About this part

Part 3 is the RaceDay website: an ASP.NET Core MVC application that consumes the
Part 2 RESTful API for every data operation. It gives Organisers and Participants
their own interface, stores event banners and profile pictures in Azure Blob
Storage through the API, and runs in a Docker container.

```
Browser  →  RaceDay.Web (MVC, Part 3)  →  RaceDay.Api (Part 2)  →  SQL Server
                                                 ↓
                                        Azure Blob Storage (images)
```

The MVC project has **no database access and no business logic**. Every page calls
the API through one class, `Services/ApiClient.cs`. Rules such as capacity limits,
closing dates, minimum ages and ownership are enforced by the API, and the website
shows the API's message to the user in a friendly way.

## Project structure

```
Part3/
├── Part3.slnx
└── RaceDay.Web/
    ├── Controllers/        Account, Home, Participant, Organiser, Profile
    ├── Filters/            RequireLogin filter for protected pages and role checks
    ├── Models/             API data shapes and page view models
    ├── Services/           ApiClient (all API calls), session keys, display helpers
    ├── Views/              Razor pages for each role, shared layout and partials
    ├── wwwroot/css/        RaceDay theme
    └── Dockerfile          Multi-stage Docker build
```

## Roles and pages

**Participant**
- **Events (home):** all upcoming events, filtered by city, event type and date
- **Event detail:** full event information, categories with places left, and an
  **Enter event** button for each category
- **My events:** every event entered, with race number and colour-coded status,
  and the option to withdraw before race day
- **My results:** race history with finish time and position, for example
  *47th out of 312 finishers*
- **Profile:** view and update personal details and upload a profile picture

**Organiser**
- **Dashboard:** summary tiles (events created, total enrolments, upcoming events,
  next race day) and a table of the organiser's events with enrolments per event
- **New / edit event:** name, description, date, start time, venue, city, province,
  event type, distance (first category) and a banner image upload
- **Categories:** add and remove age or distance categories for an event
- **Enrolments:** every participant entered, with category, race number and status,
  which the organiser can change (Pending, Confirmed, Cancelled)
- **Results entry:** capture finish times and positions, or DNF, DNS and DQ, after
  the event has taken place

Both roles have their own navigation menu. Visitors who are not logged in can
browse events but are redirected to the login page for any protected page, and a
logged in user who opens the other role's page is sent back to their own home page.

### Colour-coded statuses

| Colour | Statuses |
|---|---|
| Green | Confirmed, Published, Finished |
| Amber | Pending, Draft |
| Red | Cancelled, DQ, Full |
| Navy | Completed |

## How login works across the two applications

1. The login form posts to the MVC app, which calls `POST /api/auth/login`.
2. The API checks the hashed password and starts its own server side session.
3. The MVC app stores the user's id, role and name in **its own session**, plus a
   copy of the API's session cookie.
4. Every later API call from the MVC app sends that cookie, so the API knows which
   user is calling and enforces their role.
5. Logging out calls `POST /api/auth/logout` and clears the MVC session.

## Azure Blob Storage

- Organisers upload a banner on the New or Edit event page. Users upload a profile
  picture on the Profile page.
- The MVC app sends the file to the API (`POST /api/events/{eventId}/banner` and
  `POST /api/users/me/picture`). It never talks to Azure directly.
- The API stores the file in the `raceday-images` container, saves the public URL in
  the database (`Events.BannerImageUrl`, `Users.ProfilePictureUrl`) and returns it.
- The MVC app shows the image from that URL on the event cards, the event detail
  page and the profile page.

## Setup and running

### Prerequisites

- Visual Studio 2026, .NET 10 SDK and SQL Server Express
- An Azure Storage Account (for image uploads)
- Docker Desktop (for running the site in a container)

### 1. Start the API (Part 2)

1. Open `Part2/Part2.slnx`.
2. Right-click **RaceDay.Api**, choose **Manage User Secrets**, and add the Azure
   connection string. It is kept out of the repository on purpose.
   ```json
   {
     "AzureStorage": {
       "ConnectionString": "<your Azure Storage connection string>"
     }
   }
   ```
3. In the Package Manager Console, run `Update-Database`. This applies the
   `AddImageUrls` migration as well as the original schema.
4. Press **F5**. The API runs on `https://localhost:7015` and `http://localhost:5106`.

### 2. Run the website locally

1. Open `Part3/Part3.slnx` in a second Visual Studio window.
2. Press **F5**. The site opens on `https://localhost:7120`.

The API address is set in `RaceDay.Web/appsettings.json` under `Api:BaseUrl`.

### 3. Run the website in Docker

With the API still running, open a terminal in the `Part3` folder and build the image:

```powershell
docker build -t raceday-web ./RaceDay.Web
```

Then start the site with a single command:

```powershell
docker run -d -p 8080:8080 -e Api__BaseUrl=http://host.docker.internal:5106/ --name raceday-web raceday-web
```

Open `http://localhost:8080`. Inside the container, `host.docker.internal` points
back to your computer, which is where the API is running. The container behaves
the same as the local site because all data still comes from the same API.

To stop and remove the container:

```powershell
docker rm -f raceday-web
```

## CI/CD

The workflow at `.github/workflows/part2-api-ci.yml` runs on every push and pull
request with two jobs:

| Job | What it does |
|---|---|
| Part 2 API build and unit tests | Builds the API and runs all unit tests |
| Part 3 MVC build and Docker image | Builds the MVC app and builds its Docker image |

**Green build screenshot:**

<!-- Part 3 CI screenshot goes here -->

## AI usage

AI tools (Claude) were used to assist with planning and code during this project.
All code was reviewed, run and tested by me before being committed.
