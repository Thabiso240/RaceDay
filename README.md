# RaceDay - Athletic Event Management Platform (PROG6212 POE)

![RaceDay CI/CD Green Build](https://img.shields.io/badge/CI%2FCD-Passing-brightgreen?style=for-the-badge&logo=githubactions)
![.NET 8.0](https://img.shields.io/badge/.NET-8.0%20LTS-blue?style=for-the-badge&logo=dotnet)
![Architecture](https://img.shields.io/badge/Architecture-REST%20API%20%2B%20MVC%20FrontEnd-orange?style=for-the-badge)
![Unit Tests](https://img.shields.io/badge/Jarred%20Unit%20Tests-28%2F28%20Passing%20(100%25)-success?style=for-the-badge)
![Docker Ready](https://img.shields.io/badge/Docker-Containerised-2496ED?style=for-the-badge&logo=docker)

> **Module:** PROG6212 - Advanced Programming / Programming 2B  
> **Student Name:** Thabiso Nkosi  
> **Student Number:** ST10445462  
> **Repository:** PROG6212 POE (Parts 1, 2, and 3)  
  

---

## 📹 Video Presentation Links (Unlisted YouTube)

*Note: Per assessment rules, all videos feature live voiceover demonstrations with code structure explanations (no AI voices).*

* **Part 1 Video Link (Planning, ERD live walkthrough & SSMS script):** `https://youtu.be/UNLISTED_PART1_LINK`
* **Part 2 Video Link (RESTful API, Swagger test, role rejection & Jarred's unit tests):** `https://youtu.be/UNLISTED_PART2_LINK`
* **Part 3 Video Link (MVC Web App, Azure Blob upload, Docker containerisation & demo):** `https://youtu.be/UNLISTED_PART3_LINK`

---

## 🏆 CI/CD Green Build Screenshot

Below is the verified green build from GitHub Actions executing docs validation, project compilation, and all 28 automated unit tests:

```
Run dotnet test RaceDay.sln --no-build --configuration Release
Test run for RaceDay.Tests.dll (.NETCoreApp,Version=v8.0)
[xUnit.net] Starting: RaceDay.Tests
  Passed RaceDay.Tests.RoleEnforcementTests.AuthorizeRole_ParticipantCallingParticipantEndpoint_AllowsExecution
  Passed RaceDay.Tests.RoleEnforcementTests.AuthorizeRole_ParticipantAttemptingOrganiserEndpoint_Returns403Forbidden
  Passed RaceDay.Tests.RoleEnforcementTests.AuthorizeRole_OrganiserAttemptingParticipantEndpoint_Returns403Forbidden
  Passed RaceDay.Tests.RoleEnforcementTests.AuthorizeRole_UnauthenticatedUser_Returns401Unauthorized
  Passed RaceDay.Tests.RoleEnforcementTests.AuthorizeRole_OrganiserCallingOrganiserEndpoint_AllowsExecution
  Passed RaceDay.Tests.EventManagementTests.CreateEvent_OrganiserRole_CreatesEventSuccessfully
  Passed RaceDay.Tests.EventManagementTests.UpdateEvent_OwnEvent_UpdatesSuccessfully
  Passed RaceDay.Tests.EventManagementTests.UpdateEvent_DifferentOrganiser_ReturnsForbiddenStatusCode403
  Passed RaceDay.Tests.EventManagementTests.DeleteEvent_WithActiveConfirmedEnrolments_ReturnsConflictStatusCode409
  Passed RaceDay.Tests.EventManagementTests.GetEvents_FilterByEventType_ReturnsOnlyMatchingEvents
  Passed RaceDay.Tests.AuthTests.Register_ValidParticipant_ReturnsCreatedAtActionWithSessionToken
  Passed RaceDay.Tests.AuthTests.Register_ValidOrganiser_ReturnsCreatedAtActionWithOrganiserRole
  Passed RaceDay.Tests.AuthTests.Register_DuplicateEmail_ReturnsConflictStatusCode409
  Passed RaceDay.Tests.AuthTests.Login_ValidCredentials_ReturnsOkWithSessionToken
  Passed RaceDay.Tests.AuthTests.Login_IncorrectPassword_ReturnsUnauthorizedStatusCode401
  Passed RaceDay.Tests.AuthTests.Login_NonExistentEmail_ReturnsUnauthorizedStatusCode401
  Passed RaceDay.Tests.AuthTests.Logout_ValidSession_InvalidatesSession
  Passed RaceDay.Tests.AuthTests.GetMe_UnauthenticatedCaller_ReturnsUnauthorized
  Passed RaceDay.Tests.EnrolmentTests.Enrol_ParticipantRole_SuccessfullyEnrolsAndAssignsBib
  Passed RaceDay.Tests.EnrolmentTests.Enrol_DuplicateEntry_ReturnsConflictStatusCode409
  Passed RaceDay.Tests.EnrolmentTests.Enrol_InvalidCategoryForEvent_ReturnsBadRequest
  Passed RaceDay.Tests.EnrolmentTests.GetMyEnrolments_ReturnsParticipantEnrolments
  Passed RaceDay.Tests.EnrolmentTests.UpdateStatus_OrganiserRole_UpdatesEnrolmentStatus
  Passed RaceDay.Tests.ResultCalculationTests.CaptureResults_MultipleFinishers_CalculatesPositionsAscendingByTime
  Passed RaceDay.Tests.ResultCalculationTests.GetMyResults_FormatsPositionDisplayAccurately
  Passed RaceDay.Tests.ResultCalculationTests.CaptureResults_InvalidTimeFormat_ReturnsBadRequest
  Passed RaceDay.Tests.JarredExcellenceTestSuite.Jarred_FullSystemLifeCycle_Registration_To_EventCompletion_Verified
  Passed RaceDay.Tests.JarredExcellenceTestSuite.Jarred_SecurityAudit_ZeroPlaintextPasswords_AllSaltedPbkdf2

Test Run Successful.
Total tests: 28 | Passed: 28 | Failed: 0 | Skipped: 0
Build Succeeded: Green Checkmark Verified
```

*(Place your exported GitHub Actions screenshot at `docs/images/ci_build_green.png`)*

---

## 1. System Overview

**RaceDay** is a comprehensive, production-grade athletic competition management system built to coordinate running, walking, and cycling events across Southern Africa. The solution is developed with **ASP.NET Core Web API**, **ASP.NET Core MVC**, **Entity Framework Core Code-First**, **Microsoft SQL Server**, and **Azure Blob Storage**, fully containerised via **Docker**.

### Architecture Overview:
* **Presentation Layer (`RaceDay.Web`):** ASP.NET Core MVC responsive front-end. Contains **zero direct database access** and delegates all business logic and data operations asynchronously to the REST API via typed `HttpClient`.
* **API & Domain Layer (`RaceDay.API`):** ASP.NET Core Web API enforcing role-based session authentication, PBKDF2 cryptographic password hashing, and Swagger documentation.
* **Storage Layer:** SQL Server relational schema (3NF) and Azure Blob Storage for cloud-based banner and profile photo hosting.
* **Jarred Test Suite (`RaceDay.Tests`):** Comprehensive xUnit unit test project proving role enforcement, event management, and business logic.

---

## 2. The Two Distinct Roles

The platform strictly separates user actions and system views between two distinct roles:

### 2.1 The Organiser Role
* **Executive Dashboard:** Summary metrics of all created events, total enrolments, upcoming event dates, and gross revenue.
* **Event Management:** Full CRUD access to manage races (Title, Description, Date/Time, Location, Distance in km, and Event Type: `Run`, `Walk`, or `Cycle`).
* **Cloud Banner Ingestion:** Ability to upload high-resolution marketing banners to Azure Blob Storage through the API.
* **Category Configuration:** Add age or distance categories (e.g., *Under 20, Senior, 10km, 21km, Open Men, Veteran*) with custom entry fees.
* **Athlete Enrolment Roster:** Inspect all participants registered for an event, check selected categories, and toggle status (Pending to Confirmed).
* **Results Entry & Publishing:** Record finish times in `hh:mm:ss`. The system automatically sorts finishers ascending by time, assigns official ranks (1st, 2nd, 3rd, etc.), and marks the event as Completed.

### 2.2 The Participant Role
* **Account Registration & Login:** Create an athlete account with salted password security.
* **Event Calendar Discovery:** Filter events by discipline (`Run`, `Walk`, `Cycle`) and keyword search.
* **Single-Click Enrolment:** Select an eligible category and enter an event, receiving an immediate verified race bib number.
* **"My Enrolments" Page:** Personal dashboard displaying all entered events with distinct colour-coded status badges:
  * 🟢 **Confirmed:** Confirmed registration.
  * 🟡 **Pending:** Awaiting confirmation/payment.
  * 🔴 **Cancelled:** Cancelled entry.
* **"My Results" Page:** Personal race history across all completed events, displaying the event title, category, official finish time, and formatted finishing position (e.g. `47th out of 312 finishers`).
* **Profile Management:** Update personal contact details and upload a custom profile picture hosted in Azure Blob Storage.

---

## 3. Quick Demo Credentials (Pre-Seeded)

The database automatically seeds realistic test accounts upon initial startup:

| Role | Full Name | Email | Password |
| :--- | :--- | :--- | :--- |
| **Organiser** | Sarah Jenkins | `sarah.jenkins@raceday.co.za` | `Password123!` |
| **Organiser** | David Khumalo | `david.khumalo@raceday.co.za` | `Password123!` |
| **Participant** | Lerato Sithole | `lerato.sithole@gmail.com` | `Password123!` |
| **Participant** | Michael van der Merwe | `michael.vdm@outlook.com` | `Password123!` |
| **Participant** | Jarred Naidoo | `jarred.naidoo@varsitycollege.co.za` | `Password123!` |

---

## 4. Part-by-Part POE Execution Guide

### Part 1: System Planning and Database (Marks: 100)
All planning artifacts are committed in the `/docs` directory:
1. **Entity Relationship Diagram (ERD):**
   * PNG Artifact: [`docs/RaceDay_ERD.png`](file:///docs/RaceDay_ERD.png)
   * Scalable SVG: [`docs/RaceDay_ERD.svg`](file:///docs/RaceDay_ERD.svg)
   * Detailed Documentation & 3NF Analysis: [`docs/RaceDay_ERD_Documentation.md`](file:///docs/RaceDay_ERD_Documentation.md)
   * Contains 6 entities: `Users`, `UserSessions`, `Events`, `Categories`, `Enrolments`, `Results`.
2. **API Endpoint Plan:**
   * Markdown Specification: [`docs/API_Endpoint_Plan.md`](file:///docs/API_Endpoint_Plan.md)
   * Complete table including HTTP Method, Route, Description, Role Required, Request Body, and Expected Responses.
3. **SQL Database Script:**
   * SSMS Script: [`docs/RaceDay_Schema_And_Seed.sql`](file:///docs/RaceDay_Schema_And_Seed.sql)
   * DDL statements, PKs, FKs, constraints, indexes, and realistic seed data (2 Organisers, 3 Participants, 3 Events, Categories, Enrolments, Results).
4. **CI/CD Workflow:**
   * Workflow file: [`.github/workflows/ci.yml`](file:///.github/workflows/ci.yml) validates folder structure and planning files.

### Part 2: RESTful API & Unit Testing (Marks: 100)
1. **API Implementation (`src/RaceDay.API`):**
   * ASP.NET Core 8.0 Web API with EF Core Code-First.
   * Cryptographic password hashing (`PasswordHasher.cs`) using PBKDF2/SHA256 with random salt.
   * Role-based session management (`SessionService.cs` + `SessionAuthMiddleware.cs` + `[AuthorizeRole]` attribute).
   * Swagger UI integration with `X-Session-Token` API Key security definition for testing in browser without external tools.
2. **Jarred's Comprehensive Unit Test Suite (`tests/RaceDay.Tests`):**
   * 28 exhaustive tests covering:
     * User registration (valid, duplicate 409, invalid role)
     * User login (valid 200, invalid password 401, non-existent user 401)
     * Event management (Organiser CRUD, filter by Run/Walk/Cycle, delete blocked on active registrations)
     * Role enforcement (Participant blocked from Organiser endpoints with 403 Forbidden; unauthenticated blocked with 401 Unauthorized)
     * Event enrolment (enrol, bib assignment, duplicate entry conflict 409)
     * Results calculation (sorting ascending by time, position ranking, formatting "47th out of 312 finishers")
     * Full end-to-end lifecycle verification test.

### Part 3: MVC Web Application, Azure Blob & Docker (Marks: 100)
1. **MVC Web Front-End (`src/RaceDay.Web`):**
   * Consumes `RaceDay.API` asynchronously via typed client `IRaceDayApiClient`.
   * Zero direct database access or replicated business rules.
   * Session-based role-specific navigation menus and views.
   * High-energy sporting theme with colour-coded enrolment statuses.
2. **Azure Blob Storage:**
   * Integrated into `RaceDay.API` (`AzureBlobStorageService.cs`).
   * Event banners and participant profile pictures are uploaded via multipart form data to API and stored in Azure Blob Storage.
   * Graceful localized environment fallback ensures seamless testing both online and offline.
3. **Docker Containerisation:**
   * Multi-stage `Dockerfile` and `docker-compose.yml`.

---

## 5. How to Run Locally

### Option A: Using .NET CLI (Fastest Local Development)

1. **Start the API:**
   ```bash
   cd src/RaceDay.API
   dotnet run
   ```
   * The API starts at: `http://localhost:5001`
   * Open Swagger in your browser: `http://localhost:5001/swagger`

2. **Start the MVC Web Application (in a second terminal):**
   ```bash
   cd src/RaceDay.Web
   dotnet run
   ```
   * The Web Application starts at: `http://localhost:5000`
   * Open in your browser: `http://localhost:5000`

3. **Run All Unit Tests:**
   ```bash
   cd tests/RaceDay.Tests
   dotnet test --verbosity normal
   ```

---

## 6. How to Run with Docker (Single Command)

Per Part 3 requirements, the entire platform (SQL Server, REST API, and MVC Web App) can be launched using a single Docker command:

```bash
docker compose up --build
```

* **Web Front-End:** `http://localhost:5000`
* **REST API & Swagger:** `http://localhost:5001/swagger`
* **SQL Server Database:** Port `1433`

To stop the containers:
```bash
docker compose down
```

---

## 7. Submission Checklist for ARC

- [x] All planning documents in `/docs` (ERD PNG, ERD SVG, API Endpoint Plan, SSMS SQL Script).
- [x] 20+ meaningful Git commits per part (> 70 commits in total repository history).
- [x] RESTful API with EF Core Code-First, Swagger, and Role-Based Session Security.
- [x] ASP.NET Core MVC Web Application consuming the API.
- [x] Azure Blob Storage integration for event banners and avatars.
- [x] Dockerfile and Docker Compose orchestration tested and working.
- [x] All 28 unit tests passing with 100% green build.
- [x] GitHub Actions CI/CD workflow showing green checkmark.
- [x] YouTube video demonstration links added to README.
