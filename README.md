# RaceDay

RaceDay is a full-stack event management system for the South African road running, walking and cycling community.
Organisers create and manage events, categories and results; Participants browse events, enrol, and view their own results.

This repository contains **Part 1 (planning + database)** and **Part 2 (RESTful API + unit tests + CI/CD)**.

Student: _your name / student number_ | Repository: _your GitHub URL_

## Repository structure

```
RaceDay/
├── RaceDay.sln                         Open this in Visual Studio 2022
├── docs/
│   ├── RaceDay_ERD.png / .pdf          Section A - Entity Relationship Diagram
│   ├── RaceDay_API_Endpoint_Plan.docx  Section B - API endpoint plan (also as .md)
│   └── RaceDay_Database.sql            Section C - SQL Server script (schema + seed data)
├── src/RaceDay.Api/                    ASP.NET Core 8 Web API (EF Core code-first, JWT, Swagger)
├── tests/RaceDay.Tests/                xUnit tests
└── .github/workflows/ci.yml            GitHub Actions: build + test on every push / pull request
```

## Getting started (Visual Studio 2022)

Requirements: Visual Studio 2022 (17.8 or later) with the **ASP.NET and web development** workload, the .NET 8 SDK,
SQL Server LocalDB (installed with Visual Studio) and SQL Server Management Studio (SSMS).

1. Open `RaceDay.sln`. Visual Studio restores the NuGet packages automatically.
2. Create the database - choose **one** option:
   * **Option A - SQL script:** in SSMS connect to `(localdb)\MSSQLLocalDB`, open `docs/RaceDay_Database.sql` and click
     **Execute**. It creates the `RaceDay` database, all seven tables and the seed data. The API works immediately; do not
     run migrations in this case.
   * **Option B - EF Core code-first:** open *Tools > NuGet Package Manager > Package Manager Console*, set
     **Default project** to `RaceDay.Api` and run `Add-Migration InitialCreate` then `Update-Database`. Run the INSERT
     section of the script afterwards if you want the sample data.
3. The connection string is `ConnectionStrings:DefaultConnection` in `src/RaceDay.Api/appsettings.json` and points to
   `(localdb)\MSSQLLocalDB`, database `RaceDay`. For SQL Express use
   `Server=.\SQLEXPRESS;Database=RaceDay;Trusted_Connection=True;TrustServerCertificate=True`.
4. Set `RaceDay.Api` as the startup project and press **F5**. Swagger UI opens at `/swagger`.

### Trying the API in Swagger

1. Call `POST /api/auth/login` with a seeded account (password for all seeded users: `Password123!`).
2. Copy the `token` from the response, click **Authorize** (top right) and paste it.
3. Every endpoint can now be tested from the browser.

| Role        | Email             |
|-------------|-------------------|
| Organiser   | thabo@gmail.com   |
| Organiser   | sarah@gmail.com   |
| Participant | liam@gmail.com    |
| Participant | amahle@gmail.com  |
| Participant | jason@gmail.com   |
| Participant | naledi@gmail.com  |

## Database

The schema is built on the supplied `Raceday.sql`: Organisers, Participants, Events, Categories, EventCategories and
Enrollments keep their table and column names. Four additions were needed to meet the brief, and the ERD, SQL script, EF Core
model and endpoint plan all include them:

* `PasswordHash` on Organisers and Participants - registration/login with hashed passwords.
* `Description` and `EventType` (Run, Walk or Cycle) on Events - required event fields.
* `CategoryID` on Enrollments - an enrollment records the participant, the event and the selected category.
* A new `Results` table (finish time and finishing position, one per enrollment).

The EF Core model (`Data/RaceDayDbContext.cs`) uses the same table names, column types, keys, unique constraints, check
constraints and defaults as the script. The only cosmetic difference: the script declares UNIQUE constraints while EF Core
creates unique indexes with the same names and behaviour. Constraints are named explicitly in the script.

## Running the unit tests

* Visual Studio: **Test > Run All Tests** (Test Explorer), or from a terminal: `dotnet test RaceDay.sln`.
* The tests host the real API in memory with an isolated EF Core in-memory database, so no SQL Server is needed (which is
  also why they run on GitHub Actions).
* They cover registration and login, event management by Organisers, role-based access (Participant vs Organiser vs
  anonymous), enrollments (including that the participant/event/category link is recorded), results and the profile
  endpoints - both success and failure scenarios.
* GitHub Actions (`.github/workflows/ci.yml`) restores, builds and runs all tests on every push and pull request.

## Design decisions

* **Two user tables, one login:** Organisers and Participants live in separate tables. An email can exist only once across
  both, and `POST /api/auth/login` finds the account in either table. The token carries the user id and the role.
* **Role management:** a user chooses Organiser or Participant when registering; any other role (for example Admin) is
  rejected, so elevated roles cannot be self-assigned.
* **Session management:** the API is stateless; login returns a signed JWT that is sent as `Authorization: Bearer {token}`.
  Missing or invalid token = 401, wrong role or not the owner of the resource = 403.
* **Password security:** passwords are hashed with ASP.NET Core Identity's `PasswordHasher` (salted PBKDF2-HMAC-SHA256);
  the plain-text password is never stored.
* **Categories:** categories are shared by name. `POST /api/events/{eventId}/categories` reuses a category with that name or
  creates it, then assigns it to the event through the EventCategories table.
* **Ownership rules:** an Organiser can only change events, categories, enrollment lists and results of events they created.
* **Business rules:** one enrollment per participant per event; the chosen category must be assigned to the event; a unique
  bib number is issued per enrollment; finishing positions are unique within an event; a category with enrollments cannot be
  removed from an event; past events cannot be enrolled in.
* **Swagger descriptions:** every endpoint's purpose, expected data and responses are declared with Swagger annotation
  attributes, so the source code contains no comments.
* **JWT key:** `Jwt:Key` in `appsettings.json` is a development value. Replace it (user-secrets or an environment variable)
  for any real deployment.

## API summary

24 endpoints, planned in `docs/RaceDay_API_Endpoint_Plan.docx` and visible in Swagger: `/api/auth`, `/api/users/me`,
`/api/events`, `/api/categories`, `/api/events/{id}/categories`, `/api/events/{id}/enrollments`, `/api/enrollments`,
`/api/enrollments/{id}/results`, `/api/results`.
