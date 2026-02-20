# Checkout Service – Integration Test Automation

This repository contains a structured integration test solution for the CheckoutService API.

The objective of this exercise is to:

- Validate the `/checkout` and `/payment` endpoints
- Verify correct database persistence
- Cover successful and declined payment scenarios
- Automate execution via CI (GitHub Actions)
- Enforce a 70% coverage gate

---

# 1. Architecture Overview

The solution uses a containerized integration test approach:

- SQL Server (Docker)
- CheckoutService API (Docker)
- WireMock payment gateway mock (Docker)
- NUnit integration test project (runs on host/CI runner)

Tests execute black-box HTTP calls against the running API and validate database state directly.

This mirrors a real-world integration environment.

---

# 2. Tech Stack

| Component | Technology |
|------------|------------|
| API | ASP.NET Core (.NET 8) |
| Database | SQL Server 2022 (Docker) |
| Payment Gateway | WireMock |
| Test Framework | NUnit |
| Language | C# |
| DB Access in Tests | Dapper + Microsoft.Data.SqlClient |
| CI | GitHub Actions |
| Coverage | Coverlet (Cobertura XML) |

---

# 3. Test Plan

## 3.1 Functional Scenarios Automated

### 1️⃣ Successful Payment (APPROVED)

**Steps**
- POST `/checkout` with valid items
- POST `/payment` with valid credit card
- Verify:
  - HTTP status code
  - sales_hdr.payment_status = APPROVED
  - sales_lin records exist
  - total is correct

---

### 2️⃣ Declined Payment (DECLINED)

**Steps**
- POST `/checkout`
- POST `/payment` with invalid credit card
- Verify:
  - HTTP status code
  - sales_hdr.payment_status = DECLINED
  - sales_hdr total remains correct

---

### 3️⃣ Checkout Persists Header & Lines

Validates:
- Header inserted correctly
- Line items inserted
- Correct total aggregation

---

# 4. Project Structure

```
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
    appsettings.json

ci/
    check-coverage.sh
```

---

# 5. Running Locally

## 5.1 Start the Full Stack

From `CheckoutService/`:

```bash
docker compose up -d --build
```

This will start:

- SQL Server
- Database initializer
- WireMock payment gateway
- CheckoutService API (port 8080)

---

## 5.2 Run Integration Tests

From repository root:

```bash
dotnet test CheckoutService.IntegrationTests/CheckoutService.IntegrationTests.csproj
```

---

## 5.3 Run With Coverage

```bash
dotnet test CheckoutService.IntegrationTests/CheckoutService.IntegrationTests.csproj   /p:CollectCoverage=true   /p:CoverletOutputFormat=cobertura   /p:CoverletOutput=./TestResults/coverage/
```

---

# 6. CI Pipeline (GitHub Actions)

The pipeline performs:

1. Checkout source
2. Setup .NET 8
3. Build and start Docker stack
4. Wait for API availability
5. Run NUnit integration tests
6. Generate Cobertura coverage report
7. Enforce 70% coverage gate
8. Upload test + coverage artifacts
9. Tear down Docker stack

Coverage gate enforced via:

```
ci/check-coverage.sh
```

If coverage < 70%, pipeline fails.

---

# 7. Coverage Strategy

Because the API runs inside a Docker container (separate process),
the test runner does not instrument the service assembly.

Coverage is enforced on the integration test harness codebase,
ensuring maintainability and quality of the automation layer.

Service-level coverage could be added by instrumenting the API process,
but this was intentionally kept as a true black-box integration test.

---

# 8. Design Decisions

### ✔ Containerized integration testing

Ensures CI validates the real runtime environment.

### ✔ WireMock for payment gateway

Provides deterministic responses:
- APPROVED
- DECLINED

Avoids dependency on external services.

### ✔ Dapper for DB assertions

Lightweight and readable.
Avoids heavy ORM overhead in test code.

### ✔ Structured test infrastructure

Separation of:
- Configuration
- DB assertions
- Test logic

Keeps code clean and maintainable.

---

# 9. How This Reflects Production Practice

This solution demonstrates:

- Real integration testing (not mocked unit tests)
- Container orchestration
- CI/CD automation
- Coverage gating
- Database state validation
- Clean separation of test infrastructure

---

# 10. Future Improvements (Optional Enhancements)

- Add negative validation tests
- Add data cleanup strategy between tests
- Add test parallelization
- Add service-level instrumentation coverage
- Add Allure or HTML reporting

---

# Author

Myles Notario  
QA Automation Engineer
