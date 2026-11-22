# Event Service Web API
A RESTful Web API built with ASP.NET Core for scheduling and managing events. The service provides endpoints to create, retrieve, update, and delete events with date validation.

## Endpoints

Base path: `/events`

* `GET /events` — Retrieve events (supports optional filtering)
* `GET /events/{id}` — Retrieve an event by ID
* `POST /events` — Create a new event (`totalSeats` is required and must be greater than `0`)
* `PUT /events/{id}` — Update an existing event by ID (preserves current `availableSeats`)
* `DELETE /events/{id}` — Delete an event by ID
* `POST /events/{id}/book` — Create a booking for an event (returns `202 Accepted` with `Location` header, `404 Not Found` if the event does not exist, or `409 Conflict` if no seats are available)

Base path: `/bookings`

* `GET /bookings/{id}` — Retrieve current booking status and details by ID

### `Event` Model

| Property | Type | Required | Description |
| :--- | :--- | :--- | :--- |
| `Id` | `Guid` | Yes | Unique identifier of the event. |
| `Title` | `string` | Yes | Title of the event (1–200 characters). |
| `Description` | `string?` | No | Optional description (up to 2000 characters). |
| `StartAt` | `DateTime` | Yes | Event start date and time (UTC). |
| `EndAt` | `DateTime` | Yes | Event end date and time (UTC). Must be greater than `StartAt`. |
| `TotalSeats` | `int` | Yes | Total seat capacity of the event. Required on creation and must be greater than `0`. |
| `AvailableSeats` | `int` | Yes | Remaining available seats. Equal to `TotalSeats` upon creation; decremented on booking and restored on seat release. |

### Event Query Parameters (`GET /events`)

The `GET /events` endpoint supports optional query parameters for filtering and paginating results. When multiple parameters are provided, they are combined using a logical **AND**.

| Parameter | Type | Required | Description | Example |
| :--- | :--- | :--- | :--- | :--- |
| `title` | `string` | No | Case-insensitive substring search in event title. | `?title=conference` |
| `from` | `DateTime` (ISO 8601) | No | Filters events starting on or after the specified date/time (UTC). | `?from=2026-10-01T00:00:00Z` |
| `to` | `DateTime` (ISO 8601) | No | Filters events ending on or before the specified date/time (UTC). | `?to=2026-10-31T23:59:59Z` |
| `page` | `int` | `1` | Page number (must be greater than 0). | `?page=2` |
| `pageSize` | `int` | `10` | Number of items per page (between 1 and 100). | `?pageSize=20` |

#### Validation Rules:
* The `title` filter must not exceed 200 characters.
* If both `from` and `to` are provided, `from` must be earlier than or equal to `to`. Otherwise, the API returns `400 Bad Request`.
* `page` must be >= 1.
* `pageSize` must be between 1 and 100.

#### Example Requests:
* Search by title:  
  `GET /events?title=meetup`
* Events within a specific date range:  
  `GET /events?from=2026-10-01T00:00:00Z&to=2026-10-15T23:59:59Z`
* Combined filtering:  
  `GET /events?title=dev&from=2026-10-10T00:00:00Z&to=2026-10-25T23:59:59Z`

> Detailed specifications of endpoints, request/response models, and status codes are available via the interactive **Swagger UI** (`/swagger`).

## Booking & Background Processing
### `Booking` Model
| Property | Type | Required | Description |
| :--- | :--- | :--- | :--- |
| `Id` | `Guid` | Yes | Unique identifier of the booking. |
| `EventId` | `Guid` | Yes | Identifier of the booked event. |
| `Status` | `BookingStatus` | Yes | Current status of the booking (`Pending`, `Confirmed`, `Rejected`). |
| `CreatedAt` | `DateTime` | Yes | Date and time when the booking was created (UTC). |
| `ProcessedAt` | `DateTime?` | No | Date and time when the booking was processed (UTC). |
### Booking Statuses (`BookingStatus`)
* `Pending` — Booking has been created and is awaiting processing.
* `Confirmed` — Booking has been confirmed.
* `Rejected` — Booking has been rejected.
### Synchronization Primitives & Thread Safety
To prevent overbooking and race conditions under concurrent requests, the application uses the following synchronization primitives:

* **`Lock` in `BookingService.CreateBookingAsync`** — Protects the critical section containing the atomic sequence: fetch event → check & reserve seat (`TryReserveSeats()`) → update event repository → create & store booking. This ensures that only one thread can reserve a seat and create a booking at a time, preventing overbooking. Read operations (`GetBookingByIdAsync`) remain lock-free.
* **`SemaphoreSlim(1, 1)` + `Task.WhenAll` in `BookingProcessingService`** — Pending bookings are processed in parallel via `Task.WhenAll` so external processing delays (`ProcessingDelay`) run concurrently. `SemaphoreSlim.WaitAsync(stoppingToken)` is used instead of `lock` (since `lock` cannot span `await`) to serialize repository reads/writes during confirmation, rejection, and seat release, and is always released in a `finally` block.
* **`Interlocked.CompareExchange`, `Volatile.Read`, and `Lock` in `Event`** — `TryReserveSeats()` and `ReleaseSeats()` update `_availableSeats` atomically with a lock-free CAS loop, while `Lock` ensures consistent updates (`Update()`) and torn-free snapshots (`GetSnapshot()`) of event details.
* **`ConcurrentDictionary` in `EventRepository` and `BookingRepository`** — Provides thread-safe in-memory storage across services and background workers.

### Background Processing
Bookings are processed asynchronously and in parallel by `BookingProcessingService` (`BackgroundService`):
1. Every `PollingInterval` (5 seconds), the service fetches all `Pending` bookings from `IBookingRepository`.
2. All pending bookings are launched in parallel using `Task.WhenAll`.
3. For each booking, `ProcessBookingAsync` logs the start of processing and simulates an external call (`ProcessingDelay`, 2 seconds) **before** acquiring the semaphore.
4. Inside the `SemaphoreSlim` critical section:
   * If the event still exists in `IEventRepository`, the booking transitions to `Confirmed`, `ProcessedAt` is set, and the booking is saved.
   * If the event was deleted before processing, the booking transitions to `Rejected` with a warning log.
   * If an unexpected exception occurs, the booking is marked `Rejected`, the reserved seat is returned via `Event.ReleaseSeats()`, and both repositories are updated.

### Overbooking Scenario Example
1. **Create an event with 3 seats:**
   `POST /events`
   ```json
   {
     "title": "Backend Meetup",
     "description": "Concurrency & thread safety in ASP.NET Core",
     "startAt": "2026-12-01T10:00:00Z",
     "endAt": "2026-12-01T12:00:00Z",
     "totalSeats": 3
   }
   ```
   Returns `201 Created` with `"totalSeats": 3, "availableSeats": 3`.

2. **Create 3 bookings for the event:**
   Send `POST /events/{eventId}/book` three times. Each request returns `202 Accepted` with header `Location: /bookings/{bookingId}` and payload:
   ```json
   {
     "id": "232af52e-b74a-4500-8881-112d6d5ff1d0",
     "eventId": "daac104b-5573-4125-ad1b-7532ed35b452",
     "status": "Pending",
     "createdAt": "2026-11-22T14:13:55.5768138Z",
     "processedAt": null
   }
   ```
   After the 3rd booking, `GET /events/{eventId}` shows `"availableSeats": 0`.

3. **Attempt a 4th booking (overbooking protection):**
   `POST /events/{eventId}/book` returns `409 Conflict` (`application/problem+json`):
   ```json
   {
     "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
     "title": "Conflict",
     "status": 409,
     "detail": "No available seats for this event",
     "instance": "/events/daac104b-5573-4125-ad1b-7532ed35b452/book"
   }
   ```

4. **Check booking status after a few seconds:**
   `GET /bookings/232af52e-b74a-4500-8881-112d6d5ff1d0` returns `200 OK`:
   ```json
   {
     "id": "232af52e-b74a-4500-8881-112d6d5ff1d0",
     "eventId": "daac104b-5573-4125-ad1b-7532ed35b452",
     "status": "Confirmed",
     "createdAt": "2026-11-22T14:13:55.5768138Z",
     "processedAt": "2026-11-22T14:14:00.0482841Z"
   }
   ```

## Error Handling
All unhandled exceptions and validation errors return a standardized JSON response in the **Problem Details (RFC 7807)** format containing the HTTP status code and error message:
* `400 Bad Request` — Validation or argument errors
* `404 Not Found` — Event with the specified ID was not found
* `409 Conflict` — No available seats for the event (`NoAvailableSeatsException`), concurrency conflict or duplicate resource
* `500 Internal Server Error` — Unexpected server error

#### Example Error Response (`404 Not Found`):
```json
{
  "status": 404,
  "detail": "Event with id 3fa85f64-5717-4562-b3fc-2c963f66afa6 was not found."
}
```

## Getting Started

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd EventServiceWebApi
   ```
   
2. **Build the solution:**   
   ```bash
   dotnet build
   ```
   
3. **Run the tests:**   
   ```bash
   dotnet test
   ```

4. **Run the application:**   
   ```bash
   dotnet run --project src/EventServiceWebApi
   ```
   
5. **Open [https://localhost:7174/swagger](https://localhost:7174/swagger) in your browser to explore the API** 
