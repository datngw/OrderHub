---
sidebar_position: 11
title: "ADR-010: Category as String (Not Separate Table)"
description: Decision to use a simple string field for product categories instead of a separate table
---

# ADR-010: Category as String (Not Separate Table)

## Status

✅ Accepted — revisit if category management becomes a requirement

## Context

Products need categorization for filtering and browsing. The initial MVP needs a simple way to group products without over-engineering the data model. Categories are currently limited to a small, fixed set (e.g., "Electronics", "Clothing", "Books").

## Decision

Use a **simple string field** on the `Product` entity for category instead of a separate `Category` table with a foreign key relationship.

## Rationale

Three modeling approaches were considered:

| Criteria | String field | Separate Category table | Enum |
|----------|-------------|------------------------|------|
| **Schema complexity** | ✅ No additional table, FK, or join | ❌ Extra table, migration, join queries | ✅ No additional table |
| **Flexibility** | ✅ Any string value | ⚠️ Must insert into Category table first | ❌ Requires code change + deployment |
| **Referential integrity** | ❌ No FK constraint | ✅ Enforced by database | ⚠️ Limited to enum values |
| **Query performance** | ✅ Simple string match | ⚠️ Requires join | ✅ Integer comparison |
| **Filtering** | String matching (`WHERE Category = '...'`) | FK join | Enum comparison |

String field was chosen because:

1. **Simplest schema for MVP** — no additional table, migration, or join overhead
2. **Easy to add categories** — no schema change or admin UI needed to introduce new categories
3. **Easy to normalize later** — when category management becomes a requirement, migrating to a separate table is straightforward (add table, create FK, update queries)
4. **No premature optimization** — the current use case doesn't need category metadata, hierarchy, or admin management

A separate Category table was rejected as over-engineering for the MVP. Enum was rejected because adding categories requires a code change and deployment.

## Consequences

**Positive:**
- Simpler schema — no additional table or foreign key
- Easy to add new categories without schema changes or admin tools
- Straightforward to normalize later when needed

**Negative:**
- No referential integrity on categories — typos or inconsistent values are possible
- Filtering relies on exact string matching (case-sensitive unless indexed with CITEXT)
- No category metadata (description, image, hierarchy) without schema changes
