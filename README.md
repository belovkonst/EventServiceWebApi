# Event Service Web API
A RESTful Web API built with ASP.NET Core for scheduling and managing events. The service provides endpoints to create, retrieve, update, and delete events with date validation.

## Endpoints

Base path: `/events`

* `GET /events` — Retrieve events (supports optional filtering)
* `GET /events/{id}` — Retrieve an event by ID
* `POST /events` — Create a new event
* `PUT /events/{id}` — Update an existing event by ID
* `DELETE /events/{id}` — Delete an event by ID
* `POST /events/{id}/book` — Create a booking for an event (returns `202 Accepted` with `Location` header)

Base path: `/bookings`

* `GET /bookings/{id}` — Retrieve current booking status and details by ID

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
### Background Processing
Bookings are processed asynchronously in the background by `BookingProcessingService` (`BackgroundService`):
1. The service periodically polls the in-memory repository for bookings in the `Pending` status.
2. For each pending booking, it simulates an external system request with a 2-second delay (`Task.Delay`).
3. After the delay, the booking status changes to `Confirmed`, `ProcessedAt` is populated with the current UTC timestamp, and the updated booking is saved in the repository.
### Booking Usage Example
1. **Create an event:**
   `POST /events`
2. **Create a booking for the event:**
   `POST /events/{eventId}/book`  
   Returns `202 Accepted` with header `Location: /bookings/{bookingId}` and payload:
   ```json
   {
     "id": "232af52e-b74a-4500-8881-112d6d5ff1d0",
     "eventId": "daac104b-5573-4125-ad1b-7532ed35b452",
     "status": "Pending",
     "createdAt": "2026-11-05T17:13:55.5768138Z",
     "processedAt": null
   }
   ```
3. **Check status immediately:**
   `GET /bookings/232af52e-b74a-4500-8881-112d6d5ff1d0` — returns `"status": "Pending"`, `"processedAt": null`.
4. **Check status after a few seconds:**
   `GET /bookings/232af52e-b74a-4500-8881-112d6d5ff1d0` — returns `200 OK`:
   ```json
   {
     "id": "232af52e-b74a-4500-8881-112d6d5ff1d0",
     "eventId": "daac104b-5573-4125-ad1b-7532ed35b452",
     "status": "Confirmed",
     "createdAt": "2026-11-05T17:13:55.5768138Z",
     "processedAt": "2026-11-05T17:14:00.0482841Z"
   }
   ```

## Error Handling
All unhandled exceptions and validation errors return a standardized JSON response in the **Problem Details (RFC 7807)** format containing the HTTP status code and error message:
* `400 Bad Request` — Validation or argument errors
* `404 Not Found` — Event with the specified ID was not found
* `409 Conflict` — Concurrency conflict or duplicate resource
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
