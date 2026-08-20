# RaceDay Platform - RESTful API Endpoint Plan

**Module:** PROG6212 - Advanced Programming / Programming 2B  
**Deliverable:** Part 1 - System Planning (Section B)  
**File Location:** `/docs/API_Endpoint_Plan.md`  

---

## 1. API Architecture & Design Guidelines

The RaceDay RESTful API is structured around standard HTTP methods (`GET`, `POST`, `PUT`, `DELETE`), consistent URI naming conventions prefixed with `/api/`, JSON-formatted payloads, and standard HTTP response status codes.

### Role-Based Access Tiers:
- **`None (Public)`**: Accessible without authentication (e.g. registration, login, public event browsing).
- **`Any (Authenticated)`**: Accessible by any verified user possessing a valid session token (Organiser or Participant).
- **`Organiser`**: Restricted strictly to users authenticated with the `'Organiser'` role.
- **`Participant`**: Restricted strictly to users authenticated with the `'Participant'` role.

### Session Management:
Authentication state is managed via a secure session token passed via the `X-Session-Token` HTTP header or ASP.NET Core session cookie. Endpoints return `401 Unauthorized` for missing/expired credentials and `403 Forbidden` for role violations.

---

## 2. API Endpoint Specification Table

| HTTP Method | Route | Description | Role Required | Request Body (JSON) | Expected Response |
| :---: | :--- | :--- | :---: | :--- | :--- |
| **POST** | `/api/auth/register` | Registers a new user account as either an Organiser or Participant with hashed password storage. | **None (Public)** | `{ "fullName": "string", "email": "string", "password": "string", "role": "Organiser \| Participant", "phoneNumber": "string" }` | **201 Created** - User object & session token.<br>**400 Bad Request** - Validation failed.<br>**409 Conflict** - Email already in use. |
| **POST** | `/api/auth/login` | Authenticates existing user credentials and establishes a new stateful session. | **None (Public)** | `{ "email": "string", "password": "string" }` | **200 OK** - User profile, role & active session token.<br>**401 Unauthorized** - Invalid email or password. |
| **POST** | `/api/auth/logout` | Terminates the active session and invalidates the session token on the server. | **Any** | `None` | **200 OK** - Session successfully terminated.<br>**401 Unauthorized** - Invalid or missing session. |
| **GET** | `/api/auth/me` | Retrieves the profile and role details of the currently authenticated session caller. | **Any** | `None` | **200 OK** - Current user profile & role.<br>**401 Unauthorized** - Unauthenticated. |
| **GET** | `/api/profile` | Retrieves detailed profile information for the authenticated user. | **Any** | `None` | **200 OK** - Full user profile details.<br>**401 Unauthorized** - Unauthenticated. |
| **PUT** | `/api/profile` | Updates personal profile details (full name, phone number) for the logged-in user. | **Any** | `{ "fullName": "string", "phoneNumber": "string" }` | **200 OK** - Updated profile object.<br>**400 Bad Request** - Invalid fields.<br>**401 Unauthorized** - Unauthenticated. |
| **POST** | `/api/profile/picture` | Uploads a user profile picture to Azure Blob Storage and associates URL with user profile. | **Any** | `multipart/form-data` containing `file` (Image) | **200 OK** - `{ "profilePictureUrl": "string" }`<br>**400 Bad Request** - Empty or invalid image format.<br>**401 Unauthorized** - Unauthenticated. |
| **GET** | `/api/events` | Retrieves all scheduled sporting events, with optional query filters for type (Run, Walk, Cycle) and search terms. | **None (Public)** | `None` (Query parameters: `?type=Run&search=City`) | **200 OK** - Array of event summary objects with categories. |
| **GET** | `/api/events/{id}` | Retrieves full details of a specific event including categories and organiser details. | **None (Public)** | `None` | **200 OK** - Comprehensive event detail object.<br>**404 Not Found** - Event ID does not exist. |
| **POST** | `/api/events` | Creates a new race event with title, description, date, location, distance, and event type. | **Organiser** | `{ "title": "string", "description": "string", "eventDate": "ISO8601", "location": "string", "distanceKm": 10.0, "eventType": "Run \| Walk \| Cycle" }` | **201 Created** - Created event object with ID.<br>**400 Bad Request** - Validation failure.<br>**403 Forbidden** - Non-organiser role. |
| **PUT** | `/api/events/{id}` | Updates existing event metadata, date, venue, or status. | **Organiser** | `{ "title": "string", "description": "string", "eventDate": "ISO8601", "location": "string", "distanceKm": 10.0, "eventType": "string", "status": "string" }` | **200 OK** - Updated event object.<br>**403 Forbidden** - Caller is not the event owner.<br>**404 Not Found** - Event not found. |
| **DELETE** | `/api/events/{id}` | Deletes an event from the platform if no results have been published. | **Organiser** | `None` | **204 No Content** - Event deleted.<br>**403 Forbidden** - Unauthorized access.<br>**404 Not Found** - Event not found.<br>**409 Conflict** - Event has active registrations. |
| **POST** | `/api/events/{id}/banner` | Uploads an event banner image to Azure Blob Storage and links the cloud URI to the event. | **Organiser** | `multipart/form-data` containing `file` (Image) | **200 OK** - `{ "bannerImageUrl": "string" }`<br>**400 Bad Request** - Invalid file.<br>**403 Forbidden** - Non-organiser role.<br>**404 Not Found** - Event not found. |
| **GET** | `/api/categories/event/{eventId}` | Retrieves all competition categories (Under 20, Senior, 10km, etc.) configured for a specific event. | **None (Public)** | `None` | **200 OK** - Array of category objects.<br>**404 Not Found** - Event not found. |
| **POST** | `/api/categories` | Adds a new age or distance category with entry fee to an existing event. | **Organiser** | `{ "eventId": 1, "name": "Senior 10km", "description": "Ages 20-39", "minAge": 20, "maxAge": 39, "distanceKm": 10.0, "entryFee": 150.00 }` | **201 Created** - Created category record.<br>**400 Bad Request** - Invalid data.<br>**403 Forbidden** - Non-organiser. |
| **PUT** | `/api/categories/{id}` | Updates configuration attributes or entry fees of a category. | **Organiser** | `{ "name": "string", "description": "string", "minAge": 18, "maxAge": 35, "distanceKm": 10.0, "entryFee": 160.00 }` | **200 OK** - Updated category.<br>**403 Forbidden** - Forbidden.<br>**404 Not Found** - Category not found. |
| **DELETE** | `/api/categories/{id}` | Removes a category from an event if no participants have enrolled in it. | **Organiser** | `None` | **204 No Content** - Deleted.<br>**409 Conflict** - Category in use by enrolled athletes. |
| **POST** | `/api/enrolments` | Enrols the logged-in Participant in an event by selecting an eligible category; issues bib number. | **Participant** | `{ "eventId": 1, "categoryId": 2 }` | **201 Created** - Enrolment record with Bib Number and status.<br>**400 Bad Request** - Invalid category.<br>**409 Conflict** - Already enrolled in this event. |
| **GET** | `/api/enrolments/my` | Retrieves all event enrolments and status badges for the logged-in Participant. | **Participant** | `None` | **200 OK** - Array of participant's enrolments with event and category details. |
| **GET** | `/api/enrolments/event/{eventId}` | Retrieves all enrolled participants, categories, bib numbers, and statuses for an event. | **Organiser** | `None` | **200 OK** - List of athlete enrolments for the specified event.<br>**403 Forbidden** - Caller is not organiser. |
| **PUT** | `/api/enrolments/{id}/status` | Updates an athlete's enrolment status (e.g. from Pending to Confirmed or Cancelled). | **Organiser** | `{ "status": "Confirmed \| Pending \| Cancelled" }` | **200 OK** - Updated enrolment record.<br>**400 Bad Request** - Invalid status value.<br>**403 Forbidden** - Non-organiser. |
| **POST** | `/api/results/capture` | Captures and publishes finish times and positions for enrolled participants after an event. | **Organiser** | `{ "eventId": 1, "results": [ { "enrolmentId": 1, "finishTime": "00:45:12", "notes": "string" } ] }` | **201 Created** - Batch of published race results with calculated finishing positions.<br>**400 Bad Request** - Malformed times.<br>**403 Forbidden** - Non-organiser role. |
| **GET** | `/api/results/event/{eventId}` | Retrieves the complete leaderboard of published results for a specific completed event. | **None (Public)** | `None` | **200 OK** - Sorted leaderboard with positions, bib numbers, names, and finish times. |
| **GET** | `/api/results/my` | Retrieves the personal race history and results for the authenticated Participant across all events. | **Participant** | `None` | **200 OK** - List of personal race results with finish times and formatted positions (e.g. "47th out of 312 finishers"). |
| **GET** | `/api/organiser/dashboard` | Retrieves aggregate metrics: total events created, total enrolments, upcoming races, and revenue. | **Organiser** | `None` | **200 OK** - Summary statistics payload for dashboard rendering.<br>**403 Forbidden** - Non-organiser caller. |

---

## 3. Plan Compliance and Implementation Traceability

Every single endpoint documented above directly corresponds to:
1. **Part 2 RESTful Web API Controllers**: `AuthController`, `ProfileController`, `EventsController`, `CategoriesController`, `EnrolmentsController`, `ResultsController`, and `OrganiserController`.
2. **Part 2 Unit Test Suite**: `AuthTests`, `EventManagementTests`, `RoleEnforcementTests`, `EnrolmentTests`, `ResultCalculationTests`.
3. **Part 3 MVC Views & Services**: Consumed asynchronously by `RaceDayApiClient` within the ASP.NET Core MVC web application.

<!-- Verified Events and Categories endpoints against POE specifications -->

<!-- Verified Enrolment, Results capture, and Leaderboard endpoints -->

<!-- Finalized complete endpoint specification table matching Part 2 API design -->
