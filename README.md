# Identity API

A user/authentication API built with **Hexagonal Architecture (Ports & Adapters) + DDD +
CQRS (MediatR)** on **.NET 8**. The long-term goal is a centralized **Identity Provider (IdP)**
serving multiple applications, each one a configurable audience.

## Stack

- **.NET 8** (`net8.0` everywhere), C# with `ImplicitUsings` + `Nullable` enabled
- **PostgreSQL** via `Npgsql.EntityFrameworkCore.PostgreSQL` — **EF Core 8.0.10**
- **MediatR** (CQRS dispatch), **AutoMapper**, **FluentValidation**
- **Serilog** (logging), **BCrypt.Net-Next** (password hashing), **Swashbuckle** (Swagger)
- **Testing:** xUnit, FluentAssertions, NSubstitute, Bogus, Testcontainers.PostgreSql

## Solution layout

```
src/
├── Identity.Domain        Core: entities, enums, validators, repository ports (references Common only)
├── Identity.Application   Core: use cases / CQRS handlers (references Domain)
├── Identity.Common        Crosscutting: JWT, password hashing, logging, health checks, validation pipeline
├── Identity.IoC           Crosscutting: dependency registration (module initializers)
├── Identity.ORM           Driven adapter: EF Core DbContext, mappings, repositories, migrations
└── Identity.WebApi        Driver adapter: controllers, Program.cs, middleware (references IoC only)
tests/
├── Identity.Unit          Unit tests (Domain / Application)
├── Identity.Integration   Integration tests (Testcontainers PostgreSQL)
└── Identity.Functional    Functional / end-to-end tests
```

**Dependency rule:** everything points inward. `Domain` → `Common`; `Application` → `Domain`;
`ORM` → `Domain`; `IoC` → all; `WebApi` → `IoC` only. `Domain` and `Application` must never
learn about the ORM or WebApi.

## Prerequisites

- [.NET SDK 8](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for the local PostgreSQL)
- EF Core CLI tools: `dotnet tool install --global dotnet-ef`

## Local database

A PostgreSQL container is defined in [`docker-compose.yml`](./docker-compose.yml):

```bash
docker compose up -d identity.database
```

| Setting  | Value                              |
|----------|------------------------------------|
| Image    | `postgres:13`                      |
| Database | `identity_db`                      |
| Host port| **5433** (→ 5432 in the container) |

Port **5433** is deliberate — it avoids clashing with the sales API's PostgreSQL instance. The
connection string in `src/Identity.WebApi/appsettings.json` already points at it.

## Build & test

Run from the repository root:

```bash
dotnet build Identity.sln     # expect 0 errors (2 NU1903/AutoMapper warnings are known & intentional)
dotnet test  Identity.sln     # runs Unit + Integration + Functional
```

## Run the API

```bash
dotnet run --project src/Identity.WebApi
```

Swagger UI is available in the `Development` environment at `/swagger`.

## Migrations (EF Core)

The DbContext is `DefaultContext` in **`Identity.ORM`**. Migrations are generated into the
**`src/Identity.ORM/Migrations/`** folder — this is fixed by `MigrationsAssembly("Identity.ORM")`,
which is configured both in `src/Identity.WebApi/Program.cs` and in the design-time
`DefaultContextFactory`.

Because `Identity.ORM` has no `appsettings.json` of its own, the connection string is resolved
from the **WebApi** project, so EF commands must pass `Identity.WebApi` as the startup project
(`-s`). Run these **from the repository root**:

```bash
# Create a migration (files land in src/Identity.ORM/Migrations/)
dotnet ef migrations add <MigrationName> -p src/Identity.ORM -s src/Identity.WebApi

# List existing migrations
dotnet ef migrations list -p src/Identity.ORM -s src/Identity.WebApi

# Apply migrations to the database (requires the Postgres container to be running)
dotnet ef database update -p src/Identity.ORM -s src/Identity.WebApi

# Remove the last (not-yet-applied) migration
dotnet ef migrations remove -p src/Identity.ORM -s src/Identity.WebApi
```

- `-p` (**project**) `src/Identity.ORM` — where the migration files are written.
- `-s` (**startup**) `src/Identity.WebApi` — supplies configuration and the connection string.

**Tooling note:** a globally installed `dotnet-ef` newer than the projects' EF Core 8.0.10 works
for generating 8.x migrations. If your global tool ever errors against the 8.x runtime, pin a
local one and prefix commands with `dotnet`:

```bash
dotnet new tool-manifest                       # once, if no manifest exists
dotnet tool install dotnet-ef --version 8.0.10
dotnet dotnet-ef migrations add <MigrationName> -p src/Identity.ORM -s src/Identity.WebApi
```

The current schema is created by the `InitialCreate` migration (`Users` and `RefreshTokens`
tables; unique index on `Users.Email`; index on `RefreshTokens.TokenHash`; FK
`RefreshTokens.UserId → Users.Id` with cascade delete).
