param (
    [Parameter(Mandatory=$false)]
    [string]$databaseServer = "",
    
    [Parameter(Mandatory=$false)]
    [string]$databaseName = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$RunAppHost
)

if ($RunAppHost) {
    # Run the AppHost and wait for it to be stopped
    & dotnet run --project src/AppHost/AppHost.csproj --no-launch-profile
    return
}

. .\build.ps1

# Pass through only what the user explicitly provided; build.ps1 owns
# DATABASE_ENGINE detection and database-server defaulting.
$buildArgs = @{}
if (-not [string]::IsNullOrEmpty($databaseServer)) {
    $buildArgs["databaseServer"] = $databaseServer
}
if (-not [string]::IsNullOrEmpty($databaseName)) {
    $buildArgs["databaseName"] = $databaseName
}
Build @buildArgs