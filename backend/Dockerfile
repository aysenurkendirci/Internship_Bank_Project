# Builds the API and the database migrator from one source tree; docker-compose.yml picks a stage with `target`.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first: the restore layer is reused until a dependency changes, not on every code change.
COPY src/Bank.Domain/Bank.Domain.csproj src/Bank.Domain/
COPY src/Bank.Contracts/Bank.Contracts.csproj src/Bank.Contracts/
COPY src/Bank.Application/Bank.Application.csproj src/Bank.Application/
COPY src/Bank.Infrastructure/Bank.Infrastructure.csproj src/Bank.Infrastructure/
COPY src/Bank.Api/Bank.Api.csproj src/Bank.Api/
COPY src/Bank.Database/Bank.Database.csproj src/Bank.Database/
RUN dotnet restore src/Bank.Api/Bank.Api.csproj \
 && dotnet restore src/Bank.Database/Bank.Database.csproj

COPY src/ src/
RUN dotnet publish src/Bank.Api/Bank.Api.csproj -c Release --no-restore -o /out/api \
 && dotnet publish src/Bank.Database/Bank.Database.csproj -c Release --no-restore -o /out/migrator


# Applies pending migrations and exits; the API only starts after it has finished successfully.
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS migrator
WORKDIR /app
COPY --from=build /out/migrator .
USER $APP_UID
ENTRYPOINT ["dotnet", "Bank.Database.dll"]


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /out/api .
# The image's non-root user: a compromised process cannot modify the application files.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Bank.Api.dll"]
