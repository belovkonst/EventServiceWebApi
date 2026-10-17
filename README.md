# Event Service Web API
A RESTful Web API built with ASP.NET Core for scheduling and managing events. The service provides endpoints to create, retrieve, update, and delete events with date validation.

## Endpoints

Base path: `/events`

* `GET /events` — Retrieve events (supports optional filtering)
* `GET /events/{id}` — Retrieve an event by ID
* `POST /events` — Create a new event
* `PUT /events/{id}` — Update an existing event by ID
* `DELETE /events/{id}` — Delete an event by ID

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
   
3. **Run the application:**   
   ```bash
   dotnet test
   ```

4. **Run the application:**   
   ```bash
   dotnet run --project src/EventServiceWebApi
   ```
   
5. **Open [https://localhost:7174/swagger](https://localhost:7174/swagger) in your browser to explore the API** 
