# Checkout Service – Integration Test Automation

This repository contains a **containerized black-box integration test** solution for `CheckoutService`.

It validates the API **end-to-end** by:
- Exercising HTTP endpoints (`/checkout`, `/payment`)
- Verifying database persistence (header, lines, totals, payment status)
- Simulating a payment gateway via WireMock
- Running automatically in GitHub Actions
- Enforcing a **coverage gate ≥ 70%**
- Publishing a live HTML coverage report via **GitHub Pages**

---

# Live Coverage Report

The latest coverage report is automatically published from the `main` branch:

➡️ https://richardnotario.github.io/checkout-service-integration-tests/

## Current Metrics (CI Generated)

- **Line Coverage:** 97.8%
- **Branch Coverage:** 66.6%
- **Coverage Gate Threshold:** 70% (enforced in CI)
- **Status:** Passing

Coverage is regenerated and redeployed on every successful push to `main`.

---

# Architecture Overview

The integration environment is fully containerized via Docker Compose:

- SQL Server 2022 (Docker)
- Database initializer / seed container
- WireMock payment gateway mock
- CheckoutService API
- NUnit integration test project (runs on CI runner)

Tests execute real HTTP calls against the running API and validate database state directly.

This mirrors a realistic production-like integration environment.

---

# Tech Stack

| Concern | Technology |
|----------|------------|
| API | ASP.NET Core (.NET 8) |
| Database | SQL Server 2022 (Docker) |
| Payment Mock | WireMock |
| Test Framework | NUnit |
| Language | C# |
| DB Assertions | Dapper + Microsoft.Data.SqlClient |
| CI/CD | GitHub Actions |
| Coverage | Coverlet + ReportGenerator |
| Coverage Hosting | GitHub Pages |

---

# Automated Test Scenarios

## Checkout Persists Header & Lines

- POST `/checkout`
- Validate:
  - sales_hdr record created
  - sales_lin records created
  - Correct total aggregation

## Payment Approved

- POST `/checkout`
- POST `/payment` with approved card
- Validate:
  - HTTP success response
  - sales_hdr.payment_status = APPROVED
  - Totals remain correct

## Payment Declined

- POST `/checkout`
- POST `/payment` with declined card
- Validate:
  - HTTP response as per contract
  - sales_hdr.payment_status = DECLINED
  - Totals remain correct

---

# Project Structure

CheckoutService/
  docker-compose.yml
  Dockerfile
  Program.cs
  ...

CheckoutService.IntegrationTests/
  Tests/
    CheckoutTests.cs
    PaymentTests.cs
  Infrastructure/
    TestConfig.cs
    DbAsserts.cs
    DbCleanup.cs
    ApiClient.cs
  appsettings.json

ci/
  check-coverage.sh

---

# How the Test Structure Is Designed

The integration test suite follows a clean separation-of-concerns approach:

- `Tests/` → Contains scenario-driven test cases only (business behavior)
- `Infrastructure/ApiClient.cs` → Encapsulates HTTP communication with the API
- `Infrastructure/DbAsserts.cs` → Centralizes SQL validation logic
- `Infrastructure/DbCleanup.cs` → Ensures test isolation between runs
- `Infrastructure/TestConfig.cs` → Handles environment configuration loading

This structure intentionally avoids:

- Hard-coded URLs inside tests
- Inline SQL queries in test methods
- Tight coupling between test logic and infrastructure concerns

Each test represents a business scenario, not an implementation detail.

This keeps the automation maintainable, readable, and production-aligned.

---

# Running Locally

## Start the Full Stack

From `CheckoutService/`:

docker compose up -d --build

This starts:
- SQL Server
- DB initializer
- WireMock
- CheckoutService API (http://localhost:8080)

## Run Integration Tests

From repository root:

dotnet test CheckoutService.IntegrationTests/CheckoutService.IntegrationTests.csproj

## Run with Coverage

dotnet test CheckoutService.IntegrationTests/CheckoutService.IntegrationTests.csproj   /p:CollectCoverage=true   /p:CoverletOutputFormat=cobertura   /p:CoverletOutput=./TestResults/coverage/

---

# CI Pipeline Responsibilities

The GitHub Actions workflow performs:

1. Restore & build
2. Start Docker stack
3. Wait for API readiness
4. Execute NUnit integration tests
5. Generate Cobertura coverage
6. Enforce coverage ≥ 70%
7. Generate HTML report
8. Publish coverage to GitHub Pages
9. Upload artifacts
10. Tear down containers

If coverage drops below 70%, the pipeline fails.

---

# Coverage Strategy

Because the API runs in a separate Docker container process,
the integration test runner does not instrument the service assembly directly.

Therefore:

- Coverage is enforced on the integration test harness
- This guarantees maintainability and quality of the automation layer
- The approach remains a true black-box integration test

Service-level instrumentation could be added later if required.

---

# Design Decisions

- Fully containerized integration testing  
- Deterministic payment simulation via WireMock  
- Lightweight DB assertions via Dapper  
- Clean separation of test infrastructure  
- Automated coverage gating  
- Automated coverage publishing  

---

# Author

Myles Notario  
QA Automation Engineer
