# HabitTracker API — Technical Overview

A .NET Web API for tracking one-off tasks and recurring habits, built to demonstrate
Clean Architecture layering, JWT-based authentication with refresh token rotation,
and standard ASP.NET Core API design practices.

## Tech Stack

| Concern | Technology |
|---|---|
| Runtime | .NET 10 (LTS) |
| Language | C# 14 |
| Web framework | ASP.NET Core Web API (controller-based) |
| ORM | Entity Framework Core |
| Database | PostgreSQL (via Npgsql.EntityFrameworkCore.PostgreSQL) |
| Authentication | JWT Bearer (Microsoft.IdentityModel.JsonWebTokens) |
| Validation | FluentValidation |
| API documentation | Swashbuckle (Swagger / OpenAPI) |
| API versioning | Asp.Versioning.Mvc |
| Logging | Serilog (console + rolling file sinks) |
| Testing | xUnit, Moq, FluentAssertions, EF Core InMemory provider |
| Containerization | Docker (multi-stage Dockerfile for the API, Docker Compose orchestrating API + PostgreSQL) |

## Solution Structure (Clean Architecture)

```
HabitTracker.Domain          Entities, enums, BaseEntity — no external dependencies
HabitTracker.Application     DTOs, validators, feature-organized request/response models
HabitTracker.Infrastructure  EF Core DbContext, JWT/refresh token generation, seeding, migrations
HabitTracker.Api             Controllers, Program.cs, Swagger/versioning/auth wiring
HabitTracker.Tests           Unit tests (xUnit) against controllers and validators
Docker/                      Dockerfile and docker-compose.yml for containerized runs
```

Dependencies flow inward only: `Api` → `Infrastructure`/`Application` → `Domain`.
`Domain` has no dependency on any other project.

### Base entity pattern

All persisted entities inherit from `BaseEntity`, which supplies `Id` (client-generated
GUID, defaulted on construction), `CreatedAt`, and `UpdatedAt`. `AppDbContext` overrides
`SaveChangesAsync` to automatically stamp `CreatedAt`/`UpdatedAt` on insert/update, and
explicitly marks `CreatedAt` as unmodified on update so it can never be overwritten
after creation, regardless of how an entity was attached.

## Authentication & Authorization

- **JWT Bearer authentication**, issued and validated via `Microsoft.IdentityModel.JsonWebTokens`
  (`JsonWebTokenHandler`), not the older `System.IdentityModel.Tokens.Jwt`.
- **Access tokens** are short-lived (15 minutes) and self-contained — no database
  lookup is required to validate a request, only signature/expiry checks.
- **Refresh tokens** are opaque random strings, stored **hashed** (SHA-256) in a
  dedicated `RefreshTokens` table, never in plaintext. This is what makes logout/
  revocation possible, since a JWT itself cannot be invalidated before it expires.
- **Refresh token rotation**: each refresh call revokes the token used and issues a
  new access/refresh pair, so a stolen refresh token can only be used once before
  detection.
- **Password hashing** via ASP.NET Core Identity's `IPasswordHasher<User>`
  (`Microsoft.Extensions.Identity.Core` — the lightweight package, not the full
  `Microsoft.AspNetCore.Identity` stack, since this project doesn't use Identity's
  EF stores).
- **Every Tasks/Habits endpoint filters by the authenticated user's ID**, extracted
  from the JWT's `sub` claim (`options.MapInboundClaims = false` keeps claim names
  unmapped/predictable). Ownership checks happen in the same database query as the
  lookup (`WHERE Id = @id AND UserId = @userId`), so a resource belonging to another
  user returns `404 Not Found`, not `403 Forbidden` — this avoids confirming to a
  caller that a given ID exists at all under someone else's account.
- Shared logic for extracting the current user's ID lives in `ApiControllerBase`,
  which all resource controllers inherit from.

## API Versioning

- **URL path versioning** (`/api/v1/...`), the most common and cache-friendly
  approach for public REST APIs.
- Configured via `Asp.Versioning.Mvc` + `Asp.Versioning.Mvc.ApiExplorer`.
- `[ApiVersion("1.0")]` is declared explicitly on each concrete controller rather
  than on the shared `ApiControllerBase` — the versioning library has documented
  issues combining version attribution with controller inheritance, so version
  metadata is kept explicit per controller while only version-agnostic behavior
  (current-user extraction) lives in the base class.
- Swagger is configured to enumerate and display all registered API versions.

## Logging

- **Serilog**, replacing the default `ILogger` console-only provider, writing to:
  - Console (for local dev visibility)
  - Rolling daily files (`logs/log-YYYYMMDD.txt`), retained for 7 days
- **Structured logging** throughout — log calls use named placeholders
  (`"User {UserId} logged in"`) rather than string interpolation, so log fields
  remain queryable rather than flattened into plain text.
- Log levels used consistently:
  - `Information` — successful, noteworthy actions (login, resource created/updated)
  - `Warning` — expected-but-notable failures (bad credentials, not found, conflict)
  - `Error` — unexpected exceptions, logged with the exception object attached
- **Never logged**: passwords, raw JWTs, or raw refresh tokens — only identifiers
  (`UserId`, `Email`) are included in log context.
- A global exception-handling middleware catches and logs any unhandled exception
  with the request method/path, returning a generic `500` response rather than
  leaking exception details to the client.

## Validation

- **FluentValidation**, called manually per-action (not the deprecated
  `FluentValidation.AspNetCore` auto-validation package, which is no longer
  maintained). Validators are registered via `AddValidatorsFromAssemblyContaining<T>`
  (from `FluentValidation.DependencyInjectionExtensions`) and resolved either via
  constructor injection or `[FromServices]` per action.
- Validation failures return `400` via `ValidationProblem(...)`, using
  FluentValidation's `ValidationResult.ToDictionary()` helper to produce the
  standard `ValidationProblemDetails` shape.

## API Design Conventions

- `ActionResult<T>` is used for actions that return data (enables accurate
  OpenAPI/Swagger response typing); plain `IActionResult` is used for actions with
  no meaningful response body (`204 No Content` deletes, status toggles).
- `201 Created` (via `CreatedAtAction`) on resource creation, `204 No Content` on
  successful deletion, `409 Conflict` for valid-but-state-conflicting requests
  (duplicate email on register, duplicate habit log for the same day) — reserving
  `400` strictly for malformed/invalid input.
- `PUT` performs full-resource replacement (all fields required); `PATCH` performs
  partial updates via a nullable-fields DTO, where omitted fields are left
  untouched. A dedicated `PATCH .../complete` action exists for the single most
  common task operation, in addition to the general-purpose `PATCH`.
- Hard deletes are used throughout (no soft-delete/`IsDeleted` flag) — a deliberate
  choice given this project has no audit/compliance/recovery requirement.

## Testing Approach

- **Unit tests only** at this stage (no integration/end-to-end HTTP tests yet),
  targeting controllers directly with an EF Core InMemory-backed `AppDbContext`
  and a mocked `ILogger`.
- Each single-resource endpoint (`GetById`, `Update`, `Delete`, etc.) has a
  recurring four-case test pattern: happy path, validation failure, not found,
  and — most importantly — **belongs to another user**, which is the test that
  actually verifies the ownership/authorization boundary rather than just the
  happy path.
- Known limitation: the EF Core InMemory provider does not perfectly replicate
  real relational query translation (e.g. `Include` + `Select` projection
  interactions, correlated subqueries) — a few tests required restructuring
  queries to be provider-agnostic. A future improvement would be switching to
  SQLite's in-memory mode for closer parity with production PostgreSQL behavior.

## Data Model Summary

- `User` — account/auth
- `TaskItem` — one-off to-do items (title, due date, priority, completion state)
- `Habit` — recurring habit definitions (title, frequency)
- `HabitLog` — one row per day a habit was completed; a unique index on
  `(HabitId, CompletedDate)` prevents double-logging the same day. This event-log
  design (rather than a single mutable field on `Habit`) is what makes streak
  calculation, completion-rate stats, and calendar/history views possible.
- `RefreshToken` — hashed refresh tokens tied to a user, supporting revocation

## Containerization

- **Multi-stage `Dockerfile`** (`Docker/Dockerfile`): an SDK-based build stage
  compiles and publishes the app, then a much smaller ASP.NET runtime-only image
  copies just the published output — keeping the final image free of build
  tooling.
- **`docker-compose.yml`** (`Docker/docker-compose.yml`) orchestrates two services:
  - `postgres` — PostgreSQL 16, with a named volume for data persistence and a
    healthcheck (`pg_isready`) so the API doesn't start before the database is
    actually ready to accept connections.
  - `api` — built from the Dockerfile, depends on `postgres`'s healthcheck,
    exposed on port 8080.
- The Dockerfile lives in `Docker/` while the build **context** is the solution
  root (`context: ..` in the compose file) — this is necessary because the build
  needs access to every project (`Domain`, `Application`, `Infrastructure`, `Api`),
  not just the `Docker` folder itself.
- Containers communicate via Docker's internal DNS using service names
  (`Host=postgres` in the API's connection string), not `localhost` — `localhost`
  inside a container refers to the container itself, not the host machine or
  sibling containers.
- The JWT signing key is supplied via a `.env` file (`Docker/.env`, git-ignored)
  and substituted into `docker-compose.yml` as `${JWT_KEY}` — kept out of both the
  image and version control. This is a development-appropriate simplification;
  a production deployment would use a dedicated secrets manager instead.
- **Migrations are applied automatically at startup** via
  `app.Database.MigrateAsync()` in `Program.cs`, run before the demo-data seeder.
  This makes `docker compose up --build` fully self-contained — a fresh
  Postgres volume gets its schema created automatically, with no separate manual
  migration step required. This is a deliberate development/demo-mode choice;
  auto-migrating on every startup is generally avoided in real production
  systems in favor of a controlled, separate deployment step.
