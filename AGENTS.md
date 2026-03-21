# AGENTS.md

See `CLAUDE.md` for full project overview, solution structure, architecture, coding standards, and build/test commands.

## Cursor Cloud specific instructions

### System Dependencies

- **.NET SDK 10.0.100** (prerelease) - installed via `dotnet-install.sh`
- **PowerShell 7 (pwsh)** - required for all build scripts (`build.ps1`, `PrivateBuild.ps1`, `BuildFunctions.ps1`)
- **Docker** - required for SQL Server container on Linux; needs `fuse-overlayfs` storage driver and `iptables-legacy` in the cloud VM

### Running the Full Build

```bash
pwsh -NoProfile -ExecutionPolicy Bypass -File ./PrivateBuild.ps1
```

This runs clean, restore, compile, unit tests, starts the AppHost-managed development environment, and then runs integration tests.

### Running the Application

Use AppHost as the local entry point:

```bash
cd src/AppHost && dotnet run
```

Key gotchas:
- AppHost owns SQL Server container startup, database migrations, `UI.Server`, and `Worker`.
- AppHost uses `src/AppHost/appsettings.Development.json` for the development SQL password and Application Insights connection string.
- `DISABLE_NGROK_TUNNEL=true` can be used for build and test scenarios that should not start the ngrok sidecar.

### Docker Daemon

In the cloud VM, Docker needs to be started manually:

```bash
sudo dockerd &>/tmp/dockerd.log &
sleep 5
sudo chmod 666 /var/run/docker.sock
```

### Database

The AppHost-managed SQL Server container listens on port `1433`. The container name is `aisoftwarefactory-mssql` and the password is `aisoftwarefactory-mssql#1A`. The AppHost also runs the database migration project before `UI.Server` and `Worker` start.

### Optional Services

- **Ollama** (localhost:11434): Local LLM for AI agent features. Not required; errors in logs about Ollama connection refused are expected and harmless.
- **Azure OpenAI**: Cloud LLM alternative. Requires `AI_OpenAI_ApiKey`, `AI_OpenAI_Url`, `AI_OpenAI_Model` env vars.

### Gotchas

- NServiceBus runs in trial mode (no license). This produces a warning at startup but does not block functionality.
- The HTTPS dev certificate is untrusted. Browser interactions require clicking through the security warning.
- Local development and test setup should go through AppHost rather than starting `UI.Server`, `Worker`, or SQL Server directly.
