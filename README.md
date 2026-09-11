# RaceDay

**Module:** Programming 2B (PROG6212)
**Student:** Ulikhaya A. Mazibuko (ST10468748)
**Part:** 1 of 3

---

## About the system

RaceDay is a web based event management system for the South African road
running, walking and cycling community. Many local events are still run off
paper entry forms and spreadsheets, which leaves organisers doing admin by hand
and participants with no reliable record of what they have entered or how they
finished.

The platform gives organisers one place to create events, define the distances
on offer, see who has entered and capture results after the race. Participants
can browse upcoming events, enter by choosing a category, see everything they
have entered, and keep a personal race history across every event they have
completed.

This repository covers Part 1, which is the planning stage. No application code
is written here. Part 2 builds the RESTful API in C# against the plan in this
repository and Part 3 adds the MVC front end, Azure Blob Storage and Docker.

## The two roles

**Organiser**
Creates, edits and deletes their own events. Defines the age or distance
categories within each event. Views the full list of participants entered into
their events. Captures finish times and finishing positions once an event has
been run.

**Participant**
Creates an account and browses published events. Enters an event by selecting a
category. Views their own entries and their status. Tracks their personal race
history, including finish time and finishing position, across all completed
events.

Role based access is enforced at the API level in Part 2 and reflected in the
MVC navigation in Part 3. An organiser can only modify events they created
themselves, and a participant can only see their own entries and results.

## The planning documents

### Entity Relationship Diagram - `docs/raceday_erd`

Seven entities: `Roles`, `Disciplines`, `Users`, `Events`, `EventCategories`,
`Enrolments` and `Results`. Drawn in Information Engineering notation with
min..max cardinality and a named verb phrase on every relationship.

Two design points worth noting:

Both roles live in one `Users` table, distinguished by `RoleId`. Splitting them
into separate tables would mean duplicating every login and contact column, and
would make `Results` awkward, since it has to reference an organiser while
`Enrolments` references a participant.

`Enrolments` resolves the many to many relationship between `Users` and
`EventCategories`. Because a category belongs to exactly one event, a single
enrolment row records the participant, the event and the chosen category
together.

### API endpoint plan - `docs/PROG6212_Part1_SectionB_API_Endpoint_Plan_ST10468748`

Twenty nine endpoints across seven resource groups, each with its HTTP method,
route, description, required role, request body and expected response including
failure status codes. The API built in Part 2 is implemented against this
specification.

### SQL script - `docs/raceday_schema.sql`

Creates the full schema and seeds it with realistic sample data: two organisers,
four participants, three events across the three disciplines, categories for
every event, eight enrolments and four results.

The script is safe to run more than once. It drops the tables in reverse
dependency order before rebuilding them, so re-running it gives a clean database
rather than duplicate rows.

## Running the SQL script

1. Open SQL Server Management Studio and connect to your local instance.
2. Open `docs/raceday_schema.sql`.
3. Execute the whole script with F5.

The script creates the `RaceDayDb` database itself, so no database needs to
exist beforehand. The last two queries are verification rather than schema: the
first prints a row count for every table, and the second joins `Users` through
`Enrolments`, `EventCategories` and `Events` out to `Results` to confirm the
relationships work end to end.

Tested on SQL Server 2025 Express.

## Notes on the ERD and the script matching

The SQL script matches the ERD exactly. Every table, column, key and constraint
on the diagram has a direct equivalent in the script, and there are no
deliberate differences to explain.

## CI/CD

The workflow at `.github/workflows/validate-docs.yml` runs on every push. It
checks that the `/docs` folder exists and holds all three deliverables, that the
SQL script creates all seven tables from the ERD, that keys and constraints are
defined, that seed data is present, and that this README describes both roles.

**Green build screenshot:**

<img width="1897" height="967" alt="ci-green-build" src="https://github.com/user-attachments/assets/e0cf3a69-04f5-4b4e-b250-3ac741ea2892" />


