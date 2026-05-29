---
sidebar_position: 1
title: "Architecture Decisions"
description: Index of all Architecture Decision Records (ADRs) for OrderHub
---

# Architecture Decisions

Architecture Decision Records (ADRs) capture important architectural decisions made in the OrderHub project. Each ADR documents the context, decision, rationale, and consequences of a significant technical choice.

## ADR Index

| ADR | Title | Status |
|-----|-------|--------|
| [ADR-001](./adr-001-postgresql) | PostgreSQL over SQL Server | ✅ Accepted |
| [ADR-002](./adr-002-pessimistic-locking) | Pessimistic Locking for Stock Control | ✅ Accepted |
| [ADR-003](./adr-003-mapster) | Mapster over AutoMapper | ✅ Accepted |
| [ADR-004](./adr-004-memory-cache) | IMemoryCache with Version-Key Pattern | ✅ Accepted (revisit for multi-instance) |
| [ADR-005](./adr-005-password-hasher) | PasswordHasher over BCrypt | ✅ Accepted |
| [ADR-006](./adr-006-repository-unit-of-work) | Specific Repository + Unit of Work | ✅ Accepted |
| [ADR-007](./adr-007-serilog-seq) | Serilog + Seq for Observability | ✅ Accepted |
| [ADR-008](./adr-008-html-sanitizer) | HtmlSanitizer for XSS Prevention | ✅ Accepted |
| [ADR-009](./adr-009-result-pattern) | Result Pattern over Exceptions | ✅ Accepted |
| [ADR-010](./adr-010-category-string) | Category as String (Not Separate Table) | ✅ Accepted (revisit if needed) |
| [ADR-011](./adr-011-clean-architecture) | Clean Architecture | ✅ Accepted |
