#!/bin/bash
set -euo pipefail

# Only run in remote Claude Code on the web sessions
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

# Install .NET 10 SDK
if ! dotnet --version 2>/dev/null | grep -q "^10\."; then
  echo "Installing .NET 10 SDK..."
  curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0 --install-dir /usr/local/dotnet
  export DOTNET_ROOT=/usr/local/dotnet
  export PATH="$DOTNET_ROOT:$PATH"
  echo "export DOTNET_ROOT=/usr/local/dotnet" >> "$CLAUDE_ENV_FILE"
  echo "export PATH=\"/usr/local/dotnet:\$PATH\"" >> "$CLAUDE_ENV_FILE"
else
  echo ".NET $(dotnet --version) already installed"
fi

# Install PowerShell 7
if ! pwsh --version 2>/dev/null; then
  echo "Installing PowerShell 7..."
  # Detect distro
  if [ -f /etc/debian_version ]; then
    apt-get update -qq
    apt-get install -y -qq wget apt-transport-https software-properties-common
    wget -q "https://github.com/PowerShell/PowerShell/releases/download/v7.4.6/powershell_7.4.6-1.deb_amd64.deb" -O /tmp/powershell.deb
    dpkg -i /tmp/powershell.deb
    rm /tmp/powershell.deb
  else
    echo "Unsupported distro for PowerShell install; skipping"
  fi
else
  echo "PowerShell $(pwsh --version) already installed"
fi

echo "Session start hook complete."
