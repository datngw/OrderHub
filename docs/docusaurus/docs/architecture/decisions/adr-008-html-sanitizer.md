---
sidebar_position: 9
title: "ADR-008: HtmlSanitizer for XSS Prevention"
description: Decision to use HtmlSanitizer to strip all HTML from string inputs for XSS prevention
---

# ADR-008: HtmlSanitizer for XSS Prevention

## Status

✅ Accepted

## Context

String inputs such as product names, descriptions, and user full names could contain malicious HTML or JavaScript. If stored and later rendered without sanitization, these inputs enable stored XSS attacks. The system needs comprehensive, automatic protection that cannot be bypassed by forgetting to sanitize a particular field.

## Decision

Use **HtmlSanitizer v9.0.892** to strip all HTML from string inputs. A `SanitizeHtmlEndpointFilter` auto-applies sanitization to all string properties on request DTOs via reflection, ensuring no input is missed.

## Rationale

Four XSS prevention approaches were considered:

| Criteria | HtmlSanitizer (strip all) | Manual encoding (per field) | Input validation (regex) | Output encoding only |
|----------|--------------------------|----------------------------|--------------------------|---------------------|
| **Coverage** | ✅ Automatic via reflection | ❌ Manual per field (error-prone) | ⚠️ Blocklist approach (misses edge cases) | ⚠️ Depends on template engine |
| **Zero-tolerance** | ✅ All HTML stripped | ⚠️ Developer chooses what to encode | ❌ May allow "safe" HTML | ❌ Only at render time |
| **Bypass risk** | ✅ Very low (library-maintained) | High (forgot a field = vulnerability) | High (new XSS vectors emerge) | Medium (context-dependent) |
| **Testability** | ✅ 12 unit tests covering XSS vectors | Per-field tests needed | Regex tests needed | Integration tests |

HtmlSanitizer with auto-sanitization was chosen because:

1. **Complete XSS prevention** — all HTML is stripped, not sanitized; zero-tolerance policy eliminates nuanced decisions about "safe" tags
2. **Automatic application** — `SanitizeHtmlEndpointFilter` uses reflection to sanitize all string properties on request DTOs; developers cannot forget to sanitize a field
3. **Library-maintained protection** — HtmlSanitizer is actively maintained and handles edge cases in HTML parsing that manual approaches miss
4. **Well-tested** — 12 unit tests covering various XSS vectors (`<script>`, `<img onerror>`, `<svg onload>`, encoded variants, etc.)

Manual encoding was rejected because it relies on developers remembering to sanitize every field. Input validation was rejected because it uses a blocklist approach that can miss new XSS vectors. Output encoding was rejected because it depends on the rendering layer and doesn't protect the stored data.

## Consequences

**Positive:**
- Complete XSS prevention for stored content
- Zero-tolerance policy (all HTML stripped, not partially sanitized)
- Automatic application via reflection on all request DTOs — no field can be missed
- 12 unit tests covering various XSS vectors
- Library-maintained protection against evolving XSS techniques

**Negative:**
- Legitimate HTML in inputs is lost (acceptable for this API — no rich text support needed)
- Reflection-based approach adds minimal overhead on each request
- HtmlSanitizer is an external dependency that must be kept up-to-date
