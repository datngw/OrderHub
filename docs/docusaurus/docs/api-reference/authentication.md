---
sidebar_position: 2
title: Authentication
description: Endpoints for user registration, login, token refresh, and logout
---

# Authentication API

Authentication endpoints allow clients to manage user accounts, log in to obtain JWT access and refresh tokens, rotate expired tokens, and log out.

---

## 1. Register Account

Creates a new user account with the `Customer` role and immediately logs the user in, returning active tokens.

```http
POST /api/v1/auth/register
```

*   **Authentication:** None (Public)
*   **Rate Limit:** 3 requests per minute per IP (`auth-register` policy).

### Request Body (JSON)
```json
{
  "email": "customer@orderhub.com",
  "password": "User@12345",
  "fullName": "John Doe"
}
```

### Input Validation Rules

| Property | Required | Type | Rules & Constraints |
|---|:---:|:---:|---|
| **`email`** | Yes | string | Must be a valid email format. Max 256 characters. Must be unique in the database. |
| **`password`** | Yes | string | Minimum 8 characters. Must contain at least: one uppercase letter `[A-Z]`, one lowercase letter `[a-z]`, one digit `[0-9]`, and one non-alphanumeric character. |
| **`fullName`** | Yes | string | Must not be empty. Max 200 characters. |

### Response (201 Created)
Returns a payload of type `AuthResponse` containing credentials:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsIn...",
  "refreshToken": "7c5e2d6b8f9a0c1d2e3f4a5b6c7d8e9f",
  "email": "customer@orderhub.com",
  "fullName": "John Doe",
  "role": "Customer"
}
```

---

## 2. Login

Authenticates user credentials against the database. On success, issues a stateless JWT access token and a database-backed refresh token.

```http
POST /api/v1/auth/login
```

*   **Authentication:** None (Public)
*   **Rate Limit:** 5 requests per minute per IP (`auth-login` policy).

### Request Body (JSON)
```json
{
  "email": "customer@orderhub.com",
  "password": "User@12345"
}
```

### Response (200 OK)
Returns a payload of type `AuthResponse` containing active tokens and user profile properties:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsIn...",
  "refreshToken": "7c5e2d6b8f9a0c1d2e3f4a5b6c7d8e9f",
  "email": "customer@orderhub.com",
  "fullName": "John Doe",
  "role": "Customer"
}
```

:::info Token Lifetimes
*   **Access Token:** Valid for **15 minutes** (configured in JWT properties). Contains user ID (`sub`), email, and role claims.
*   **Refresh Token:** Valid for **7 days** (configured in database storage limits).
:::

---

## 3. Refresh Token

Rotates an expired JWT access token using a valid, non-expired refresh token.

```http
POST /api/v1/auth/refresh
```

*   **Authentication:** None (Public)
*   **Rate Limit:** 10 requests per minute per IP (`auth-refresh` policy).

### Request Body (JSON)
```json
{
  "refreshToken": "7c5e2d6b8f9a0c1d2e3f4a5b6c7d8e9f"
}
```

### Response (200 OK)
Returns a rotated access token and a brand new refresh token:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsIn...",
  "refreshToken": "f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2c3",
  "email": "customer@orderhub.com",
  "fullName": "John Doe",
  "role": "Customer"
}
```

:::warning Refresh Token Rotation (RTR)
OrderHub implements strict **Refresh Token Rotation**. When a new access token is requested, the old refresh token is revoked immediately. A single-use new refresh token is issued and returned. Clients must discard the old refresh token and store the new token for future requests.
:::

---

## 4. Logout

Revokes the specified refresh token, ending the active session and preventing further access token refreshes.

```http
POST /api/v1/auth/logout
```

*   **Authentication:** Required (Requires a valid JWT in the `Authorization` header)
*   **Rate Limit:** 60 requests per minute per user (`products` rate limiting policy applied to standard authorized requests).

### Request Body (JSON)
```json
{
  "refreshToken": "f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2c3"
}
```

### Response (204 No Content)
Returns an empty response indicating success.
