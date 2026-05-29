---
sidebar_position: 3
title: Running Tests
description: How to run unit tests, integration tests, and generate coverage reports
---

# Running Tests

OrderHub has a comprehensive test suite with 52 unit tests and integration tests using Testcontainers.

## Test Architecture

| Project | Type | Count | Requires Docker |
|---------|------|-------|----------------|
| `OrderHub.UnitTests` | Handler + validator tests | 52 | No |
| `OrderHub.IntegrationTests` | WebApplicationFactory + Testcontainers | ~10 | Yes |

### Unit Test Coverage

| Feature Area | Tests |
|-------------|-------|
| Auth | 4 handlers + 4 validators |
| Products | 5 handlers + 2 validators |
| Orders | 5 handlers + 2 validators |
| Reports | 2 handlers + 1 validator + cache tests |
| HTML Sanitization | 12 sanitization tests |

## Run All Tests

```bash
dotnet test OrderHub.slnx
```

## Unit Tests Only

```bash
dotnet test tests/OrderHub.UnitTests
```

No external dependencies required — all repos and services are mocked with Moq.

## Integration Tests

:::info
Integration tests require Docker to be running for Testcontainers (real PostgreSQL instances).
:::

```bash
dotnet test tests/OrderHub.IntegrationTests
```

Integration tests verify:
- **Concurrency** — 20 concurrent order requests against stock=5 → exactly 5 succeed
- **Product query performance** — Projection + covering indexes verified
- **Real database** — Testcontainers spins up a PostgreSQL container per test run

## Coverage Reports

Generate an HTML coverage report for the Application and Domain layers:

```powershell
# PowerShell (Windows)
Remove-Item -Recurse -Force coverage -ErrorAction SilentlyContinue
dotnet test tests/OrderHub.UnitTests --collect:"XPlat Code Coverage" --results-directory ./coverage
reportgenerator `
  -reports:"coverage/**/coverage.cobertura.xml" `
  -targetdir:"coverage/report" `
  -reporttypes:"Html" `
  -assemblyfilters:"+OrderHub.Application;+OrderHub.Domain"
Start-Process coverage/report/index.html
```

```bash
# Bash (macOS/Linux)
rm -rf coverage
dotnet test tests/OrderHub.UnitTests --collect:"XPlat Code Coverage" --results-directory ./coverage
reportgenerator \
  -reports:"coverage/**/coverage.cobertura.xml" \
  -targetdir:"coverage/report" \
  -reporttypes:"Html" \
  -assemblyfilters:"+OrderHub.Application;+OrderHub.Domain"
open coverage/report/index.html
```

## Test Patterns

### Unit Test Structure (AAA Pattern)

```csharp
[Fact]
public async Task Handle_ValidCommand_ReturnsCreatedProduct()
{
    // Arrange
    var command = new CreateProductCommand("SKU-001", "Widget", "A widget", 9.99m, 100, "Electronics");
    _productRepository.Setup(r => r.AddAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Name.Should().Be("Widget");
}
```

### Integration Test with Testcontainers

```csharp
[Fact]
public async Task CreateOrder_ConcurrentRequests_ExactlyStockSucceeds()
{
    // Uses WebApplicationFactory with Testcontainers PostgreSQL
    // 20 concurrent requests against stock=5
    // Asserts exactly 5 succeed and final stock=0
}
```
