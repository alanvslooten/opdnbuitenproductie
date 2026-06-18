# syntax=docker/dockerfile:1

# ── Build-stage: SDK 10, restore + publish van de API ──
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Eerst alleen de projectbestanden kopiëren → betere layer-cache voor restore.
COPY src/KinderKompas.Domain/KinderKompas.Domain.csproj src/KinderKompas.Domain/
COPY src/KinderKompas.Application/KinderKompas.Application.csproj src/KinderKompas.Application/
COPY src/KinderKompas.Infrastructure/KinderKompas.Infrastructure.csproj src/KinderKompas.Infrastructure/
COPY src/KinderKompas.Api/KinderKompas.Api.csproj src/KinderKompas.Api/
RUN dotnet restore src/KinderKompas.Api/KinderKompas.Api.csproj

# Daarna de rest van de broncode + publishen.
COPY src/ src/
RUN dotnet publish src/KinderKompas.Api/KinderKompas.Api.csproj -c Release -o /app --no-restore

# ── Runtime-stage: alleen de ASP.NET-runtime ──
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Production
# Render injecteert PORT (standaard 10000); Program.cs leest die en bindt erop.
EXPOSE 10000

ENTRYPOINT ["dotnet", "KinderKompas.Api.dll"]
