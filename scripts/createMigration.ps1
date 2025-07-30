param(
    [Parameter(Mandatory = $true)]
    [string]$MigrationName
)

if ($MigrationName -notmatch '^[A-Za-z0-9_]+$') {
    Write-Error "Migration name can only contain letters, numbers, and underscores."
    exit 1
}

$StartupProject = "src\ScarletpigsServices.Api"
$Project = "src\ScarletpigsServices.Data"

$CurrentDir = Split-Path -Leaf (Get-Location)
if ($CurrentDir -eq "scripts") {
    # Prepend the project paths with ".."
    $StartupProject = "..\$StartupProject"
    $Project = "..\$Project"
}

if (-not (Test-Path -Path (Join-Path $StartupProject "*.csproj"))) {
    Write-Error "No .csproj file found in startup project folder: $StartupProject"
    exit 1
}
if (-not (Test-Path -Path (Join-Path $Project "*.csproj"))) {
    Write-Error "No .csproj file found in project folder: $Project"
    exit 1
}

dotnet ef migrations add $MigrationName --startup-project $StartupProject --project $Project