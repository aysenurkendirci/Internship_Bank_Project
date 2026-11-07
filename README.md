# Personal Finance Platform

A personal finance life-management platform built on top of a banking core.
Beyond summarising past spending, it projects future obligations (installments,
subscriptions, credit card statements, recurring expenses) over the next 3–12
months and lets users compare spending scenarios.

> Decision-support and simulation only. The system never makes credit or
> investment recommendations.

## Tech stack

| Layer    | Technology                                   |
|----------|----------------------------------------------|
| Backend  | .NET 10, ASP.NET Core Web API, Dapper        |
| Database | SQL Server, schema migrations with DbUp      |
| Frontend | Angular, Tailwind CSS                        |
| Auth     | JWT bearer tokens                            |

Backend follows Clean Architecture: `Domain` ← `Application` ← `Infrastructure` / `Api`,
with request/response DTOs in `Contracts`.

## Local setup

### Database

Requires a local SQL Server instance reachable with Windows Authentication.
The migrator creates `BankDb` if it does not exist and applies pending scripts:

```bash
cd backend
dotnet run --project src/Bank.Database
```

Schema changes are added as new numbered scripts under
`backend/src/Bank.Database/Migrations/` (`V002__...sql`). An applied script is never edited.

> `TrustServerCertificate=True` in the connection string is for local development only.
>
> When running scripts by hand with `sqlcmd`, pass `-I`: tables with filtered indexes
> require `QUOTED_IDENTIFIER ON`, which `sqlcmd` disables by default.

### Backend

Secrets are never committed. Configure them once per machine with
[user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets):

```bash
cd backend/src/Bank.Api
dotnet user-secrets set "Jwt:Key" "<random string, at least 32 characters>"
dotnet run
```

The API fails fast at startup if `Jwt:Key` is missing.

### Tests

```bash
cd backend
dotnet test
```

### Frontend

```bash
cd frontend
npm install
npm start
```
