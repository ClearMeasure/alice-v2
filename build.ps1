#!/usr/bin/env pwsh
. .\BuildFunctions.ps1

# Clean environment variables that may interfere with local builds
if ($env:ConnectionStrings__SqlConnectionString -and -not (Test-IsGitHubActions)) {
	$env:ConnectionStrings__SqlConnectionString = $null
	[Environment]::SetEnvironmentVariable("ConnectionStrings__SqlConnectionString", $null, "User")
}

$projectName = "AISoftwareFactory"
$base_dir = resolve-path .\
$source_dir = Join-Path $base_dir "src"
$solutionName = Join-Path $source_dir "AISoftwareFactory.slnx"
$unitTestProjectPath = Join-Path $source_dir "UnitTests"
$integrationTestProjectPath = Join-Path $source_dir "IntegrationTests"
$acceptanceTestProjectPath = Join-Path $source_dir "AcceptanceTests"
$uiProjectPath =  Join-PathSegments $source_dir "UI" "Server"
$databaseProjectPath = Join-Path $source_dir "Database"
$projectConfig = $env:BuildConfiguration
$framework = "net10.0"
$version = $env:BUILD_BUILDNUMBER

$verbosity = "quiet"

$build_dir = Join-Path $base_dir "build"
$test_dir = Join-Path $build_dir "test"

$databaseAction = $env:DatabaseAction
if ([string]::IsNullOrEmpty($databaseAction)) { $databaseAction = "Rebuild" }

$script:databaseEngine = "AppHost"

$databaseName = $projectName

$script:databaseServer = $databaseServer;
$script:databaseScripts = Join-PathSegments $source_dir "Database" "scripts"

if ([string]::IsNullOrEmpty($version)) { $version = "1.0.0" }
if ([string]::IsNullOrEmpty($projectConfig)) { $projectConfig = "Release" }

# ── Main Functions ──────────────────────────────────────────────────────────────

Function Init {
	$pwshPath = (Get-Command pwsh -ErrorAction SilentlyContinue).Source
	if (-not $pwshPath) {
		throw "PowerShell 7 is required to run this build script."
	}

	if (Test-IsLinux) {
		if (-not (Test-IsGitHubActions)) {
			$env:NUGET_PACKAGES = "/tmp/nuget-packages"
		}
	}

	if ([string]::IsNullOrEmpty($script:databaseServer)) {
		$script:databaseServer = Get-DefaultDatabaseServer -engine $script:databaseEngine
	}

	if (Test-Path "build") {
		Remove-Item -Path "build" -Recurse -Force
	}

	New-Item -Path $build_dir -ItemType Directory -Force | Out-Null

	exec {
		& dotnet clean $solutionName -nologo -v $verbosity /p:SuppressNETCoreSdkPreviewMessage=true
	}

	exec {
		& dotnet restore $solutionName -nologo --interactive -v $verbosity /p:SuppressNETCoreSdkPreviewMessage=true
	}
}

Function Compile {
	exec {
		& dotnet build $solutionName -nologo --no-restore -v `
			$verbosity -maxcpucount --configuration $projectConfig --no-incremental `
			/p:TreatWarningsAsErrors="true" `
			/p:MSBuildTreatAllWarningsAsErrors="true" `
			/p:SuppressNETCoreSdkPreviewMessage=true `
			/p:Version=$version /p:Authors="Programming with Palermo" `
			/p:Product="AI Software Factory"
	}
}

Function UnitTests {
	Push-Location -Path $unitTestProjectPath

	try {
		exec {
			& dotnet test /p:CopyLocalLockFileAssemblies=true -nologo -v $verbosity --logger:trx `
				--results-directory $(Join-Path $test_dir "UnitTests") --no-build `
				--no-restore --configuration $projectConfig `
				--collect:"XPlat Code Coverage"
		}
	}
	finally {
		Pop-Location
	}
}

Function MigrateDatabaseLocal {
	$env:ConnectionStrings__SqlConnectionString = Get-AppHostSqlConnectionString
	Log-Message -Message "Using AppHost SQL connection string: $(Get-RedactedConnectionString -ConnectionString $env:ConnectionStrings__SqlConnectionString)" -Type "DEBUG"

	$databaseAssemblyPath = Get-DatabaseAssemblyPath -Configuration $projectConfig -Framework $framework
	if (-not (Test-Path $databaseAssemblyPath)) {
		throw "Database assembly not found at $databaseAssemblyPath"
	}

	$sqlPort = Get-AppHostSqlPort
	$databaseServer = "127.0.0.1,$sqlPort"

	Log-Message -Message "Running database $databaseAction against AppHost SQL endpoint $databaseServer" -Type "INFO"
	exec {
		& dotnet $databaseAssemblyPath $databaseAction $databaseServer $script:databaseName $script:databaseScripts "sa" "aisoftwarefactory-mssql#1A"
	} "Database migration failed."
}

Function IntegrationTest {
	Push-Location -Path $integrationTestProjectPath

	try {
		exec {
			& dotnet test /p:CopyLocalLockFileAssemblies=true -nologo -v $verbosity --logger:trx `
				--results-directory $(Join-Path $test_dir "IntegrationTests") --no-build `
				--no-restore --configuration $projectConfig `
				--collect:"XPlat Code Coverage"
		}
	}
	finally {
		Pop-Location
	}
}

Function Package-Everything{

	# Allow Octopus.DotNet.Cli (targets net6.0) to run on the current .NET SDK
	$env:DOTNET_ROLL_FORWARD = "LatestMajor"

	dotnet tool install --global Octopus.DotNet.Cli 2>$null # prevents red 'already installed' message

	# Ensure dotnet tools are in PATH
	$dotnetToolsPath = [System.IO.Path]::Combine([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::UserProfile), ".dotnet", "tools")
	$pathEntries = $env:PATH -split [System.IO.Path]::PathSeparator
	$dotnetToolsPathPresent = $pathEntries | Where-Object { $_.Trim().ToLowerInvariant() -eq $dotnetToolsPath.Trim().ToLowerInvariant() }
	if (-not $dotnetToolsPathPresent) {
		$env:PATH = "$dotnetToolsPath$([System.IO.Path]::PathSeparator)$env:PATH"
	}

	PackageUI
	PackageDatabase
	PackageAcceptanceTests
	PackageScript
}

Function Build {
	param (
		[Parameter(Mandatory = $false)]
		[string]$databaseServer = "",

		[Parameter(Mandatory = $false)]
		[string]$databaseName = ""
	)

	Resolve-DatabaseEngine

	if (-not [string]::IsNullOrEmpty($databaseServer)) {
		Log-Message -Message "Ignoring databaseServer parameter. AppHost owns database startup." -Type "WARNING"
	}
	if (-not [string]::IsNullOrEmpty($databaseName)) {
		Log-Message -Message "Ignoring databaseName parameter. AppHost uses the AISoftwareFactory database." -Type "WARNING"
	}
	$script:databaseName = Get-ResolvedDatabaseName -explicitName $databaseName -baseName $projectName -onLinux (Test-IsLinux) -localBuild (Test-IsLocalBuild)

	$script:buildStopwatch = [Diagnostics.Stopwatch]::StartNew()

	try {
		Init
		Compile
		UnitTests
		Log-Message -Message "Starting AppHost-managed infrastructure..." -Type "INFO"
		Start-AppHostEnvironment -StartupMode ContainersOnly | Out-Null
		MigrateDatabaseLocal
		IntegrationTest
	}
	finally {
		Stop-AppHostEnvironment
	}

	$script:buildStopwatch.Stop()
	$elapsed = $script:buildStopwatch.Elapsed.ToString()
	Log-Message -Message "BUILD SUCCEEDED - Build time: $elapsed" -Type "INFO"
}

Function Invoke-CIBuild {
	Resolve-DatabaseEngine

	$script:databaseName = Get-ResolvedDatabaseName -baseName $projectName -onLinux (Test-IsLinux) -localBuild $false

	$script:buildStopwatch = [Diagnostics.Stopwatch]::StartNew()

	try {
		Init
		Compile
		UnitTests
		Log-Message -Message "Starting AppHost-managed infrastructure..." -Type "INFO"
		Start-AppHostEnvironment -StartupMode ContainersOnly | Out-Null
		MigrateDatabaseLocal
		IntegrationTest
	}
	finally {
		Stop-AppHostEnvironment
	}

	$script:buildStopwatch.Stop()
	$elapsed = $script:buildStopwatch.Elapsed.ToString()
	Log-Message -Message "BUILD SUCCEEDED - Build time: $elapsed" -Type "INFO"
}

# ── Helper Functions (in call order) ────────────────────────────────────────────

Function Resolve-DatabaseEngine {
	if (-not [string]::IsNullOrEmpty($env:DATABASE_ENGINE) -and $env:DATABASE_ENGINE -ne "AppHost") {
		Log-Message -Message "Ignoring DATABASE_ENGINE=$($env:DATABASE_ENGINE). Build setup is standardized through AppHost." -Type "WARNING"
	}

	$script:databaseEngine = "AppHost"
}

Function PackageUI {
	exec {
		& dotnet publish $uiProjectPath -nologo --no-restore --no-build -v $verbosity --configuration $projectConfig
	}

	exec {
		& dotnet-octo pack --id "$projectName.UI" --version $version --basePath $(Join-PathSegments $uiProjectPath "bin" $projectConfig $framework "publish") --outFolder $build_dir  --overwrite
	}

}

Function PackageDatabase {
	exec {
		& dotnet publish $databaseProjectPath -nologo --no-restore -v $verbosity --configuration Debug
	}
	exec {
		& dotnet-octo pack --id "$projectName.Database" --version $version --basePath $databaseProjectPath --outFolder $build_dir --overwrite
	}

}

Function PackageAcceptanceTests {
	# Use Debug configuration so full symbols are available to display better error messages in test failures
	exec {
		& dotnet publish $acceptanceTestProjectPath -nologo --no-restore -v $verbosity --configuration Debug
	}

	# Copy the .playwright metadata folder into the publish output so the nupkg
	# is self-contained.  The playwright.ps1 install command needs this folder to
	# know which browser versions to download on the target machine.
	$publishDir = Join-PathSegments $acceptanceTestProjectPath "bin" "Debug" $framework "publish"
	$playwrightSource = Join-PathSegments $acceptanceTestProjectPath "bin" "Debug" $framework ".playwright"
	if (Test-Path $playwrightSource) {
		Copy-Item -Path $playwrightSource -Destination (Join-Path $publishDir ".playwright") -Recurse -Force
	}

	exec {
		& dotnet-octo pack --id "$projectName.AcceptanceTests" --version $version --basePath $publishDir --outFolder $build_dir --overwrite
	}

}

Function PackageScript {
	exec {
		& dotnet publish $uiProjectPath -nologo --no-restore --no-build -v $verbosity --configuration $projectConfig
	}
	exec {
		& dotnet-octo pack --id "$projectName.Script" --version $version --basePath $uiProjectPath --include "*.ps1" --outFolder $build_dir  --overwrite
	}

}

Function AcceptanceTests {
	$projectConfig = "Release"
	Push-Location -Path $acceptanceTestProjectPath

	$playwrightScript = Join-PathSegments "bin" "Release" $framework "playwright.ps1"

	if (Test-Path $playwrightScript) {
		& pwsh $playwrightScript install chromium --with-deps
		if ($LASTEXITCODE -ne 0) {
			throw "Failed to install Playwright chromium"
		}
	}
	else {
		throw "Playwright script not found at $playwrightScript. Cannot run acceptance tests without the browsers."
	}

	if (Test-AppHostHealthy) {
		Log-Message -Message "AppHost is already running. Reusing existing environment for acceptance tests." -Type "INFO"
	}

	$runSettingsPath = Join-Path $acceptanceTestProjectPath "AcceptanceTests.runsettings"
	try {
		exec {
		& dotnet test /p:CopyLocalLockFileAssemblies=true -nologo -v normal --logger:trx `
				--results-directory $(Join-Path $test_dir "AcceptanceTests") --no-build `
				--no-restore --configuration $projectConfig `
				--settings:$runSettingsPath `
				--collect:"XPlat Code Coverage"
		}
	}
	finally {
		Pop-Location
	}
}

Function Invoke-AcceptanceTests {
	param (
		[Parameter(Mandatory = $false)]
		[string]$databaseServer = "",
		[Parameter(Mandatory=$false)]
		[string]$databaseName =""
	)

	$projectConfig = "Release"
	$sw = [Diagnostics.Stopwatch]::StartNew()

	Resolve-DatabaseEngine

	if (-not [string]::IsNullOrEmpty($databaseServer)) {
		Log-Message -Message "Ignoring databaseServer parameter. AppHost owns database startup." -Type "WARNING"
	}
	if (-not [string]::IsNullOrEmpty($databaseName)) {
		Log-Message -Message "Ignoring databaseName parameter. AppHost uses the AISoftwareFactory database." -Type "WARNING"
	}
	$script:databaseName = Get-ResolvedDatabaseName -explicitName $databaseName -baseName $projectName -onLinux (Test-IsLinux) -localBuild (Test-IsLocalBuild)

	try {
		Init
		Compile
		Log-Message -Message "Starting AppHost-managed infrastructure..." -Type "INFO"
		Start-AppHostEnvironment -StartupMode ContainersOnly | Out-Null
		MigrateDatabaseLocal
		AcceptanceTests
	}
	finally {
		Stop-AppHostEnvironment
	}

	$sw.Stop()
	$elapsed = $sw.Elapsed.ToString()
	Log-Message -Message "ACCEPTANCE BUILD SUCCEEDED - Build time: $elapsed" -Type "INFO"
}
