---
sidebar_position: 1
title: Quick Start
description: Get OrderHub running in 5 minutes with Docker Compose
---

# Quick Start

Get the full OrderHub stack running with a single command — API, PostgreSQL, pgAdmin, and Seq.

## Prerequisites

- [Docker](https://www.docker.com/get-started) installed and running
- Git

## Step 1: Clone & Configure

```bash
git clone <repo-url>
cd OrderHub
cp .env.example .env
```

Edit `.env` with your own secrets:

```env
POSTGRES_DB=orderhub
POSTGRES_USER=orderhub
POSTGRES_PASSWORD=<your-strong-password>

PGADMIN_DEFAULT_EMAIL=admin@orderhub.dev
PGADMIN_DEFAULT_PASSWORD=<your-admin-password>

JWT_KEY=<your-min-32-char-secret>
```

:::warning
Never commit the `.env` file to source control. The `.gitignore` already excludes it.
:::

## Step 2: Start All Services

```bash
docker-compose up --build
```

This starts 4 containers:

| Service | URL | Purpose |
|---------|-----|---------|
| **API** | `http://localhost:5000` | OrderHub REST API |
| **Scalar UI** | `http://localhost:5000/scalar/v1` | Interactive API documentation |
| **pgAdmin** | `http://localhost:5050` | PostgreSQL admin UI |
| **Seq** | `http://localhost:8081` | Structured log viewer |

## Step 3: Verify

### Health Check

```bash
curl http://localhost:5000/health/ready
```

### Create an Account

```bash
curl -X POST http://localhost:5000/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@orderhub.com",
    "password": "Test@12345",
    "fullName": "Test User"
  }'
```

### Login with Seed Account

Two accounts are seeded on first run:

| Role | Email | Password |
|------|-------|----------|
| Admin | `admin@orderhub.com` | `Admin@123` |
| Customer | `customer@orderhub.com` | `User@123` |

```bash
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@orderhub.com",
    "password": "Admin@123"
  }'
```

## Step 4: Explore

- **Scalar UI** at `http://localhost:5000/scalar/v1` — interactive API explorer with auth support
- **Seq** at `http://localhost:8081` — search and filter structured logs
- **pgAdmin** at `http://localhost:5050` — browse the database schema and data

## Stopping

```bash
docker-compose down
```

To remove all data (database volumes):

```bash
docker-compose down -v
```
