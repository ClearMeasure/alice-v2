#!/bin/bash
set -euo pipefail

echo '{"async": true, "asyncTimeout": 300000}'

# Only run in remote Claude Code sessions
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

# ── .NET 10.0.100 SDK ────────────────────────────────────────────────────────
DOTNET_DIR="$HOME/.dotnet"
if [ ! -f "$DOTNET_DIR/dotnet" ]; then
  echo "Installing .NET 10.0.100 SDK..."
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --version 10.0.100
else
  echo ".NET SDK already installed: $($DOTNET_DIR/dotnet --version)"
fi

# Persist dotnet on PATH for the session
echo "export PATH=\"\$PATH:$DOTNET_DIR\"" >> "$CLAUDE_ENV_FILE"
export PATH="$PATH:$DOTNET_DIR"

# Trust HTTPS dev certificate (needed for UI.Server to bind https://localhost:7174)
dotnet dev-certs https --trust 2>/dev/null || true

# ── PowerShell 7 ─────────────────────────────────────────────────────────────
if ! command -v pwsh &>/dev/null; then
  echo "Installing PowerShell 7..."
  PWSH_DEB="/tmp/powershell.deb"
  wget -q https://github.com/PowerShell/PowerShell/releases/download/v7.4.6/powershell_7.4.6-1.deb_amd64.deb \
    -O "$PWSH_DEB"
  dpkg -i "$PWSH_DEB"
  rm -f "$PWSH_DEB"
else
  echo "PowerShell already installed: $(pwsh --version)"
fi

# ── Docker daemon ─────────────────────────────────────────────────────────────
echo "Starting Docker daemon..."
dockerd &>/tmp/dockerd.log &
# Wait up to 15 s for the socket
for i in $(seq 1 15); do
  sleep 1
  if docker info &>/dev/null 2>&1; then
    echo "Docker daemon ready."
    break
  fi
done

# ── NuGet restore (warms package cache) ──────────────────────────────────────
echo "Restoring NuGet packages..."
dotnet restore src/AISoftwareFactory.slnx --verbosity quiet

echo "Session setup complete."
