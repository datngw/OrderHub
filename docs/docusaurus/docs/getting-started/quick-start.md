---
sidebar_position: 1
title: Quick Start
description: Get OrderHub running in 5 minutes with Docker Compose
---

# Quick Start

Get the full OrderHub stack running locally in under 5 minutes using Docker Compose. This starts the API, PostgreSQL database, pgAdmin console, and the Seq structured log explorer.

## Prerequisites

Ensure you have the following installed on your machine:
*   [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or Docker Engine with Docker Compose)
*   Git

---

## Step 1: Clone & Configure

1.  Clone the repository and navigate into the project directory:
    ```bash
    git clone <repo-url>
    cd OrderHub
    ```
2.  Copy the example environment file to create your active configurations:
    ```bash
    cp .env.example .env
    ```
3.  Open the newly created `.env` file and set secure passwords and keys:
    ```env
    POSTGRES_DB=orderhub
    POSTGRES_USER=orderhub
    POSTGRES_PASSWORD=SetSecurePassword123!
    
    PGADMIN_DEFAULT_EMAIL=admin@orderhub.dev
    PGADMIN_DEFAULT_PASSWORD=SetSecureAdminPassword123!
    
    JWT_KEY=AtLeast32CharactersSecretKeyStringHere!
    ```

:::warning
Never commit the `.env` file to version control. It is already added to `.gitignore` to prevent secret leaks.
:::

---

## Step 2: Start All Services

Run the following command to build and launch the container stack in the background:

```bash
docker-compose up --build -d
```

This command builds and runs **4 coordinated services**:

| Container Name | External URL | Design Purpose |
|---|---|---|
| **`orderhub-api`** | `http://localhost:5000` | The .NET 8 Web API process. |
| **`orderhub-scalar`** | `http://localhost:5000/scalar/v1` | Scalar interactive API documentation and explorer. |
| **`orderhub-db`** | `localhost:5432` (Internal) | PostgreSQL database server (persisted in volume `postgres-data`). |
| **`orderhub-pgadmin`** | `http://localhost:5050` | pgAdmin web console to browse schemas and query tables. |
| **`orderhub-seq`** | `http://localhost:8081` | Seq structured log engine and search UI. |

---

## Step 3: Autonomic Migration & Seeding Verification

When the API container launches, it automatically executes pending database migrations, registers the PostgreSQL `pg_trgm` extension, and seeds test data if the tables are empty.

### 1. Wait for database seeding to complete
Because the seeder generates a production-scale catalog of **10,000 product items** (batched in groups of 2,000 to prevent startup memory spikes), the initialization process can take 5 to 10 seconds on first run.

### 2. Check API Health
You can verify that the database is migrated and the API is ready by querying the readiness health probe:

```bash
curl http://localhost:5000/health/ready
```
**Expected Response (HTTP 200):**
```json
{
  "status": "Healthy"
}
```

---

## Step 4: Verify Auth and API Access

The seeder generates two default user accounts with pre-hashed passwords:

| User Role | Seed Email | Seed Password | Action Capabilities |
|---|---|---|---|
| **Admin** | `admin@orderhub.com` | `Admin@123` | Create/update/delete products, update order status, run analytics reports. |
| **Customer** | `customer@orderhub.com` | `User@123` | Browse catalog, create orders (checkouts), view and cancel own orders. |

### 1. Login to obtain a JWT Token
Execute the following request to authenticate as a Customer:

```bash
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "customer@orderhub.com",
    "password": "User@123"
  }'
```
**Expected Response (HTTP 200):**
```json
{
  "accessToken": "eyJhbGciOi...",
  "refreshToken": "7c5e2d6b..."
}
```

### 2. Search the Product Catalog (GIN Trigram Search)
You can search the 10,000 products case-insensitively using the substring query filter:

```bash
curl "http://localhost:5000/api/v1/products?search=Samsung&pageSize=2"
```

---

## Step 5: Clean Up

To stop all running services while preserving database records:
```bash
docker-compose down
```

To stop all services and delete the database storage volume permanently (e.g., to force a fresh seed run on next start):
```bash
docker-compose down -v
```
