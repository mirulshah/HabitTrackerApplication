# HabitTracker API — Setup Guide (New Machine)

Steps to get the project running after cloning it onto a different computer.
Two paths are covered: **Docker Compose** (recommended — one command, no local
Postgres/SDK setup needed beyond Docker itself) and **native** (running directly
via Visual Studio/`dotnet run` against a standalone Postgres container).

## 1. Prerequisites — install these first

- **Docker Desktop** — https://www.docker.com/products/docker-desktop
- **Git**
- **.NET 10 SDK** — https://dotnet.microsoft.com/download
  (only required for the native path, or for running `dotnet ef`/tests locally)
- **Visual Studio 2022 (v17.14+) or Visual Studio 2026**, with the ASP.NET and web
  development workload — only required for the native path

Verify installs:
```bash
docker --version
git --version
dotnet --version    # should show a 10.x.x version, if installed
```

## 2. Clone the repository

```bash
git clone https://github.com/<your-username>/HabitTrackerApplication.git
cd HabitTrackerApplication/HabitTracker.Api
```

---

## Path A — Docker Compose (recommended)

## A1. Set the JWT secret

The API needs a signing key for JWTs, supplied via a `.env` file that Docker
Compose reads automatically. This file is git-ignored — create it yourself:

```bash
cd Docker
```
Create a file named `.env` in this folder with:
```
JWT_KEY=REPLACE-WITH-A-LONG-RANDOM-STRING-AT-LEAST-32-CHARS
```
Any long, random 32+ character string works for local development.

## A2. Build and run everything

From the `Docker` folder:
```bash
docker compose up --build
```
This builds the API image, starts PostgreSQL, waits for it to become healthy, then
starts the API. Database migrations are applied automatically on startup, and demo
data is seeded automatically the first time the app runs — no separate manual
migration step is needed.

## A3. Verify

```
http://localhost:8080/swagger
```

## A4. Stopping

```bash
docker compose down
```
Add `-v` (`docker compose down -v`) to also wipe the Postgres data volume and
start completely fresh next time.

## A5. Running from the solution root instead of `Docker/`

If you'd rather not `cd` into `Docker/` every time:
```bash
docker compose -f Docker/docker-compose.yml up --build
```

---

## Path B — Native (Visual Studio / `dotnet run`)

Use this if you want to debug directly in Visual Studio rather than running
inside a container.

## B1. Start PostgreSQL in Docker (database only — the API still runs natively)

```bash
docker run --name habittracker-pg -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=HabitTrackerDb -p 5432:5432 -d postgres:16
```
Confirm it's running: `docker ps` should list `habittracker-pg` with port `5432`
mapped.

> If something else is already using port 5432, stop it or map this container to
> a different host port (e.g. `-p 5433:5432`) and adjust the connection string in
> step B3 accordingly.

## B2. Open the solution

Open the solution in Visual Studio. Confirm `HabitTracker.Api` is set as the
**startup project** (bold in Solution Explorer — right-click it and choose
**Set as Startup Project** if not).

## B3. Configure the connection string

Confirm `HabitTracker.Api/appsettings.json` has:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=HabitTrackerDb;Username=postgres;Password=postgres"
}
```

## B4. Set the JWT secret (User Secrets — not committed to the repo)

```bash
cd HabitTracker.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "REPLACE-WITH-A-LONG-RANDOM-STRING-AT-LEAST-32-CHARS"
```
(Or in Visual Studio: right-click `HabitTracker.Api` → **Manage User Secrets**.)

## B5. Apply migrations

Migrations also run automatically at startup (`app.Database.MigrateAsync()` in
`Program.cs`), so this step is optional — but you can apply them manually first
if you want the schema in place before the first run:

Package Manager Console (Default project: `HabitTracker.Infrastructure`, Startup
project: `HabitTracker.Api`):
```powershell
Update-Database
```
CLI equivalent (requires the `dotnet-ef` global tool — install with
`dotnet tool install --global dotnet-ef` if you get a "command not found" error):
```bash
dotnet ef database update --project HabitTracker.Infrastructure --startup-project HabitTracker.Api
```
Run this from the solution root, not from inside `Docker/`.

## B6. Run the app

Press **F5** in Visual Studio, or:
```bash
dotnet run --project HabitTracker.Api
```
Navigate to `https://localhost:PORT/swagger` (check the console output for the
actual port on first run).

---

## Log in with the seeded demo account (either path)

```
Email: demo@habittracker.com
Password: Password123!
```
Use `POST /api/v1/auth/login` in Swagger, copy the returned `accessToken`, click
the **Authorize** button (top right of the Swagger page), and enter:
```
Bearer <paste your access token here>
```

## Run the test suite (either path)

No database or Docker container is required for tests — they run against EF
Core's InMemory provider.

In Visual Studio: **Test → Run All Tests** (or open Test Explorer).
From the CLI: `dotnet test`

## Troubleshooting quick reference

| Symptom | Likely cause |
|---|---|
| `relation "Users" does not exist` (containerized run) | A fresh Postgres volume with no migrations applied — should self-resolve automatically via `MigrateAsync()` on startup; if seen on an older image, rebuild (`docker compose up --build`) |
| `password authentication failed for user "postgres"` | Container was created previously with a different password baked into its volume — remove and recreate it (`docker rm -f habittracker-pg` for the native path, or `docker compose down -v` for Compose) |
| `failed to solve: failed to read dockerfile: open Dockerfile: no such file or directory` | Filename casing mismatch — Linux-based Docker builds are case-sensitive; confirm the file is exactly `Dockerfile`, not `DockerFile` |
| `dotnet ef` — "could not execute because the specified command or file was not found" | The `dotnet-ef` global tool isn't installed — `dotnet tool install --global dotnet-ef` |
| `Add-Migration`/`Update-Database` not recognized in PMC | `Microsoft.EntityFrameworkCore.Tools` not installed on `HabitTracker.Infrastructure`, or PMC needs restarting after installing it |
| 404 on `/swagger` | Missing/incorrect `launchUrl` in `launchSettings.json`, or Swagger services not registered in `Program.cs` |
| 401 on every authenticated endpoint even with a token | Token expired (15 min lifetime) — log in again for a fresh one; also confirm you prefixed the token with the literal word `Bearer ` in Swagger's Authorize dialog |
| Config changes in `Program.cs` seem to not take effect | Hot Reload doesn't apply `Program.cs`/DI changes — fully stop debugging and press F5 again (native path only) |

## Notes

- The `logs/` folder (Serilog output), `bin/`/`obj/`/`.vs/`, and `Docker/.env` are
  git-ignored and either regenerate automatically or must be created locally per
  the steps above.
- Demo/seed data is only inserted in the `Development` environment and only if the
  `Users` table is empty — safe to restart the app repeatedly without duplicating
  data.
- Migrations auto-apply on every startup in both paths (`MigrateAsync()` in
  `Program.cs`) — a deliberate development-mode convenience, not a pattern used
  as-is in production deployments.
