---
sidebar_position: 6
title: Health Checks
description: Liveness and readiness probe endpoints
---

# Health Checks API

OrderHub exposes liveness and readiness health probes to facilitate container orchestrator health checking (e.g., Docker container health, Kubernetes, or automated uptime monitoring services).

---

## 1. Liveness Probe

Determines if the API container process is alive and responding to HTTP requests.

```http
GET /health/live
```

*   **Authentication:** None (Public)
*   **Response Format:** Plain Text

### Response (200 OK - Healthy)
Returned when the API container process is running:
```http
Healthy
```

:::tip Container Orchestration
Use the `/health/live` endpoint in Docker container `HEALTHCHECK` definitions or Kubernetes `livenessProbe` specifications. If this endpoint returns a failure (non-2xx) or times out, the container orchestrator will automatically restart the container.
:::

---

## 2. Readiness Probe

Checks if the API process is ready to serve traffic, verifying connectivity to backing services (PostgreSQL database).

```http
GET /health/ready
```

*   **Authentication:** None (Public)
*   **Response Format:** Plain Text

### Response (200 OK - Healthy)
Returned when the API is running and successfully connected to the PostgreSQL database:
```http
Healthy
```

### Response (503 Service Unavailable - Unhealthy)
Returned if the database connection fails, is misconfigured, or times out:
```http
Unhealthy
```

:::warning Readiness vs Liveness
Use `/health/ready` for Kubernetes `readinessProbe` routing rules. If this endpoint returns `Unhealthy` (HTTP 503), the load balancer stops routing traffic to this container until the database connection recovers. Do **not** use the readiness probe for liveness checks, or transient database outages could cause cascading container restarts.
:::
