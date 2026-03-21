param (
    [Parameter(Mandatory=$false)]
    [string]$databaseServer = "",
    
    [Parameter(Mandatory=$false)]
    [string]$databaseName = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$Headful,
    
    [Parameter(Mandatory=$false)]
    [switch]$StartAppHost
)

if ($StartAppHost) {
    # Start AppHost in background and store the process
    $appHostProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "src/AppHost/AppHost.csproj", "--no-launch-profile" -WindowStyle Hidden -PassThru
    Write-Host "Started AppHost with PID: $($appHostProcess.Id)"
    
    # Wait a moment for AppHost to initialize
    Start-Sleep -Seconds 10
}

. .\build.ps1

if ($Headful) {
    $env:HeadlessTestBrowser = "false"
    Log-Message -Message "Running acceptance tests with headful browser windows." -Type "INFO"
}

# Pass through only what the user explicitly provided; build.ps1 owns
# DATABASE_ENGINE detection and database-server defaulting.
$buildArgs = @{}
if (-not [string]::IsNullOrEmpty($databaseServer)) {
    $buildArgs["databaseServer"] = $databaseServer
}
if (-not [string]::IsNullOrEmpty($databaseName)) {
    $buildArgs["databaseName"] = $databaseName
}
Invoke-AcceptanceTests @buildArgs

# Cleanup: Stop AppHost if we started it
if ($StartAppHost) {
    Write-Host "Stopping AppHost (PID: $($appHostProcess.Id))"
    Stop-Process -Id $appHostProcess.Id -Force
}
