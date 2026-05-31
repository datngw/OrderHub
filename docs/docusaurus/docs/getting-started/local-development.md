---
sidebar_position: 2
title: Local Development
description: Set up OrderHub for local development without Docker containers
---

# Local Development Setup

Follow this guide to set up, build, and run the OrderHub development environment directly on your local machine using the .NET SDK and user secrets.

## Prerequisites

Before starting, install the following development tools:
*   [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
*   [PostgreSQL 16](https://www.postgresql.org/download/) (or Docker Desktop to spin up database-only containers)
*   [dotnet-ef CLI tool](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) (install globally using `dotnet tool install -g dotnet-ef`)
*   An IDE: [Visual Studio 2022](https://visualstudio.microsoft.com/), [JetBrains Rider](https://www.jetbrains.com/rider/), or [VS Code](https://code.visualstudio.com/) with C# Dev Kit.

---

## Step 1: Build the Solution

Restore dependencies and build the Solution file (`OrderHub.slnx`):

```bash
dotnet build OrderHub.slnx
```

---

## Step 2: Database Setup

Select one of the following setups for your PostgreSQL database:

### Option A: PostgreSQL via Docker (Recommended)
You can run a lightweight PostgreSQL database container in Docker, exposing port `5432`:

```bash
docker run -d \
  --name orderhub-db-dev \
  -e POSTGRES_DB=orderhub \
  -e POSTGRES_USER=orderhub \
  -e POSTGRES_PASSWORD=orderhub \
  -p 5432:5432 \
  postgres:16-alpine
```

### Option B: Local PostgreSQL Server
Ensure PostgreSQL 16 is running locally, listening on port `5432`. Connect to your PostgreSQL server using an administration client (like pgAdmin or psql) and create an empty database named `orderhub`.

---

## Step 3: Configure User Secrets

To prevent database credentials from leaking into source files, inject the connection string at runtime using `.NET User Secrets` inside the API project:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Database=orderhub;Username=orderhub;Password=orderhub" \
  --project src/OrderHub.Api
```

Alternatively, you can set the configuration as an environment variable in your shell:

```powershell
# PowerShell (Windows)
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Database=orderhub;Username=orderhub;Password=orderhub"
```

```bash
# Bash (macOS/Linux)
export ConnectionStrings__DefaultConnection="Host=localhost;Database=orderhub;Username=orderhub;Password=orderhub"
```

---

## Step 4: Apply Database Migrations

Use the EF Core CLI to apply migrations to your database. This compiles the schema changes and applies them to your target PostgreSQL database:

```bash
dotnet ef database update \
  --project src/OrderHub.Infrastructure \
  --startup-project src/OrderHub.Api
```

### What happens on execution:
1.  EF Core queries the target database and creates the schema tables (Users, Products, Orders, OrderItems, RefreshTokens).
2.  Registers the `pg_trgm` PostgreSQL extension.
3.  Executes the seeder, inserting the default admin account, customer account, and generating **10,000 product variants** in batched SQL statements.

---

## Step 5: Run the API Server

Start the API project using the run command:

```bash
dotnet run --project src/OrderHub.Api
```

The Kestrel server launches and begins listening on `http://localhost:5000` (and `https://localhost:5001` if local certificates are configured).

### Hot Reload Watch Mode
For rapid development, run the API using watch mode:

```bash
dotnet watch --project src/OrderHub.Api
```
Any modifications made to C# source files will be dynamically compiled and hot-reloaded into the running process without requiring a manual server restart.

---

## Directory Solution Structure

```
OrderHub/
├── src/
│   ├── OrderHub.Domain/           # Core Entities, Domain Errors, and Repository contracts
│   ├── OrderHub.Application/      # CQRS commands/queries, handlers, caching, and behaviors
│   ├── OrderHub.Infrastructure/   # DBContext, Repository implementations, JWT, and Serilog
│   └── OrderHub.Api/              # Minimal API Endpoints, Middleware, and Program.cs
├── tests/
│   ├── OrderHub.UnitTests/        # Use-case handlers, validators, and HTML sanitization tests
│   └── OrderHub.IntegrationTests/ # WebApplicationFactory & Testcontainers database integration
├── OrderHub.slnx                  # Visual Studio XML-based Solution file
└── docker-compose.yml             # Development container stack orchestration
```
