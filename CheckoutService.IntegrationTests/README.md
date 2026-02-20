# Checkout Service – Integration Test Automation

This repository contains a **containerized black‑box integration test** solution for `CheckoutService`.

It validates the API **end-to-end** by:
- exercising the HTTP endpoints (`/checkout`, `/payment`)
- asserting **database persistence** (header + line items + totals)
- simulating a payment gateway via **WireMock**
- running automatically in **GitHub Actions** with a **coverage gate (≥ 70%)**

> Note: The exercise mentions PostgreSQL; this implementation uses **SQL Server (Docker)** instead. The CI goals (build → run stack → test → publish reports → fail on low coverage) remain the same.

---

## Architecture

The test environment is orchestrated with Docker Compose:

- **SQL Server 2022** (Docker)
- **DB init/seed** container (Docker)
- **WireMock** payment gateway mock (Docker)
- **CheckoutService API** (Docker)
- **NUnit integration tests** (runs on the host / GitHub runner)

Tests interact with the system like a real client:
1) call the API over HTTP  
2) validate resulting DB state via direct SQL queries

---

## Tech stack

| Concern | Tech |
|---|---|
| API | ASP.NET Core (.NET 8) |
| Database | SQL Server 2022 (Docker) |
| Payment mock | WireMock (Docker) |
| Tests | NUnit (C#) |
| DB assertions | Dapper + `Microsoft.Data.SqlClient` |
| CI | GitHub Actions |
| Coverage | Coverlet → Cobertura XML |

---

## Test coverage (what’s automated)

### Checkout persists header + lines
- `POST /checkout`
- Assert:
  - `sales_hdr` created with correct total
  - `sales_lin` records created for items

### Payment approved
- `POST /checkout`
- `POST /payment` with **approved** card data (WireMock)
- Assert:
  - HTTP success response
  - `sales_hdr.payment_status = APPROVED`
  - totals and lines remain correct

### Payment declined
- `POST /checkout`
- `POST /payment` with **declined** card data (WireMock)
- Assert:
  - HTTP success response (or expected failure code—per API contract)
  - `sales_hdr.payment_status = DECLINED`
  - totals remain correct

---

## Repository layout

```text
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
```

---

## Run locally

### 1) Start the stack

From `CheckoutService/`:

```bash
docker compose up -d --build
```

This starts:
- SQL Server
- DB initializer/seed
- WireMock payment gateway
- CheckoutService API (default: `http://localhost:8080`)

### 2) Run integration tests

From repository root:

```bash
dotnet test CheckoutService.IntegrationTests/CheckoutService.IntegrationTests.csproj
```

### 3) Run with coverage (Cobertura)

```bash
dotnet test CheckoutService.IntegrationTests/CheckoutService.IntegrationTests.csproj   /p:CollectCoverage=true   /p:CoverletOutputFormat=cobertura   /p:CoverletOutput=./TestResults/coverage/
```

Coverage output example:
- `CheckoutService.IntegrationTests/TestResults/**/coverage.cobertura.xml`

---

## CI pipeline (GitHub Actions)

The CI pipeline performs:

1. Checkout source
2. Setup .NET SDK
3. Restore + build
4. Build and start Docker Compose stack
5. Wait for API readiness
6. Run NUnit integration tests + generate Cobertura coverage
7. Enforce **coverage gate ≥ 70%** (`ci/check-coverage.sh`)
8. Upload test + coverage artifacts
9. Tear down Docker stack

If coverage is below the threshold, the workflow fails.

---

## Coverage strategy (why coverage is on the test harness)

Because the API runs **in a separate Docker container/process**, the test runner doesn’t instrument the service assembly by default.

So coverage is enforced on the **integration test harness** (tests + infrastructure helpers), which:
- keeps the CI requirement meaningful (coverage gate works)
- ensures the automation layer remains maintainable and exercised

If needed, **service-level coverage** can be added later by instrumenting the API container (e.g., running tests inside the same process space or exporting coverage from the container).

---

## Design decisions

- **Containerized integration testing:** CI validates the real runtime composition (API + DB + external dependency).
- **WireMock for payments:** deterministic APPROVED/DECLINED responses; no external flakiness.
- **Dapper for DB assertions:** lightweight, fast, readable queries.
- **Test infrastructure separation:** config, API client, DB asserts/cleanup kept out of test methods for clarity.

---

## Future enhancements (optional)

- Additional negative/validation tests (bad payloads, missing fields, invalid sale id)
- Stronger isolation between tests (per-test transaction or schema reset strategy)
- Parallel execution (where DB + data model allow)
- HTML reporting (Allure, ReportGenerator)

---

## Author

**Myles Notario**  
QA Automation Engineer
