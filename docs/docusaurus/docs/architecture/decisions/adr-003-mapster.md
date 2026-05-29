---
sidebar_position: 4
title: "ADR-003: Mapster over AutoMapper"
description: Decision to use Mapster for object mapping between entities and DTOs
---

# ADR-003: Mapster over AutoMapper

## Context

OrderHub needs object mapping between domain entities and DTOs at the API boundary. Entities must never be exposed directly to clients — all data flows through request and response DTOs. The mapping library should be fast, type-safe, and minimize boilerplate.

## Decision

Use **Mapster 10.0.7** for compile-time code generation-based object mapping.

## Rationale

Three approaches were considered:

| Criteria | Mapster | AutoMapper | Manual mapping |
|----------|---------|------------|----------------|
| **Performance** | Compile-time code generation (no reflection) | Runtime reflection-based | Fastest (hand-written) |
| **Type safety** | Compile-time checking | Runtime profile validation | Compile-time checking |
| **Boilerplate** | Minimal | Minimal | High (per-entity mapping code) |
| **Community** | Smaller but active | Large, industry standard | N/A |
| **Debugging** | Generated code is readable | Profile resolution can be opaque | Fully transparent |

Mapster was chosen because:

1. **Compile-time code generation** eliminates runtime reflection overhead, producing faster mapping code
2. **Type safety at compile time** — mapping errors are caught during build, not at runtime
3. **Less magic** — mappings are explicit and generated code is inspectable
4. Mapster's API is simpler and requires less configuration than AutoMapper profiles

AutoMapper was rejected because its runtime reflection adds overhead and its profile-based configuration can hide mapping errors until runtime. Manual mapping was rejected because it creates too much boilerplate for the number of entity-to-DTO mappings in the project.

## Consequences

**Positive:**
- Faster execution via compile-time code generation (no runtime reflection)
- Compile-time safety — mapping errors caught during build
- Less magic — generated mappings are explicit and inspectable
- Simpler API than AutoMapper profiles

**Negative:**
- Smaller community and ecosystem compared to AutoMapper
- Fewer third-party extensions and integrations
- Less familiarity for developers coming from AutoMapper-heavy projects
