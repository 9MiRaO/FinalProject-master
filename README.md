# User Management API

ASP.NET Core Minimal API with CRUD endpoints for user records. Open Swagger UI after starting the app.

## Run

```bash
dotnet restore
dotnet run
```

Then open `/swagger` in the browser (for example `http://localhost:5164/swagger`). The root URL redirects to Swagger.

## Endpoints

- `GET /users` — list users
- `GET /users/{id}` — get a user by ID
- `POST /users` — create a user
- `PUT /users/{id}` — update a user
- `DELETE /users/{id}` — delete a user

User fields: `firstName`, `lastName`, `email`, `role` (`Admin` or `User`).
