# HotelOS — Hotel Management System

A modular-monolith Hotel Management System built with **ASP.NET Core (.NET 10)** following
**Clean Architecture**, with **Entity Framework Core + PostgreSQL**, **JWT auth + refresh
tokens + RBAC**, **CQRS (MediatR)**, **FluentValidation**, **SignalR** realtime, and **Serilog**.

> The React + TypeScript + Tailwind frontend is added on top of this API (next phase).

## Architecture (Clean Architecture)

```
src/
  HotelOS.Domain          # entities, enums, domain services (no dependencies)
  HotelOS.Application     # CQRS (MediatR), DTOs, validators, abstractions (ports)
  HotelOS.Infrastructure  # EF Core DbContext, configs, repositories, UoW, JWT, seeding
  HotelOS.Api             # controllers, SignalR hub, middleware, DI, Program.cs
```

Dependencies point inward: `Api → Infrastructure → Application → Domain`.
Patterns: **Repository + Unit of Work**, **CQRS**, **DTOs**, **Dependency Injection**,
pipeline **validation behavior**, global **exception handling**.

## Prerequisites

- .NET SDK 10
- PostgreSQL 16 running locally (no Docker). Default connection:
  `Host=localhost;Port=5432;Database=hotelos;Username=khusniddindev` (edit in
  `src/HotelOS.Api/appsettings.json`).

```bash
# macOS (Homebrew)
brew install postgresql@16 && brew services start postgresql@16
createdb hotelos
```

## Run

```bash
dotnet restore
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/HotelOS.Api --urls http://localhost:5080
```

On startup the API **applies migrations** and **seeds** roles, room types, demo users and 10 rooms.

- Swagger UI: <http://localhost:5080/swagger>
- SignalR hub: `ws://localhost:5080/hubs/dashboard?access_token=<JWT>`

## Seeded accounts

| Username | Password | Role |
|---|---|---|
| admin | Admin@123 | Administrator |
| manager | Password@123 | HotelManager |
| reception | Password@123 | Receptionist |
| housekeeping | Password@123 | Housekeeping |
| kitchen | Password@123 | KitchenStaff |
| roomservice | Password@123 | RoomServiceStaff |
| maintenance | Password@123 | MaintenanceStaff |

## API (current foundation)

| Method | Route | Auth |
|---|---|---|
| POST | `/api/auth/login` | anonymous |
| POST | `/api/auth/refresh` | anonymous |
| GET | `/api/auth/me` | any authenticated |
| GET | `/api/rooms` (page, pageSize, search, sortBy, sortDir, status, roomTypeId, floor) | authenticated |
| GET | `/api/rooms/{id}` | authenticated |
| POST | `/api/rooms` | Administrator, HotelManager |
| PUT | `/api/rooms/{id}/status` | Administrator, HotelManager |
| GET | `/api/dashboard` | authenticated |
| GET | `/api/users` | Administrator |
| POST | `/api/users` | Administrator |

### Quick test

```bash
TOKEN=$(curl -s -X POST http://localhost:5080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin","password":"Admin@123"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['accessToken'])")

curl -s http://localhost:5080/api/rooms -H "Authorization: Bearer $TOKEN"
curl -s http://localhost:5080/api/dashboard -H "Authorization: Bearer $TOKEN"
```

## EF Core migrations

```bash
dotnet ef migrations add <Name> --project src/HotelOS.Infrastructure --startup-project src/HotelOS.Api
dotnet ef database update            --project src/HotelOS.Infrastructure --startup-project src/HotelOS.Api
```

## Roadmap (next modules)

Reception (guests, reservations, check-in/out, assignment) · Housekeeping queue ·
Kitchen · Room Service + billing charges · Maintenance priority queue · Billing &
invoices · Payments · Notifications · live Dashboard broadcast · React frontend.

> The previous event-driven microservices build is preserved in git under the tag
> `microservices-build`.
