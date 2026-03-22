# Work Order Management System

A work order management application built with .NET 10.0 implementing Onion Architecture. The system uses Blazor WebAssembly for the UI, Entity Framework Core for data access, MediatR for CQRS, and deploys to Azure Container Apps.

This codebase serves as both a working application and a teaching reference for software architecture. The 51 architectural patterns cataloged below are all demonstrated in the source code.

## Solution Structure

```
src/
  Core/                  Domain layer — models, interfaces, queries (no dependencies)
  DataAccess/            EF Core, MediatR handlers (references Core only)
  Database/              DbUp schema migrations
  UI/Server/             Blazor Server host, Lamar DI
  UI/Client/             Blazor WebAssembly frontend
  UI/Api/                Web API endpoints
  UI.Shared/             Shared UI types
  LlmGateway/            Azure OpenAI integration
  Worker/                Background hosted service
  AppHost/         .NET Aspire orchestration
  ServiceDefaults/ Aspire service defaults
  UnitTests/             NUnit + Shouldly
  IntegrationTests/      NUnit against the AppHost-managed SQL environment
  AcceptanceTests/       NUnit + Playwright
```

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- Docker Desktop or Docker Engine for the AppHost-managed SQL Server container
- [PowerShell 7+](https://github.com/PowerShell/PowerShell) (cross-platform, required for build scripts)
- [Playwright browsers](https://playwright.dev/) (for acceptance tests only)

## Build

```powershell
# Quick build (Windows)
.\build.bat

# Quick build (Linux/macOS)
./build.sh

# Full build — clean, compile, unit tests, AppHost startup, integration tests
. .\build.ps1 ; Build

# dotnet CLI directly
dotnet build src/AISoftwareFactory.slnx --configuration Release
```

## Run Tests

```powershell
# Unit tests
dotnet test src/UnitTests --configuration Release

# Full acceptance test pass
.\AcceptanceTests.ps1
```

## Run Locally

```bash
cd src/AppHost
dotnet run
```

The AppHost starts the SQL Server container, database migrations, `UI.Server`, and `Worker`. The application is available at `https://localhost:7174`. Health check endpoint: `https://localhost:7174/_healthcheck`.

---

# Architecture Patterns Reference

A catalog of 51 architectural patterns and design concepts demonstrated in this codebase, annotated with authoritative reference URLs suitable for student learning.

---

