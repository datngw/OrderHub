---
sidebar_position: 2
title: Local Development
description: Set up OrderHub for local development without Docker containers
---

# Local Development Setup

Run OrderHub locally for development and debugging without Docker.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 16](https://www.postgresql.org/download/) (or use Docker for the database only)
- An IDE: Visual Studio 2022, Rider, or VS Code with C# Dev Kit

## Step 1: Build

```bash
dotnet build OrderHub.slnx
```

## Step 2: Database Setup

### Option A: PostgreSQL via Docker (recommended)

```bash
docker run -d \
  --name orderhub-db \
  -e POSTGRES_DB=orderhub \
  -e POSTGRES_USER=orderhub \
  -e POSTGRES_PASSWORD=orderhub \
  -p 5432:5432 \
  postgres:16-alpine
```

### Option B: Local PostgreSQL

Ensure PostgreSQL 16 is running on `localhost:5432` and create a database named `orderhub`.

## Step 3: Configure Connection String

Using .NET User Secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Database=orderhub;Username=orderhub;Password=orderhub" \
  --project src/OrderHub.Api
```

Alternatively, set the environment variable:

```bash
# PowerShell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Database=orderhub;Username=orderhub;Password=orderhub"

# Bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=orderhub;Username=orderhub;Password=orderhub"
```

## Step 4: Apply Migrations

```bash
dotnet ef database update \
  --project src/OrderHub.Infrastructure \
  --startup-project src/OrderHub.Api
```

This creates the database schema and seeds initial data (1 admin + 1 customer + 100 products).

## Step 5: Run the API

```bash
dotnet run --project src/OrderHub.Api
```

The API starts on `http://localhost:5000` by default.

## Project Structure

```
OrderHub/
├── src/
│   ├── OrderHub.Domain/           # Entities, enums, interfaces
│   ├── OrderHub.Application/      # Commands, queries, handlers, validators, DTOs
│   ├── OrderHub.Infrastructure/   # EF Core, repositories, auth, Serilog config
│   └── OrderHub.Api/              # Endpoints, middleware, Program.cs
├── tests/
│   ├── OrderHub.UnitTests/        # Handler and validator tests
│   └── OrderHub.IntegrationTests/ # WebApplicationFactory + Testcontainers
├── OrderHub.slnx
└── docker-compose.yml
```

## Development Workflow

1. **Add a feature** — Create Command/Query, Handler, Validator, DTOs, Endpoint
2. **Run unit tests** — `dotnet test tests/OrderHub.UnitTests`
3. **Run integration tests** — `dotnet test tests/OrderHub.IntegrationTests` (requires Docker)
4. **Check API** — Open Scalar UI at `http://localhost:5000/scalar/v1`
5. **View logs** — Console output shows structured Serilog logs

## Hot Reload

For rapid development, use `dotnet watch`:

```bash
dotnet watch --project src/OrderHub.Api
```

This enables hot reload — code changes are applied without restarting the server.
