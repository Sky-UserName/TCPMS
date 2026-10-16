[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ApiBaseUrl,
    [switch]$SkipNpmInstall
)

$ErrorActionPreference = "Stop"

function Assert-Command {
    param([Parameter(Mandatory = $true)][string]$Name)

    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($null -eq $command) {
        throw "Command not found: $Name"
    }
    return $command.Source
}

function Invoke-Native {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed ($LASTEXITCODE): $FilePath $($Arguments -join ' ')"
    }
}

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$releaseRoot = Join-Path $root "release"
$packageName = "TCPMS-release-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
$packageDir = Join-Path $releaseRoot $packageName
$zipPath = Join-Path $releaseRoot "$packageName.zip"
$adminOutput = Join-Path $packageDir "admin"
$backendOutput = Join-Path $packageDir "backend"
$backendProject = Join-Path $root "src/backend/TCPMS.Api/TCPMS.Api.csproj"
$releaseGuide = Join-Path $root "docs/RELEASE.md"

if ([string]::IsNullOrWhiteSpace($ApiBaseUrl)) {
    throw "ApiBaseUrl cannot be empty."
}

$apiUri = $null
if (-not [Uri]::TryCreate($ApiBaseUrl, [UriKind]::Absolute, [ref]$apiUri) -or
    $apiUri.Scheme -notin @("http", "https")) {
    throw "ApiBaseUrl must be an absolute http or https URL."
}

$null = Assert-Command "npm"
$null = Assert-Command "dotnet"

New-Item -ItemType Directory -Path $releaseRoot -Force | Out-Null
if (Test-Path -LiteralPath $packageDir) {
    throw "Release directory already exists: $packageDir"
}
New-Item -ItemType Directory -Path $packageDir -Force | Out-Null

$previousApiBaseUrl = $env:VITE_API_BASE_URL
$env:VITE_API_BASE_URL = $ApiBaseUrl.TrimEnd("/")

try {
    Push-Location (Join-Path $root "src/admin")
    try {
        if (-not $SkipNpmInstall) {
            Invoke-Native "npm" @("ci", "--no-audit", "--no-fund")
        }

        Invoke-Native "npm" @(
            "run",
            "build",
            "--",
            "--outDir",
            $adminOutput,
            "--emptyOutDir"
        )
    }
    finally {
        Pop-Location
    }

    Invoke-Native "dotnet" @(
        "publish",
        $backendProject,
        "--configuration",
        "Release",
        "--output",
        $backendOutput,
        "--nologo"
    )

    if (-not (Test-Path -LiteralPath $releaseGuide)) {
        throw "Release guide not found: $releaseGuide"
    }
    Copy-Item -LiteralPath $releaseGuide -Destination (Join-Path $packageDir "RELEASE.md")

    Compress-Archive -Path (Join-Path $packageDir "*") -DestinationPath $zipPath -CompressionLevel Optimal
}
finally {
    if ($null -eq $previousApiBaseUrl) {
        Remove-Item Env:VITE_API_BASE_URL -ErrorAction SilentlyContinue
    }
    else {
        $env:VITE_API_BASE_URL = $previousApiBaseUrl
    }
}

Write-Host ""
Write-Host "Release complete."
Write-Host "Directory: $packageDir"
Write-Host "Archive: $zipPath"
Write-Host "Swagger UI: <API URL>/swagger"
Write-Host "OpenAPI JSON: <API URL>/swagger/v1/swagger.json"
