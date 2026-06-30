# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Language

All user-facing messages, error messages, validation messages, and code comments must be written in **Spanish**.

## Commands

### Run with Docker (recommended)
```bash
cp .env.example .env   # fill in secrets
docker compose up --build
```
- API: http://localhost:8081/swagger
- MinIO console: http://localhost:9001
- Frontend: http://localhost:3000
- PostgreSQL is exposed on host port **5433** (not 5432) to avoid conflicts with a local Postgres instance.

### Run locally (without Docker)
Requires PostgreSQL running locally. Set `CONNECTION_STRING` in `.env` pointing to `localhost`.
```bash
cd src/Booking.WebAPI
dotnet run
```

### Build
```bash
dotnet build Booking.sln
```
All four projects target **.NET 10**.

### EF Core migrations
```bash
# Add a new migration (run from repo root)
dotnet ef migrations add <NombreMigracion> \
  --project src/Booking.Infrastructure \
  --startup-project src/Booking.WebAPI

# Apply migrations manually
dotnet ef database update \
  --project src/Booking.Infrastructure \
  --startup-project src/Booking.WebAPI
```
Migrations run automatically on startup via `db.Database.Migrate()` in `Program.cs`.

## Architecture

Clean Architecture with four projects:

| Project | Role |
|---|---|
| `Booking.Domain` | Entities, value objects, enums, domain exceptions — zero dependencies |
| `Booking.Application` | CQRS handlers (MediatR), FluentValidation validators, interface contracts |
| `Booking.Infrastructure` | EF Core (PostgreSQL), JWT, MinIO storage, Gemini AI KYC, MailKit email |
| `Booking.WebAPI` | ASP.NET Core controllers, global exception middleware, Swagger |

There are **no test projects** in this solution.

The `frontend/` directory is vanilla HTML/JS served by Nginx — no framework, no build step. Pages: `index.html` (search/listings), `auth.html` (login/register), `dashboard.html` (owner metrics/reports), `profile.html` (user profile, KYC upload, wishlist), `property.html` (property detail/booking). All API calls go through `frontend/js/api.js`.

## Key architectural decisions

**No repository pattern.** Application handlers inject `IApplicationDbContext` (defined in `Booking.Application/Common/Interfaces/IApplicationDbContext.cs`) and query EF Core DbSets directly. Do not introduce a repository layer.

**CQRS via MediatR.** Each feature lives in its own folder under `Booking.Application/<Feature>/Commands/<Name>/` or `.../Queries/<Name>/`. Each file contains the `record` command/query, its `AbstractValidator`, and its `IRequestHandler` together in one file. Request DTOs used by controllers are defined at the bottom of the same controller file (not in separate files).

**Domain enforces business rules.** The `Booking.Domain.Entities.Booking` factory method `Booking.Create(...)` enforces the double-booking prevention and the check-in/check-out time invariant (14:00/12:00 UTC) via `BookingDateRange`. The Application layer must pass all confirmed bookings for the property so the domain can validate.

**Exception-to-HTTP mapping.** `ExceptionHandlingMiddleware` maps `DomainException` → 400, `NotFoundException` → 404, `BookingConflictException` → 409, `ValidationException` → 400, `UnauthorizedAccessException` → 401.

**FluentValidation runs automatically.** `ValidationBehavior<TRequest, TResponse>` is registered as a MediatR pipeline behavior and runs all validators before any handler executes. Throw `DomainException` from domain/handlers for business-rule violations; use validators for input validation.

**KYC with Gemini Vision AI.** `KycService` calls the Gemini Vision API with identity document images fetched from MinIO. When `GEMINI_API_KEY` is the placeholder `YOUR_…`, it returns deterministic mock data (seeded by object key hash) so the flow works in development without a real key.

## User roles and authorization

Two roles exist in `UserRole` enum: `Guest` (huésped) and `Owner` (propietario). Controllers use `[Authorize]` without role restrictions; handlers enforce role-level rules (e.g., only the owner of a property can update it). The authenticated user's ID is extracted via `User.FindFirstValue(ClaimTypes.NameIdentifier)`.

## DbSet naming convention
`IApplicationDbContext` uses Spanish names for DbSets: `Usuarios`, `Propiedades`, `Reservas`, `DocumentosIdentidad`, `ListaDeseos`, `RefreshTokens`, `Notificaciones`.

## Environment variables
The app loads `.env` from the repo root at startup (via `DotNetEnv`). In Docker Compose, variables are injected directly. See `.env.example` for all required variables: `CONNECTION_STRING`, `JWT_SECRET`, `MINIO_*`, `GEMINI_API_KEY`, `SMTP_*`.

Enums are serialized as strings in JSON responses (configured via `JsonStringEnumConverter` in `Program.cs`).

## Infrastructure services
- **Storage:** MinIO (S3-compatible) via `IStorageService` / `StorageService`. Two buckets: `kyc-documents` and `property-photos`. Max 10 photos per property; public URLs are served via `MINIO_PUBLIC_ENDPOINT`.
- **Auth:** JWT access token (60 min) + refresh token (7 days) stored as SHA-256 hash in DB. BCrypt password hashing.
- **Email:** MailKit / SMTP via `IEmailService`. Fire-and-forget pattern (non-critical path).
- **Reports:** ClosedXML for Excel export (`GET /api/owner/report/export`).
