---
sidebar_position: 2
title: Authentication
description: Auth endpoints for registration, login, token refresh, and logout
---

# Authentication

## Register

Create a new customer account.

```
POST /api/v1/auth/register
```

**Auth:** None

### Request

```json
{
  "email": "user@example.com",
  "password": "MyPass@123",
  "fullName": "John Doe"
}
```

### Validation Rules

| Field | Rules |
|-------|-------|
| Email | Required, valid email format |
| Password | Required, min 8 chars, must contain: uppercase, lowercase, digit, special character |
| FullName | Required, min 2 chars, max 200 chars |

### Response (201 Created)

```json
{
  "userId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "email": "user@example.com",
  "fullName": "John Doe",
  "role": "Customer"
}
```

---

## Login

Authenticate and receive JWT access + refresh tokens.

```
POST /api/v1/auth/login
```

**Auth:** None

### Request

```json
{
  "email": "user@example.com",
  "password": "MyPass@123"
}
```

### Response (200 OK)

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "tokenType": "Bearer",
  "expiresIn": 900
}
```

:::info
Access tokens expire in **15 minutes**. Refresh tokens expire in **7 days**.
:::

---

## Refresh Token

Get a new access token using a valid refresh token.

```
POST /api/v1/auth/refresh
```

**Auth:** None

### Request

```json
{
  "refreshToken": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

### Response (200 OK)

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "new-refresh-token-uuid",
  "tokenType": "Bearer",
  "expiresIn": 900
}
```

:::warning
The old refresh token is revoked when a new one is issued. You must use the new refresh token for subsequent refreshes.
:::

---

## Logout

Revoke the current refresh token.

```
POST /api/v1/auth/logout
```

**Auth:** Bearer token required

### Request

```json
{
  "refreshToken": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

### Response (200 OK)

```json
{
  "message": "Logged out successfully"
}
```
