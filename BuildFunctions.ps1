# Taken from psake https://github.com/psake

Function Initialize-SqlServerModule {
    <#
    .SYNOPSIS
        Installs and imports the SqlServer PowerShell module if not already available.
    .DESCRIPTION
        Ensures the SqlServer module is installed from PSGallery and imported.
        Called explicitly during Init rather than at dot-source time.
    #>
    if (-not (Get-Module -ListAvailable -Name SqlServer)) {
        Write-Host "Installing SqlServer module..." -ForegroundColor DarkCyan
        try {
            if (-not (Get-PSRepository -Name PSGallery -ErrorAction SilentlyContinue)) {
                Register-PSRepository -Default -ErrorAction Stop | Out-Null
            }
            Set-PSRepository -Name PSGallery -InstallationPolicy Trusted -ErrorAction SilentlyContinue
            Install-Module -Name SqlServer -Force -AllowClobber -Scope CurrentUser -Repository PSGallery -ErrorAction Stop
            Write-Host "SqlServer module installed successfully" -ForegroundColor Green
        }
        catch {
            Write-Host "Failed to install SqlServer module: $_" -ForegroundColor Red
            Write-Host "Some database operations may not work without this module" -ForegroundColor Yellow
        }
    }

    try {
        Import-Module SqlServer -ErrorAction Stop
    }
    catch {
        Write-Host "Warning: Could not import SqlServer module. Invoke-Sqlcmd will not be available." -ForegroundColor Yellow
    }
}

<#
.SYNOPSIS
  This is a helper function that runs a scriptblock and checks the PS variable $lastexitcode
  to see if an error occcured. If an error is detected then an exception is thrown.
  This function allows you to run command-line programs without having to
  explicitly check the $lastexitcode variable.
.EXAMPLE
  exec { svn info $repository_trunk } "Error executing SVN. Please verify SVN command-line client is installed"
#>
function Exec {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0, Mandatory = 1)][scriptblock]$cmd,
        [Parameter(Position = 1, Mandatory = 0)][string]$errorMessage = ($msgs.error_bad_command -f $cmd)
    )
    & $cmd
    if ($lastexitcode -ne 0) {
        throw ("Exec: " + $errorMessage)
    }
}

Function Poke-Xml($filePath, $xpath, $value) {
    [xml] $fileXml = Get-Content $filePath
    $node = $fileXml.SelectSingleNode($xpath)
    
    if ($node.NodeType -eq "Element") {
        $node.InnerText = $value
    }
    else {
        $node.Value = $value
    }

    $fileXml.Save($filePath) 
} 

Function Log-Message {
    param (
        [string]$Message,
        [string]$Type = "INFO"
    )

    # Suppress DEBUG messages unless BUILD_LOG_LEVEL is set to DEBUG
    if ($Type -eq "DEBUG" -and $env:BUILD_LOG_LEVEL -ne "DEBUG") {
        return
    }

    $color = switch ($Type) {
        "ERROR" { "Red" }
        "WARNING" { "Yellow" }
        "INFO" { "Cyan" }
        "DEBUG" { "DarkGray" }
        default { "White" }
    }

    $logEntry = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] [$Type] $Message"
    Write-Host $logEntry -ForegroundColor $color
}

Function Get-RedactedConnectionString {
    <#
    .SYNOPSIS
        Returns a connection string with the password redacted
    .DESCRIPTION
        Takes a connection string and replaces the password value with ***
    .PARAMETER ConnectionString
        The connection string to redact
    .OUTPUTS
        [string] The connection string with password replaced by ***
    .EXAMPLE
        Get-RedactedConnectionString -ConnectionString "Server=localhost;Database=mydb;Password=secret123;User=sa"
        Returns: "Server=localhost;Database=mydb;Password=***;User=sa"
    #>
    param (
        [Parameter(Mandatory = $true)]
        [string]$ConnectionString
    )
    
    return $ConnectionString -replace "Password=[^;]*", "Password=***"
}






Function Get-OSPlatform {
    # In PowerShell Core 6+, use built-in variables
    if ($null -ne $IsWindows) {
        if ($IsWindows) { return "Windows" }
        if ($IsLinux) { return "Linux" }
        if ($IsMacOS) { return "macOS" }
    }
    
    # Fallback for Windows PowerShell 5.1 (which only runs on Windows)
    if ($PSVersionTable.PSVersion.Major -lt 6) {
        return "Windows"
    }
    
    # Additional fallback using environment
    if ([System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Unix) {
        return "Linux"
    }
    
    return "Windows"
}

Function Test-IsLinux {
    <#
    .SYNOPSIS
        Tests if the current script is running on Linux
    .DESCRIPTION
        Returns true if the current PowerShell session is running on a Linux operating system
    .OUTPUTS
        [bool] True if running on Linux, False otherwise
    #>
    # PowerShell Core 6+ has $IsLinux variable
    if ($null -ne $IsLinux) { 
        return $IsLinux
    }
    
    # Windows PowerShell 5.1 only runs on Windows
    if ($PSVersionTable.PSVersion.Major -lt 6) {
        return $false
    }
    
    # Fallback check
    return (Get-OSPlatform -eq "Linux")
}

Function Test-IsWindows {
    <#
    .SYNOPSIS
        Tests if the current script is running on Windows
    .DESCRIPTION
        Returns true if the current PowerShell session is running on a Windows operating system
    .OUTPUTS
        [bool] True if running on Windows, False otherwise
    #>
    if ($IsWindows) { 
        return $true
    }
    
    if (Get-OSPlatform -match "Windows") {
        return $true
    }

    return $false
}

Function Test-IsGitHubActions {
    <#
    .SYNOPSIS
        Tests if the current script is running in GitHub Actions
    .DESCRIPTION
        Returns true if the current PowerShell session is running within a GitHub Actions workflow
    .OUTPUTS
        [bool] True if running in GitHub Actions, False otherwise
    .EXAMPLE
        if (Test-IsGitHubActions) {
            Write-Host "Running in GitHub Actions"
        }
    #>
    # GitHub Actions sets the GITHUB_ACTIONS environment variable to 'true'
    $githubActions = $env:GITHUB_ACTIONS
    
    if ($githubActions -eq 'true') {
        return $true
    }
    
    # Additional check for GITHUB_WORKFLOW which is also set by GitHub Actions
    if (-not [string]::IsNullOrEmpty($env:GITHUB_WORKFLOW)) {
        return $true
    }
    
    return $false
}

Function Test-IsLocalBuild {
    <#
    .SYNOPSIS
        Tests if the current script is running locally (not in GitHub Actions)
    .DESCRIPTION
        Returns true if the GITHUB_ACTIONS environment variable is not set.
        Only detects GitHub Actions as the CI environment; other CI systems are not checked.
    .OUTPUTS
        [bool] True if not running in GitHub Actions, False otherwise
    #>

    return -not (Test-IsGitHubActions)
}

Function Get-AppHostProjectPath {
    return Join-Path (Resolve-Path .\) "src\AppHost"
}

Function Get-DatabaseAssemblyPath {
    param (
        [Parameter(Mandatory = $false)]
        [string]$Configuration = "Release",

        [Parameter(Mandatory = $false)]
        [string]$Framework = "net10.0"
    )

    return Join-PathSegments (Resolve-Path .\) "src" "Database" "bin" $Configuration $Framework "ClearMeasure.Bootcamp.Database.dll"
}

Function Get-AppHostSqlConnectionString {
    $sqlPort = Get-AppHostSqlPort
    return "server=127.0.0.1,$sqlPort;database=AISoftwareFactory;User ID=sa;Password=aisoftwarefactory-mssql#1A;TrustServerCertificate=true;"
}

Function Get-AppHostSqlPort {
    $portOutput = docker port aisoftwarefactory-mssql 1433/tcp 2>$null
    if ($LASTEXITCODE -eq 0 -and $portOutput) {
        foreach ($line in $portOutput) {
            if ($line -match ':(\d+)\s*$') {
                return $matches[1]
            }
        }
    }

    return "1433"
}

Function Test-AppHostHealthy {
    param (
        [Parameter(Mandatory = $false)]
        [string]$HealthUrl = "https://localhost:7174/_healthcheck"
    )

    try {
        $response = Invoke-WebRequest -Uri $HealthUrl -SkipCertificateCheck -UseBasicParsing -TimeoutSec 5
        return $response.Content -match "Healthy|Degraded"
    }
    catch {
        return $false
    }
}

Function Start-AppHostEnvironment {
    param (
        [Parameter(Mandatory = $false)]
        [string]$DatabaseAction = "update"
    )

    if (Test-AppHostHealthy) {
        Log-Message -Message "AppHost already healthy. Reusing running environment." -Type "INFO"
        return $null
    }

    $appHostProjectPath = Get-AppHostProjectPath
    $logDirectory = Join-Path (Resolve-Path .\) "build"
    if (-not (Test-Path $logDirectory)) {
        New-Item -Path $logDirectory -ItemType Directory -Force | Out-Null
    }

    $stdoutLog = Join-Path $logDirectory "apphost.stdout.log"
    $stderrLog = Join-Path $logDirectory "apphost.stderr.log"

    $previousDotnetEnvironment = $env:DOTNET_ENVIRONMENT
    $previousAspNetCoreEnvironment = $env:ASPNETCORE_ENVIRONMENT
    $previousDisableNgrokTunnel = $env:DISABLE_NGROK_TUNNEL
    $previousNgrokAuthToken = $env:NGROK_AUTHTOKEN
    $previousDatabaseAction = $env:DatabaseAction

    $env:DOTNET_ENVIRONMENT = "Development"
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:DISABLE_NGROK_TUNNEL = "true"
    $env:NGROK_AUTHTOKEN = ""
    $env:DatabaseAction = $DatabaseAction

    try {
        $startProcessArgs = @{
            FilePath              = "dotnet"
            ArgumentList          = "run --no-build --configuration Release"
            WorkingDirectory      = $appHostProjectPath
            RedirectStandardOutput = $stdoutLog
            RedirectStandardError  = $stderrLog
            PassThru              = $true
        }
        if ($IsWindows) {
            $startProcessArgs['WindowStyle'] = 'Hidden'
        }
        $process = Start-Process @startProcessArgs
    }
    finally {
        $env:DOTNET_ENVIRONMENT = $previousDotnetEnvironment
        $env:ASPNETCORE_ENVIRONMENT = $previousAspNetCoreEnvironment
        $env:DISABLE_NGROK_TUNNEL = $previousDisableNgrokTunnel
        $env:NGROK_AUTHTOKEN = $previousNgrokAuthToken
        $env:DatabaseAction = $previousDatabaseAction
    }

    $timeout = [TimeSpan]::FromMinutes(10)
    $deadline = [DateTime]::UtcNow.Add($timeout)
    while ([DateTime]::UtcNow -lt $deadline) {
        if (Test-AppHostHealthy) {
            Log-Message -Message "AppHost environment is healthy." -Type "INFO"
            $script:appHostProcess = $process
            return $process
        }

        if ($process.HasExited) {
            throw "AppHost exited before becoming healthy. See build\apphost.stderr.log."
        }

        Start-Sleep -Seconds 3
    }

    try {
        if (-not $process.HasExited) {
            $process.Kill($true)
        }
    }
    catch {
    }

    throw "AppHost did not become healthy within $($timeout.TotalSeconds) seconds."
}

Function Stop-AppHostEnvironment {
    if ($script:appHostProcess) {
        try {
            if (-not $script:appHostProcess.HasExited) {
                $script:appHostProcess.Kill($true)
                $script:appHostProcess.WaitForExit(10000) | Out-Null
            }
        }
        finally {
            $script:appHostProcess.Dispose()
            $script:appHostProcess = $null
        }
    }
}


Function Test-IsDockerRunning {
    <#
    .SYNOPSIS
        Tests if Docker is installed and running
    .DESCRIPTION
        Checks if Docker is installed and the Docker daemon is accessible
    .PARAMETER LogOutput
        If true, outputs detailed logging information
    .OUTPUTS
        [bool] True if Docker is running, False otherwise
    #>
    param (
        [Parameter(Mandatory = $false)]
        [bool]$LogOutput = $false
    )
    
    $dockerPath = (Get-Command docker -ErrorAction SilentlyContinue).Source
    if (-not $dockerPath) {
        if ($LogOutput) {
            Log-Message -Message "Docker is not installed or not in PATH" -Type "ERROR"
            Log-Message -Message "Install Docker from: https://docs.docker.com/engine/install/" -Type "DEBUG"
        }
        return $false
    }
    else {
        if ($LogOutput) {
            Log-Message -Message "Docker found at: $dockerPath" -Type "DEBUG"
        }
        
        # Check if Docker daemon is running
        try {
            $dockerVersion = & docker version --format "{{.Server.Version}}" 2>$null
            if ($dockerVersion) {
                if ($LogOutput) {
                    Log-Message -Message "Docker daemon is running (version: $dockerVersion)" -Type "DEBUG"
                }
            }
            else {
                if ($LogOutput) {
                    Log-Message -Message "Docker is installed but the daemon may not be running. Try: sudo systemctl start docker" -Type "ERROR"
                }
                return $false
            }
        }
        catch {
            if ($LogOutput) {
                Log-Message -Message "Docker is installed but the daemon is not accessible. Try: sudo systemctl start docker" -Type "ERROR"
            }
            return $false   
        }
    }

    return $true
}

Function Generate-UniqueDatabaseName {
    param (
        [Parameter(Mandatory = $true)]
        [string]$baseName,
        
        [Parameter(Mandatory = $false)]
        [bool]$generateUnique = $false
    )
    
    if ($generateUnique) {
        $timestamp = Get-Date -Format "yyyyMMddHHmmss"
        $randomChars = -join ((65..90) + (97..122) | Get-Random -Count 4 | ForEach-Object { [char]$_ })
        $uniqueName = "${baseName}_${timestamp}_${randomChars}"
     
        Log-Message -Message "Generated unique database name: $uniqueName" -Type "DEBUG"
        return $uniqueName
    }
    else {
        Log-Message -Message "Using base database name: $baseName" -Type "DEBUG"
        return $baseName
    }
}

Function Get-ContainerName {
    <#
    .SYNOPSIS
        Creates a container name from a database name
    .DESCRIPTION
        Takes a database name and returns a container name in the format
    .PARAMETER DatabaseName
        The database name to create the container name from
    .OUTPUTS
        [string] A unique container name based on the database name
    .EXAMPLE
        Get-ContainerName -DatabaseName "MyTestDB"
        Returns: "mytestdb-mssql"
    #>
    param (
        [Parameter(Mandatory = $true)]
        [string]$DatabaseName
    )
    
    return "$DatabaseName-mssql".ToLower()
}

Function Get-SqlServerPassword {
    <#
    .SYNOPSIS
        Generates SQL Server password for Docker containers. This is for testing/CI purposes only. 
    .DESCRIPTION
        Creates a SQL Server password based on the container name that meets complexity requirements.
        Password must be at least 8 characters with uppercase, lowercase, digit, and symbol.
    .PARAMETER ContainerName
        The name of the Docker container to generate password for
    .OUTPUTS
        [string] A password that meets SQL Server complexity requirements
    .EXAMPLE
        Get-SqlServerPassword -ContainerName "mydb-mssql"
        Returns: "mydb-mssql#1A"
    #>
    param (
        [Parameter(Mandatory = $true)]
        [string]$ContainerName
    )
    
    return "${ContainerName}#1A"
}

<#
.SYNOPSIS
    Joins multiple path segments into a single path using nested Join-Path calls.
.DESCRIPTION
    Creates a cross-platform and cross-version compatible path by joining multiple segments.
    Works with PowerShell 5.1+ and pwsh 6.0+. Handles proper path separators for Windows and Linux.
.PARAMETER PathSegments
    Array of path segments to join together.
.EXAMPLE
    Join-PathSegments "C:\test", "Database", "bin", "Release", "net9.0", "test.dll"
    Returns: C:\test\Database\bin\Release\net9.0\test.dll (on Windows)
.EXAMPLE
    Join-PathSegments "/home", "user", "projects", "src", "file.txt"
    Returns: /home/user/projects/src/file.txt (on Linux)
#>
function Join-PathSegments {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, ValueFromRemainingArguments = $true)]
        [string[]]$PathSegments
    )
    
    if ($PathSegments.Count -eq 0) {
        throw "At least one path segment must be provided"
    }
    
    if ($PathSegments.Count -eq 1) {
        return $PathSegments[0]
    }
    
    $result = $PathSegments[0]
    for ($i = 1; $i -lt $PathSegments.Count; $i++) {
        $result = Join-Path $result $PathSegments[$i]
    }
    
    return $result
}

Function Get-ResolvedDatabaseEngine {
    <#
    .SYNOPSIS
        Determines the database engine to use for the standardized AppHost flow.
    .PARAMETER currentEngine
        The currently configured engine value (from DATABASE_ENGINE env var), or empty string.
    .OUTPUTS
        [string] "AppHost"
    #>
    param (
        [Parameter(Mandatory = $false)]
        [string]$currentEngine = ""
    )

    if ([string]::IsNullOrEmpty($currentEngine)) {
        return "AppHost"
    }

    $validEngines = @("AppHost")
    if ($currentEngine -notin $validEngines) {
        throw "Invalid DATABASE_ENGINE value '$currentEngine'. Valid values: $($validEngines -join ', ')"
    }
    return $currentEngine
}

Function Get-DefaultDatabaseServer {
    <#
    .SYNOPSIS
        Returns the default database server name for a given engine type.
    .PARAMETER engine
        The database engine: "AppHost".
    .OUTPUTS
        [string] The default server name.
    #>
    param (
        [Parameter(Mandatory = $true)]
        [string]$engine
    )

    return "127.0.0.1,1433"
}

Function Get-ResolvedDatabaseName {
    <#
    .SYNOPSIS
        Determines the database name to use based on explicit input or environment.
    .PARAMETER explicitName
        An explicitly provided database name. If non-empty, returned as-is.
    .PARAMETER baseName
        The base project name used to generate a database name.
    .PARAMETER onLinux
        Whether the current platform is Linux.
    .PARAMETER localBuild
        Whether this is a local (non-CI) build.
    .OUTPUTS
        [string] The resolved database name.
    #>
    param (
        [Parameter(Mandatory = $false)]
        [string]$explicitName = "",
        [Parameter(Mandatory = $true)]
        [string]$baseName,
        [Parameter(Mandatory = $false)]
        [bool]$onLinux = $false,
        [Parameter(Mandatory = $false)]
        [bool]$localBuild = $false
    )

    if (-not [string]::IsNullOrEmpty($explicitName)) {
        return $explicitName
    }

    return $baseName
}

Function New-SqlServerConnectionString {
    <#
    .SYNOPSIS
        Builds a SQL Server connection string with SQL authentication.
    .PARAMETER server
        The database server address.
    .PARAMETER database
        The database name.
    .PARAMETER password
        The SA password.
    .OUTPUTS
        [string] A SQL Server connection string.
    #>
    param (
        [Parameter(Mandatory = $true)]
        [string]$server,
        [Parameter(Mandatory = $true)]
        [string]$database,
        [Parameter(Mandatory = $true)]
        [string]$password
    )

    return "server=$server;database=$database;User ID=sa;Password=$password;TrustServerCertificate=true;"
}

Function New-IntegratedConnectionString {
    <#
    .SYNOPSIS
        Builds a SQL Server connection string with Windows Integrated Security.
    .PARAMETER server
        The database server address.
    .PARAMETER database
        The database name.
    .OUTPUTS
        [string] A connection string using Integrated Security.
    #>
    param (
        [Parameter(Mandatory = $true)]
        [string]$server,
        [Parameter(Mandatory = $true)]
        [string]$database
    )

    return "server=$server;database=$database;Integrated Security=true;"
}
