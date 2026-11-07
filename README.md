# NeoBank

A retail banking backend with a personal finance planner on top. The banking side handles accounts, IBANs, cards and
transfers between customers with a double-entry ledger; the planning side combines salary, rent, bills, subscriptions,
card statements and installment plans into a day-by-day cash-flow projection for the next 3–12 months.

> The planning features are decision support only. The system never recommends credit, investments or purchases; it
> makes the user's own obligations visible.

## What it does

### Banking

| Feature | Details |
|---|---|
| **Accounts** | Demand and foreign currency accounts with generated Turkish IBANs (mod-97 check digits). Statements are read from the ledger. |
| **Transfers** | Money moves between two accounts inside one database transaction and is recorded as one debit and one credit. Requires an `Idempotency-Key`: a retried request returns the original transfer instead of sending the money twice. The recipient name must match the account holder, as in FAST/EFT. Concurrent transfers cannot overdraw an account or deadlock each other. |
| **Cards** | Contactless and online use, daily/monthly limits, temporary block. Only the last four digits of a card number are stored (PCI DSS). |
| **Events** | Every completed transfer emits a `TransferCompleted` event through a transactional outbox to RabbitMQ, so an event exists exactly when its transfer does. |

### Personal finance

| Feature | Details |
|---|---|
| **Cash-flow projection** | Card spending is collected per statement and leaves the account on the due date, not on the day of purchase. Weekend shifts, month-end clamping and same-day ordering are modelled ([ADR-0003](docs/adr/0003-cash-flow-projection-assumptions.md)). Reports the *lowest* balance within each month, so a month that closes positive but dips below zero before payday is still visible. |
| **Installment scenarios** | "3 or 6 installments?" — each option with its own price, compared over a common horizon against not buying at all. Options are never ranked. |
| **Recurring items & credit cards** | Income, expenses, subscriptions, credit cards with statement/due days, installment plans with cent-exact schedules. |
| **Statement import** | CSV exports from other banks: UTF-8 or Windows-1254, `;`/`,`/tab, signed amount or debit/credit columns, Turkish number formats. Re-importing or overlapping statements adds only new lines. |
| **Analytics** | Category trends, recurring payment detection, anomaly detection with a robust z-score, and an everyday-spending forecast shown as a 95% range ([evaluation](docs/analytics/EVALUATION.md)). |

### Privacy and operations

KVKK data export ("download my data"), masked identity numbers in logs, a full change history of user data in temporal
tables, structured JSON logs with a correlation id per request, liveness/readiness health checks.

## Architecture

```
Api  ──►  Application  ──►  Domain
 │         (commands,         (Money, Iban, Account, FundsTransfer,
 │          queries,           projection, scenarios, analytics)
 │          pipeline)
 ▼              ▲
Infrastructure ─┘  (SQL Server via Dapper, unit of work, outbox, RabbitMQ, JWT)
```

- **CQRS with MediatR** ([ADR-0006](docs/adr/0006-cqrs-with-mediatr.md)): every endpoint maps to one command or query
  in `Features/<Area>/`. Controllers only translate HTTP.
- **Pipeline behaviours:** request timing and slow-request warnings, FluentValidation, and a database transaction
  around commands marked `ITransactionalCommand`.
- **Write side through the domain, read side straight from SQL:** commands load domain objects and let them enforce
  the rules; queries return the screen's shape directly.
- **Defence in depth:** rules are checked in the domain and repeated as database constraints (non-negative balances,
  one debit and one credit per transfer, composite ownership keys).

## Tech stack

| Layer | Technology |
|---|---|
| Backend | .NET 10, ASP.NET Core Web API, MediatR, FluentValidation, Dapper |
| Database | SQL Server, DbUp migrations, temporal tables |
| Messaging | RabbitMQ (topic exchange, publisher confirms) behind a transactional outbox |
| Frontend | Angular 21 (standalone, signals, zoneless), Tailwind CSS, Apache ECharts |
| Observability | Serilog (JSON logs), correlation ids, health checks |
| Security | PBKDF2 password hashing, JWT, rate-limited auth endpoints, RFC 9457 problem details, security headers |
| Tests | xUnit + NSubstitute, integration tests on a real SQL Server via `WebApplicationFactory`, Vitest |
| Delivery | Docker Compose, GitHub Actions (warnings as errors, vulnerability scan, SQL Server and RabbitMQ containers) |

## Project layout

```
backend/
  src/
    Bank.Domain/          business rules: Money, Iban, accounts and transfers, cards, projection, scenarios, analytics
    Bank.Application/     commands and queries (Features/), pipeline behaviours, ports, CSV parsing, synthetic data
    Bank.Infrastructure/  Dapper repositories, unit of work, outbox and RabbitMQ publisher, password hashing, JWT
    Bank.Api/             controllers, error handling, logging, rate limiting, security headers
    Bank.Contracts/       request/response DTOs and integration events
    Bank.Database/        DbUp migrator and SQL scripts
  tests/                  unit and integration tests
  tools/Bank.Evaluation/  reproduces every figure in the analytics evaluation
frontend/                 Angular app (pages are containers, display parts are presentational components)
docs/                     engineering log, ADRs, backlog, report
```

## Running

### With Docker

```bash
cp .env.example .env        # fill in the three secrets
docker compose up --build
```

Web app on http://localhost:8080, RabbitMQ console on http://localhost:15672. Migrations run before the API starts.

### Without Docker

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

Events then stay in the outbox; set `Messaging:Enabled` to `true` and point `Messaging:RabbitMq` at a broker to
publish them.

Schema changes go into new numbered scripts under `backend/src/Bank.Database/Migrations/`; an applied script is never
edited. When running scripts by hand with `sqlcmd`, pass `-I` (filtered indexes need `QUOTED_IDENTIFIER ON`).

## Tests

```bash
cd backend && dotnet test          # unit + integration (needs SQL Server)
cd frontend && npx ng test --watch=false
```

- `Bank.UnitTests` covers the domain rules, parsing, analytics, handlers and the validation pipeline.
- `Bank.IntegrationTests` hosts the API in memory against `BankDb_IntegrationTests`, rebuilt from the migrations on
  every run (`INTEGRATION_TESTS_DB` points it elsewhere). It includes concurrency tests: ten transfers racing for one
  balance, opposite transfers in parallel and five simultaneous retries with one idempotency key.
- The RabbitMQ publisher test runs when `RABBITMQ_HOST` is set; CI starts a broker for it.

## Documentation

- [`docs/ENGINEERING_LOG.md`](docs/ENGINEERING_LOG.md) — every problem found and how it was solved, in order
- [`docs/adr/`](docs/adr/) — architecture decisions
- [`docs/REPORT.md`](docs/REPORT.md) — project summary
- [`docs/BACKLOG.md`](docs/BACKLOG.md) — sprint backlog and retrospectives
- [`docs/analytics/EVALUATION.md`](docs/analytics/EVALUATION.md) — evaluation of the analytics
- [`docs/design/STITCH_PROMPT.md`](docs/design/STITCH_PROMPT.md) — prompt for the visual design
