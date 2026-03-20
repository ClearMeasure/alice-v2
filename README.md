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
  AppHost/               .NET Aspire orchestration
  ServiceDefaults/       Aspire shared defaults (OpenTelemetry, health, discovery)
  UnitTests/             NUnit + Shouldly
  IntegrationTests/      NUnit, LocalDB / SQL Server / SQLite
  AcceptanceTests/       NUnit + Playwright
```

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- One of the following database options:
  - **Windows:** SQL Server LocalDB (included with Visual Studio)
  - **Linux/macOS with Docker:** SQL Server 2022 runs automatically in a container
  - **Linux/macOS without Docker:** SQLite (automatic fallback)
- [PowerShell 7+](https://github.com/PowerShell/PowerShell) (cross-platform, required for build scripts)
- [Playwright browsers](https://playwright.dev/) (for acceptance tests only)

## Build

```powershell
# Quick build (Windows)
.\build.bat

# Quick build (Linux/macOS)
./build.sh

# Full build — clean, compile, unit tests, DB migration, integration tests
. .\build.ps1 ; Build

# dotnet CLI directly
dotnet build src/AISoftwareFactory.slnx --configuration Release
```

## Run Tests

```powershell
# Unit tests
dotnet test src/UnitTests --configuration Release

# Integration tests
dotnet test src/IntegrationTests --configuration Release

# Acceptance tests (install Playwright browsers first)
pwsh src/AcceptanceTests/bin/Debug/net10.0/playwright.ps1 install
dotnet test src/AcceptanceTests --configuration Debug
```

## Run Locally

```bash
cd src/UI/Server
dotnet run
```

The application starts at `https://localhost:7174`. Health check endpoint: `https://localhost:7174/_healthcheck`.

---

# Architecture Patterns Reference

A catalog of 51 architectural patterns and design concepts demonstrated in this codebase, annotated with authoritative reference URLs suitable for student learning.

---

