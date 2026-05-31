---
sidebar_position: 3
title: Running Tests
description: How to run unit tests, integration tests, and generate coverage reports
---

# Running Tests

OrderHub features a comprehensive test suite covering both unit tests and database integration tests. This guide explains how to run the test suites, configure testing dependencies, and generate code coverage reports.

## Test Suite Architecture

The tests are separated into two distinct projects in the solution:

| Test Project | Execution Focus | Count | External Dependencies |
|---|---|---|---|
| **`OrderHub.UnitTests`** | Handler logic, FluentValidation rules, and HTML sanitization filters. | 52 | **None.** Uses Moq to mock database repositories. |
| **`OrderHub.IntegrationTests`** | Web API endpoints, transaction boundaries, index queries, and database locks. | ~10 | **Docker.** Spins up real database instances using Testcontainers. |

---

## 1. Run All Solution Tests

Execute the following command in the solution root to run all tests:

```bash
dotnet test OrderHub.slnx
```

---

## 2. Run Unit Tests Only

To run the unit test project without requiring Docker or active backing services:

```bash
dotnet test tests/OrderHub.UnitTests
```

### Unit Test Area Distribution
*   **Authentication (8 tests):** Registration validation, password hashing matching, JWT generation, and token rotation rules.
*   **Products (7 tests):** CRUD command parameters, inactive product filtering, and pagination query handling.
*   **Orders (7 tests):** Checkout availability logic, status changes, and cancellation validation.
*   **Admin Reports (3 tests):** Report query logic and cache version key invalidation checks.
*   **HTML Sanitizer (12 tests):** Confirms that scripts, iframes, onload events, and vector XSS injections are safely stripped.

---

## 3. Run Integration Tests

Integration tests run the full web host in-memory (`WebApplicationFactory`) and connect to a real, isolated PostgreSQL instance managed by **Testcontainers**.

:::info
Ensure Docker Desktop is running before executing integration tests.
:::

```bash
dotnet test tests/OrderHub.IntegrationTests
```

### Key Scenarios Tested
*   **Stock Concurrency Safety:** Simulates 20 concurrent HTTP checkouts against a single product with 5 items in stock. Asserts that exactly 5 requests succeed (HTTP 201) and 15 requests fail (HTTP 409) with zero inventory leakage.
*   **Trigram Index Coverage:** Queries the database using substring filters, verifying that the database utilizes the GIN index for search optimization.
*   **Cache Stampede (Thundering Herd) Protection:** Asserts that only 1 query hit occurs on database misses under high concurrent request volume.

---

## 4. Generate Code Coverage Reports

Generate an HTML report using `ReportGenerator` to analyze code coverage for the Application and Domain projects.

### Prerequisites
Install the global report generator tool:
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

### Generate Coverage (PowerShell - Windows)
```powershell
# Delete legacy coverage directories
Remove-Item -Recurse -Force coverage -ErrorAction SilentlyContinue

# Execute tests and collect xml data
dotnet test tests/OrderHub.UnitTests --collect:"XPlat Code Coverage" --results-directory ./coverage

# Generate HTML visual map
reportgenerator `
  -reports:"coverage/**/coverage.cobertura.xml" `
  -targetdir:"coverage/report" `
  -reporttypes:"Html" `
  -assemblyfilters:"+OrderHub.Application;+OrderHub.Domain"

# View in browser
Start-Process coverage/report/index.html
```

### Generate Coverage (Bash - macOS/Linux)
```bash
# Delete legacy coverage directories
rm -rf coverage

# Execute tests and collect xml data
dotnet test tests/OrderHub.UnitTests --collect:"XPlat Code Coverage" --results-directory ./coverage

# Generate HTML visual map
reportgenerator \
  -reports:"coverage/**/coverage.cobertura.xml" \
  -targetdir:"coverage/report" \
  -reporttypes:"Html" \
  -assemblyfilters:"+OrderHub.Application;+OrderHub.Domain"

# View in browser
open coverage/report/index.html
```

---

## Code Testing Patterns

### AAA Unit Testing Pattern (Example)
Unit tests mock repository dependencies and evaluate handler outputs:

```csharp
[Fact]
public async Task Handle_ValidCommand_ReturnsCreatedProduct()
{
    // Arrange (Setup variables and mocks)
    var command = new CreateProductCommand("SKU-001", "Widget", "A widget", 9.99m, 100, "Electronics");
    _productRepository.Setup(r => r.AddAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);

    // Act (Execute use case)
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert (Verify expectations)
    result.IsSuccess.Should().BeTrue();
    result.Value.Name.Should().Be("Widget");
}
```
