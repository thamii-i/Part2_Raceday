# RaceDay - API Endpoint Plan

Part 1, Section B. Same content as `RaceDay_API_Endpoint_Plan.docx`.

**Role required:** None (public) | Any (logged in) | Organiser | Participant. "(event owner)" = the Organiser who created the event.

## 1. Authentication

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Creates a new Organiser or Participant account and stores only a hash of the password. | None (public) | `{ firstName, lastName, email, password, phone, role, organisationName (Organiser) | dateOfBirth, gender (Participant) }` | 201 Created - new profile returned (never the password)<br>400 Bad Request - validation failed, role is not Organiser/Participant, or role-specific fields missing<br>409 Conflict - email already registered |
| POST | `/api/auth/login` | Checks the credentials and returns a JWT carrying the user's id and role, which keeps the session authenticated on every later request. | None (public) | `{ email, password }` | 200 OK - { token, expiresAtUtc, userId, email, firstName, role }<br>400 Bad Request - invalid body<br>401 Unauthorized - wrong email or password |

## 2. User Profile

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| GET | `/api/users/me` | Returns the profile of the logged-in Organiser or Participant. | Any (logged in) | None | 200 OK - profile<br>401 Unauthorized - not logged in<br>404 Not Found - account no longer exists |
| PUT | `/api/users/me` | Updates the logged-in user's own details (role-specific fields apply). | Any (logged in) | `{ firstName, lastName, phone, organisationName (Organiser) | dateOfBirth, gender (Participant) }` | 200 OK - updated profile<br>400 Bad Request - validation failed<br>401 Unauthorized - not logged in<br>404 Not Found - account no longer exists |
| PUT | `/api/users/me/password` | Changes the logged-in user's password after confirming the current one. | Any (logged in) | `{ currentPassword, newPassword }` | 204 No Content - password changed<br>400 Bad Request - wrong current password or new password too short<br>401 Unauthorized - not logged in |

## 3. Events

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| GET | `/api/events` | Lists events (optionally filtered by ?type, ?upcomingOnly and ?search) so users can browse what is coming up. | Any (logged in) | None (optional query: type, upcomingOnly, search) | 200 OK - list of events ordered by date<br>401 Unauthorized - not logged in |
| GET | `/api/events/mine` | Lists only the events created by the logged-in Organiser. | Organiser | None | 200 OK - the organiser's events<br>401 Unauthorized - not logged in<br>403 Forbidden - caller is a Participant |
| GET | `/api/events/{id}` | Returns the details of a single event. | Any (logged in) | None | 200 OK - event<br>401 Unauthorized - not logged in<br>404 Not Found - event does not exist |
| POST | `/api/events` | Creates a new event (name, description, date, location, distance, type) owned by the logged-in Organiser. | Organiser | `{ eventName, description, eventDate, location, distanceKm, eventType }` | 201 Created - event returned<br>400 Bad Request - validation failed or date is in the past<br>401 Unauthorized - not logged in<br>403 Forbidden - caller is a Participant |
| PUT | `/api/events/{id}` | Updates an existing event; only the Organiser who created it may change it. | Organiser (event owner) | `{ eventName, description, eventDate, location, distanceKm, eventType }` | 200 OK - updated event<br>400 Bad Request - validation failed<br>401 Unauthorized - not logged in<br>403 Forbidden - Participant, or Organiser who does not own the event<br>404 Not Found - event does not exist |
| DELETE | `/api/events/{id}` | Deletes an event together with its category links, enrollments and results; only the owning Organiser may do this. | Organiser (event owner) | None | 204 No Content - event deleted<br>401 Unauthorized - not logged in<br>403 Forbidden - Participant, or Organiser who does not own the event<br>404 Not Found - event does not exist |

## 4. Categories

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| GET | `/api/categories` | Lists every category known to the system. | Any (logged in) | None | 200 OK - list of categories<br>401 Unauthorized - not logged in |
| GET | `/api/events/{eventId}/categories` | Lists the categories assigned to an event so participants can pick one. | Any (logged in) | None | 200 OK - list of categories<br>401 Unauthorized - not logged in<br>404 Not Found - event does not exist |
| POST | `/api/events/{eventId}/categories` | Defines a category for an event by reusing the category with that name or creating it, then assigning it to the event. | Organiser (event owner) | `{ categoryName, description }` | 201 Created - category returned<br>400 Bad Request - validation failed<br>401 Unauthorized - not logged in<br>403 Forbidden - Participant, or Organiser who does not own the event<br>404 Not Found - event does not exist<br>409 Conflict - category already assigned to this event |
| DELETE | `/api/events/{eventId}/categories/{categoryId}` | Removes a category from an event when nobody has enrolled in it. | Organiser (event owner) | None | 204 No Content - category removed from the event<br>401 Unauthorized - not logged in<br>403 Forbidden - Participant, or Organiser who does not own the event<br>404 Not Found - event does not exist or category not assigned to it<br>409 Conflict - participants are enrolled in this category |

## 5. Event Enrolments (Enrollments table)

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| POST | `/api/events/{eventId}/enrollments` | Enters the logged-in Participant into an event by selecting one of its categories, recording participant, event and category and issuing a bib number. | Participant | `{ categoryId }` | 201 Created - enrollment (participant, event, category, bibNumber, status)<br>400 Bad Request - category not assigned to this event, or event already took place<br>401 Unauthorized - not logged in<br>403 Forbidden - caller is an Organiser<br>404 Not Found - event does not exist<br>409 Conflict - already enrolled in this event |
| GET | `/api/events/{eventId}/enrollments` | Lists every enrollment for an event so the Organiser can see who has entered. | Organiser (event owner) | None | 200 OK - list of enrollments<br>401 Unauthorized - not logged in<br>403 Forbidden - Participant, or Organiser who does not own the event<br>404 Not Found - event does not exist |
| GET | `/api/enrollments/mine` | Lists the logged-in Participant's own enrollments. | Participant | None | 200 OK - the participant's enrollments<br>401 Unauthorized - not logged in<br>403 Forbidden - caller is an Organiser |
| DELETE | `/api/enrollments/{id}` | Withdraws the logged-in Participant from an event they previously entered. | Participant (enrollment owner) | None | 204 No Content - enrollment removed<br>401 Unauthorized - not logged in<br>403 Forbidden - Organiser, or enrollment belongs to someone else<br>404 Not Found - enrollment does not exist<br>409 Conflict - a result is already recorded |

## 6. Results

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| POST | `/api/enrollments/{enrollmentId}/results` | Captures the finish time and finishing position of a participant after an event. | Organiser (event owner) | `{ finishTime, finishPosition }` | 201 Created - result returned<br>400 Bad Request - finish time must be greater than zero / invalid body<br>401 Unauthorized - not logged in<br>403 Forbidden - Participant, or Organiser who does not own the event<br>404 Not Found - enrollment does not exist<br>409 Conflict - result already exists, or position already taken in this event |
| PUT | `/api/results/{id}` | Corrects a previously captured finish time or position. | Organiser (event owner) | `{ finishTime, finishPosition }` | 200 OK - updated result<br>400 Bad Request - validation failed<br>401 Unauthorized - not logged in<br>403 Forbidden - Participant, or Organiser who does not own the event<br>404 Not Found - result does not exist<br>409 Conflict - position already taken by another result |
| DELETE | `/api/results/{id}` | Removes a captured result entered in error. | Organiser (event owner) | None | 204 No Content - result deleted<br>401 Unauthorized - not logged in<br>403 Forbidden - Participant, or Organiser who does not own the event<br>404 Not Found - result does not exist |
| GET | `/api/events/{eventId}/results` | Lists all captured results for an event, ordered by finishing position. | Organiser (event owner) | None | 200 OK - results ordered by position<br>401 Unauthorized - not logged in<br>403 Forbidden - Participant, or Organiser who does not own the event<br>404 Not Found - event does not exist |
| GET | `/api/results/mine` | Lets a Participant view their own results, i.e. their personal performance history. | Participant | None | 200 OK - the participant's own results, newest event first<br>401 Unauthorized - not logged in<br>403 Forbidden - caller is an Organiser |

