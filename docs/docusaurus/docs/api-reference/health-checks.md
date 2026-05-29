---
sidebar_position: 6
title: Health Checks
description: Liveness and readiness probe endpoints
---

# Health Checks

## Liveness Probe

Check if the API process is alive and responding.

```
GET /health/live
```

**Auth:** None

### Response (200 OK)

```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "self",
      "status": "Healthy",
      "duration": "00:00:00.0001234"
    }
  ]
}
```

Use this endpoint for container restart policies — if it fails, the process is unresponsive.

---

## Readiness Probe

Check if the API is ready to serve requests, including database connectivity.

```
GET /health/ready
```

**Auth:** None

### Response (200 OK)

```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "self",
      "status": "Healthy",
      "duration": "00:00:00.0001234"
    },
    {
      "name": "postgresql",
      "status": "Healthy",
      "duration": "00:00:00.0054321",
      "data": {
        "connectionString": "Host=orderhub-db;Database=orderhub;..."
      }
    }
  ]
}
```

### Response (503 Service Unavailable)

If the database is unreachable:

```json
{
  "status": "Unhealthy",
  "checks": [
    {
      "name": "self",
      "status": "Healthy",
      "duration": "00:00:00.0001234"
    },
    {
      "name": "postgresql",
      "status": "Unhealthy",
      "duration": "00:00:05.0012345",
      "exception": "Npgsql.NpgsqlException: Failed to connect..."
    }
  ]
}
```

:::tip
Use `/health/ready` for Kubernetes readiness probes or Docker health checks to prevent traffic from reaching the API before the database is ready.
:::
