# GitHub Actions Integration Build Setup

This document explains how to set up the GitHub Actions integration build workflow based on the existing Azure Pipelines configuration.

## Overview

The integration build workflow has been created based on `src/pure-azdo-pipeline.yml` and mirrors the build steps from the Azure Pipelines Integration_Build stage. It now starts the development environment through `src/AppHost` rather than provisioning LocalDB or ad hoc Docker resources in the workflow.

## Installation Steps

1. **Move the workflow file to the correct location:**
   ```bash
   mkdir -p .github/workflows
   mv integration-build.yml .github/workflows/integration-build.yml
   ```

2. **Commit and push the workflow:**
   ```bash
   git add .github/workflows/integration-build.yml
   git commit -m "Add GitHub Actions integration build workflow"
   git push
   ```

3. **Verify the workflow:**
   - Navigate to the "Actions" tab in your GitHub repository
   - You should see the "Integration Build" workflow listed
   - The workflow will run automatically on all pushes and pull requests

## Workflow Features

### Build Environment
- **Runner:** `windows-latest` and `ubuntu-latest`
- **Environment orchestration:** .NET Aspire AppHost
- **SQL Server:** AppHost-managed SQL Server container
- **.NET SDK:** 10.0.x

### Build Steps
The workflow executes the following steps in order:
1. Checkout code
2. Setup .NET SDK and Runtime
3. Build the solution
4. Start AppHost
4. Set version environment variable (format: `MAJOR.MINOR.RUN_NUMBER`)
5. Run `build.ps1` with `Build` function, which:
    - Cleans and restores dependencies
    - Compiles the solution
    - Runs unit tests with code coverage
    - Starts the AppHost-managed SQL environment
    - Runs integration tests with code coverage
    - Creates NuGet packages

### Artifacts Retained
All test artifacts and logs are kept for 30 days:
- **Test Results:** All `.trx` files from `build/test/**/*.trx`
- **Code Coverage:** Cobertura XML files from `build/test/**/coverage.cobertura.xml`
- **Build Logs:** Any log or binlog files from the build directory
- **NuGet Packages:** All `.nupkg` files created during the build

### Test Reporting
- Test results are published using the `publish-unit-test-result-action`
- Code coverage is uploaded to Codecov (optional, can be removed if not using Codecov)
- All artifacts are available for download from the workflow run page

## Differences from Azure Pipelines

The GitHub Actions workflow focuses on the Integration Build stage only and does not include:
- Docker build and push (separate job in Azure Pipelines)
- Deployment stages (TDD, UAT, PROD)
- NuGet feed publishing (requires Azure Artifacts configuration)

These can be added as separate workflows if needed.

## Version Numbering

The workflow uses semantic versioning:
- Major version: `1` (configured via `MAJOR_VERSION` env var)
- Minor version: `3` (configured via `MINOR_VERSION` env var)
- Build number: GitHub run number (automatic)

Format: `MAJOR.MINOR.RUN_NUMBER` (e.g., `1.3.42`)

## AppHost-managed SQL environment

The workflow starts `src/AppHost`, which owns SQL Server container startup, database migrations, `UI.Server`, and `Worker`. Integration and acceptance tests connect to the SQL Server instance exposed by AppHost on `localhost,1433`.

## Troubleshooting

### AppHost startup issues
If the build cannot reach `https://localhost:7174/_healthcheck`, inspect the AppHost logs captured by the build script under `build\apphost.stdout.log` and `build\apphost.stderr.log`.

### Test Failures
All test results are uploaded as artifacts even if tests fail. Check the "Artifacts" section of the workflow run to download and review test results.

### Build Failures
Build logs are uploaded as artifacts. Download them from the workflow run page to diagnose issues.

## Customization

To customize the workflow:
- **Change triggers:** Modify the `on:` section
- **Adjust retention:** Change `retention-days:` values
- **Version scheme:** Update `MAJOR_VERSION` and `MINOR_VERSION` env vars
- **Build configuration:** Modify `BUILD_CONFIGURATION` env var (default: Release)
