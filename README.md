# Personal Finance Platform

Most budgeting apps summarise the past. This one looks ahead: it combines salary, rent, bills, subscriptions,
credit card statements and installment plans into a day-by-day cash-flow projection for the next 3–12 months,
and lets you compare installment options before you buy.

> Decision support and simulation only. The system never recommends credit, investments or purchases; it makes
> the user's own obligations visible.

## What it does

| Feature | Details |
|---|---|
| **Cash-flow projection** | Card spending is collected per statement and leaves the account on the due date, not on the day of purchase. Weekend shifts, month-end clamping and same-day ordering are modelled ([ADR-0003](docs/adr/0003-cash-flow-projection-assumptions.md)). Reports the *lowest* balance within each month, so a month that closes positive but dips below zero before payday is still visible. |
| **Installment scenarios** | "3 or 6 installments?" — each option with its own price, compared over a common horizon against not buying at all. Options are never ranked. |
| **Recurring items & cards** | Income, expenses, subscriptions, credit cards with statement/due days, installment plans with cent-exact schedules. |
| **Statement import** | CSV exports from other banks: UTF-8 or Windows-1254, `;`/`,`/tab, signed amount or debit/credit columns, Turkish number formats. Re-importing or overlapping statements adds only new lines. |
| **Analytics** | Category trends, recurring payment detection (with one-click tracking), anomaly detection with a robust z-score, and an everyday-spending forecast shown as a 95% range. |
| **Synthetic data** | A reproducible profile generator (seeded) for demos and for evaluating the analytics against known answers. |

The analytics are evaluated on held-out synthetic profiles; method, results and known limitations are in
[`docs/analytics/EVALUATION.md`](docs/analytics/EVALUATION.md).

## Tech stack

| Layer | Technology |
|---|---|
| Backend | .NET 10, ASP.NET Core Web API, Dapper, Clean Architecture (`Domain` ← `Application` ← `Infrastructure` / `Api`) |
| Database | SQL Server, versioned migrations with DbUp, temporal tables for a full change history of user data |
| Frontend | Angular 21 (standalone, signals, zoneless), Tailwind CSS, Apache ECharts |
| Tests | xUnit + NSubstitute, integration tests on a real SQL Server via `WebApplicationFactory`, Vitest |
| Security | PBKDF2 password hashing, JWT, rate-limited auth endpoints, RFC 9457 problem details, security headers, masked personal data in logs |

## Project layout

```
backend/
  src/
    Bank.Domain/          pure business rules: Money, recurrence, card cycles, projection, scenarios, analytics
    Bank.Application/     use cases, CSV parsing, synthetic data, ports (repository interfaces)
    Bank.Infrastructure/  SQL Server repositories (Dapper), password hashing, JWT
    Bank.Api/             HTTP: controllers, error handling, rate limiting, security headers
    Bank.Contracts/       request/response DTOs
    Bank.Database/        DbUp migrator and SQL scripts
  tests/                  unit and integration tests
  tools/Bank.Evaluation/  reproduces every figure in the evaluation report
frontend/                 Angular app (pages are containers, display parts are presentational components)
docs/                     engineering log, ADRs, backlog, evaluation, design prompt
```

## Running locally

Requirements: .NET 10 SDK, Node 22, a local SQL Server reachable with Windows authentication.

```bash
# 1. Database: creates BankDb and applies pending migrations
cd backend
dotnet run --project src/Bank.Database

# 2. API (http://localhost:5164); secrets are never committed
cd src/Bank.Api
dotnet user-secrets set "Jwt:Key" "<random string, at least 32 characters>"
dotnet run

# 3. Web app (http://localhost:4200); /api is proxied to the backend
cd ../../../frontend
npm install
npm start
```

After signing up, *İşlem geçmişi → 12 aylık demo verisi oluştur* fills the account with a realistic history.

Schema changes go into new numbered scripts under `backend/src/Bank.Database/Migrations/` (`V004__...sql`); an
applied script is never edited. When running scripts by hand with `sqlcmd`, pass `-I` (filtered indexes need
`QUOTED_IDENTIFIER ON`). `TrustServerCertificate=True` in the connection string is for local development only.

## Tests

```bash
cd backend && dotnet test          # unit + integration (needs SQL Server)
cd frontend && npx ng test --watch=false
```

- `Bank.UnitTests` covers the domain rules, parsing, analytics and services without external dependencies.
- `Bank.IntegrationTests` hosts the API in memory against `BankDb_IntegrationTests`, rebuilt from the migrations
  on every run. Point it elsewhere with the `INTEGRATION_TESTS_DB` environment variable.
- CI (`.github/workflows/ci.yml`) runs both against a SQL Server container, plus the web build and specs.

```bash
# Evaluation of the analytics (writes the tables in docs/analytics/EVALUATION.md)
cd backend && dotnet run --project tools/Bank.Evaluation -c Release
```

## Documentation

- [`docs/ENGINEERING_LOG.md`](docs/ENGINEERING_LOG.md) — every problem found and how it was solved, in order
- [`docs/adr/`](docs/adr/) — architecture decisions (database, projection assumptions, session hardening, right to erasure)
- [`docs/analytics/EVALUATION.md`](docs/analytics/EVALUATION.md) — evaluation of the analytics
- [`docs/REPORT.md`](docs/REPORT.md) — project summary
- [`docs/BACKLOG.md`](docs/BACKLOG.md) — sprint backlog and retrospectives
- [`docs/design/STITCH_PROMPT.md`](docs/design/STITCH_PROMPT.md) — prompt for the visual design
