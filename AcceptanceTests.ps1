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
    try {
        Write-Host "Stopping AppHost (PID: $($appHostProcess.Id))"
        Stop-Process -Id $appHostProcess.Id -Force -ErrorAction Stop
        # Wait a bit for process to terminate
        Start-Sleep -Seconds 2
    } catch {
        Write-Host "Error stopping AppHost process: $($_.Exception.Message)"
        # Try to kill process tree if Stop-Process fails
        try {
            Get-WmiObject -Query "SELECT * FROM Win32_Process WHERE ParentProcessId=$($appHostProcess.Id)" | ForEach-Object {
                Stop-Process -Id $_.ProcessId -Force
            }
            Stop-Process -Id $appHostProcess.Id -Force
        } catch {
            Write-Host "Failed to kill AppHost process tree: $($_.Exception.Message)"
        }
    }
}
