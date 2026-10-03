# Event Service Web API
A RESTful Web API built with ASP.NET Core for scheduling and managing events. The service provides endpoints to create, retrieve, update, and delete events with date validation.

## Endpoints

Base path: `/api/events`

* `GET /api/events` — Retrieve all events
* `GET /api/events/{id}` — Retrieve an event by ID
* `POST /api/events` — Create a new event
* `PUT /api/events/{id}` — Update an existing event by ID
* `DELETE /api/events/{id}` — Delete an event by ID

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
   dotnet run --project src/EventServiceWebApi
   ```
   
4. **Open [https://localhost:7174/swagger](https://localhost:7174/swagger) in your browser to explore the API** 
