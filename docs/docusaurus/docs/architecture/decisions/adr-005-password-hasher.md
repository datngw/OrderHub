---
sidebar_position: 6
title: "ADR-005: PasswordHasher over BCrypt"
description: Decision to use ASP.NET Core built-in PasswordHasher for password hashing
---

# ADR-005: PasswordHasher over BCrypt

## Status

✅ Accepted

## Context

User accounts require secure password hashing. The hashing algorithm must be computationally expensive to resist brute-force attacks, and should support hash format upgrades when stronger algorithms become available in the future.

## Decision

Use **ASP.NET Core built-in `PasswordHasher<T>`** (PBKDF2 with HMAC-SHA256) for password hashing and verification.

## Rationale

Three hashing options were considered:

| Criteria | PasswordHasher\<T\> | BCrypt | Argon2 |
|----------|--------------------|---------|---------|
| **Dependency** | Built-in (zero external deps) | External NuGet package | External NuGet package |
| **Algorithm** | PBKDF2 with HMAC-SHA256 | Blowfish-based | Winner of Password Hashing Competition |
| **Auto-upgrade** | ✅ Detects old format, rehashes on login | ❌ Manual migration | ❌ Manual migration |
| **Framework alignment** | ASP.NET Core Identity standard | Independent library | Independent library |
| **Test coverage** | Microsoft-maintained, extensively tested | Community-tested | Community-tested |

`PasswordHasher<T>` was chosen because:

1. **Zero external dependency** — it ships with ASP.NET Core, no additional NuGet packages
2. **Auto-upgradable hash format** — when the algorithm changes in a future .NET version, passwords are automatically rehashed on next successful login
3. **Consistent with ASP.NET Core Identity patterns** — familiar to any .NET developer
4. **Well-tested and maintained** by Microsoft with regular security updates

BCrypt was rejected because it adds an external dependency for functionality the framework already provides. Argon2 was rejected for the same reason, plus its GPU-resistance advantage is not necessary for this application's threat model.

## Consequences

**Positive:**
- No external dependency — ships with ASP.NET Core
- Auto-upgradable hash format (future algorithm changes handled transparently)
- Consistent with ASP.NET Core Identity patterns
- Well-tested and maintained by Microsoft

**Negative:**
- PBKDF2 is less GPU-resistant than Argon2 (acceptable for this threat model)
- Less configurable than BCrypt (work factor is managed by the framework)
