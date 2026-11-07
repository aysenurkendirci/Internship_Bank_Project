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
| Database | Oracle (PL/SQL packages)                     |
| Frontend | Angular, Tailwind CSS                        |
| Auth     | JWT bearer tokens                            |

Backend follows Clean Architecture: `Domain` ← `Application` ← `Infrastructure` / `Api`,
with request/response DTOs in `Contracts`.

## Local setup

### Backend

Secrets are never committed. Configure them once per machine with
[user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets):

```bash
cd backend/src/Bank.Api
dotnet user-secrets set "Jwt:Key" "<random string, at least 32 characters>"
dotnet run
```

The API fails fast at startup if `Jwt:Key` is missing.

### Frontend

```bash
cd frontend
npm install
npm start
```
