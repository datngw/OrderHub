---
sidebar_position: 8
title: "ADR-007: Serilog + Seq for Observability"
description: Decision to use Serilog structured logging with Seq for local development visualization
---

# ADR-007: Serilog + Seq for Observability

## Status

✅ Accepted

## Context

The system needs structured logging that supports correlation IDs, sensitive data redaction, and local development visualization. Logs must be searchable and filterable for debugging. Future plans include OpenTelemetry integration for distributed tracing.

## Decision

Use **Serilog** with three sinks: Console, File (rolling JSON), and Seq (development). Configure enrichers for environment, process, thread, span (TraceId/SpanId), and exception details. Implement custom `SensitiveDataDestructuringPolicy` and `SensitiveLogEventFilter` to redact JWT tokens and PII.

## Rationale

Three logging approaches were evaluated:

| Criteria | Serilog + Seq | Microsoft.Extensions.Logging only | NLog |
|----------|--------------|----------------------------------|------|
| **Structured logging** | ✅ Native (log events as objects) | ⚠️ Basic structured logging | ⚠️ Supported but less ergonomic |
| **Sink ecosystem** | ✅ Rich (Console, File, Seq, Elasticsearch, etc.) | Limited (providers per target) | Good (targets) |
| **Enrichers** | ✅ Rich ecosystem (span, environment, correlation) | Manual | Manual |
| **Local dev visualization** | ✅ Seq (powerful search/filter UI) | None built-in | None built-in |
| **Sensitive data control** | ✅ Custom destructuring policies and filters | Manual middleware | Manual |
| **OpenTelemetry readiness** | ✅ `Serilog.Enrichers.Span` for TraceId/SpanId | OTel SDK direct | OTel integration |

Serilog was chosen because:

1. **Rich structured logging** — log events are first-class objects with properties, enabling powerful filtering and search
2. **Seq for local development** — provides a web UI for searching, filtering, and analyzing logs during development
3. **Sensitive data control** — custom destructuring policies and filters ensure no passwords, tokens, or PII appear in logs
4. **OpenTelemetry-ready** — `Serilog.Enrichers.Span` is installed and ready for TraceId/SpanId correlation when OTel SDK is configured

Microsoft.Extensions.Logging was rejected because it lacks the sink ecosystem and local development visualization. NLog was rejected because Serilog's structured logging model is more ergonomic and its enricher ecosystem is richer.

## Consequences

**Positive:**
- Rich structured logging with first-class log event objects
- Seq provides powerful local log search and filtering
- Sensitive data redaction via custom policies (no JWT/PII in logs)
- `Serilog.Enrichers.Span` ready for OpenTelemetry correlation
- Rolling JSON files for production log persistence

**Negative:**
- Seq requires a license for production use (plan: use Loki/ELK for production)
- Serilog adds a dependency on top of Microsoft.Extensions.Logging
- Additional configuration complexity compared to built-in logging
