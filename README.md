# NeoBank

Retail banking backend with a personal finance planner on top. Accounts, IBANs, cards and transfers on one side; a day-by-day
cash-flow projection and installment comparison on the other. A portfolio project, not a live service.

The planner only shows the user's own obligations. It does not recommend anything.

## Stack

.NET 10, ASP.NET Core, MediatR, FluentValidation, Dapper, SQL Server (DbUp migrations), RabbitMQ, Serilog, Angular 21, Tailwind,
ECharts. xUnit and Vitest for tests, GitHub Actions for CI.

## Layout

```
backend/src/
  Bank.Domain/          business rules
  Bank.Application/     commands and queries (Features/), pipeline behaviours
  Bank.Infrastructure/  repositories, outbox, RabbitMQ, security
  Bank.Api/             controllers and middleware
  Bank.Contracts/       DTOs and events
  Bank.Database/        DbUp migrator and SQL scripts
backend/tests/          unit and integration tests
frontend/               Angular app
docs/                   decisions (adr/), notes, analytics evaluation
```

## Run

With Docker:

```bash
cp .env.example .env    # fill in the secrets
docker compose up --build
```

The web app is on http://localhost:8080.

Without Docker you need the .NET 10 SDK, Node 22 and a local SQL Server:

```bash
cd backend
dotnet run --project src/Bank.Database      # creates BankDb, applies migrations

cd src/Bank.Api
dotnet user-secrets set "Jwt:Key" "<at least 32 random characters>"
dotnet run                                  # http://localhost:5164

cd ../../../frontend
npm install
npm start                                   # http://localhost:4200
```

After signing up, *İşlem geçmişi → 12 aylık demo verisi oluştur* fills the account with sample data.

## Test

```bash
cd backend && dotnet test                   # integration tests need SQL Server
cd frontend && npx ng test --watch=false
```

Integration tests run against a real database, `BankDb_IntegrationTests`, rebuilt from the migrations on every run. Set
`INTEGRATION_TESTS_DB` to use another server. The RabbitMQ test only runs when `RABBITMQ_HOST` is set.

## Docs

- [Decisions](docs/adr/)
- [Notes](docs/ENGINEERING_LOG.md): problems that took a while to solve
- [TODO](docs/TODO.md)
- [Analytics evaluation](docs/analytics/EVALUATION.md)
