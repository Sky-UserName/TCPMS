<#
.SYNOPSIS
    Build the uni-app mini program (mp-weixin) with the HBuilderX bundled compiler.

.DESCRIPTION
    HBuilderX projects do not keep package.json / node_modules / vite.config.js in the
    project folder, so the bundled uni-app CLI cannot run directly. This script creates
    them temporarily, runs the compile, then restores the folder to its original state.

    Output goes to src/TCPMS.User/unpackage/dist/dev/mp-weixin (dev) or
    src/TCPMS.User/unpackage/dist/build/mp-weixin (prod). Import that folder in WeChat
    DevTools.

.PARAMETER Mode
    dev  - development build, watch mode, allows mock pay and dev login (default)
    prod - production build, minified, reads .env.production

.PARAMETER HBuilderXHome
    HBuilderX installation folder. Auto-detected when omitted.

.PARAMETER KeepTemp
    Keep the temporary package.json / vite.config.js / node_modules after the build.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/build-miniapp.ps1
#>
[CmdletBinding()]
param(
    [ValidateSet('dev', 'prod')]
    [string]$Mode = 'dev',
    [string]$HBuilderXHome,
    [switch]$KeepTemp
)

$ErrorActionPreference = 'Stop'

function Write-Step($message) { Write-Host "[build-miniapp] $message" -ForegroundColor Cyan }
function Write-Warn($message) { Write-Host "[build-miniapp] $message" -ForegroundColor Yellow }

# ---------------------------------------------------------------- locate HBuilderX
if (-not $HBuilderXHome) {
    $candidates = @(
        $env:HBUILDERX_HOME,
        'D:\SoftWare\HBuilderX',
        'C:\Program Files\HBuilderX',
        'C:\Program Files (x86)\HBuilderX',
        (Join-Path $env:LOCALAPPDATA 'HBuilderX')
    ) | Where-Object { $_ -and (Test-Path $_) }

    foreach ($candidate in $candidates) {
        if (Test-Path (Join-Path $candidate 'plugins\uniapp-cli-vite')) {
            $HBuilderXHome = $candidate
            break
        }
    }
}

if (-not $HBuilderXHome -or -not (Test-Path $HBuilderXHome)) {
    throw 'HBuilderX not found. Pass -HBuilderXHome <folder>.'
}

$nodeExe  = Join-Path $HBuilderXHome 'plugins\node\node.exe'
$cliVite  = Join-Path $HBuilderXHome 'plugins\uniapp-cli-vite'
$uniJs    = Join-Path $cliVite 'node_modules\@dcloudio\vite-plugin-uni\bin\uni.js'
$dartSass = Join-Path $HBuilderXHome 'plugins\compile-dart-sass\node_modules\sass'
$sassLink = Join-Path $cliVite 'node_modules\sass'

foreach ($required in @($nodeExe, $uniJs)) {
    if (-not (Test-Path $required)) { throw "Missing compiler file: $required" }
}

Write-Step "HBuilderX: $HBuilderXHome"

# ---------------------------------------------------------------- locate project
$projectRoot = Join-Path (Split-Path -Parent $PSScriptRoot) 'src\TCPMS.User'
if (-not (Test-Path (Join-Path $projectRoot 'manifest.json'))) {
    throw "Not a uni-app project folder: $projectRoot"
}

$platform    = 'mp-weixin'
$configName  = if ($Mode -eq 'prod') { 'build' } else { 'dev' }
$outputDir   = Join-Path $projectRoot "unpackage\dist\$configName\$platform"

Write-Step "Project: $projectRoot"
Write-Step "Mode: $Mode  ->  $outputDir"

# ---------------------------------------------------------------- temp scaffolding
$packageJson = Join-Path $projectRoot 'package.json'
$viteConfig  = Join-Path $projectRoot 'vite.config.js'
$nodeModules = Join-Path $projectRoot 'node_modules'
$stamp       = Get-Date -Format 'yyyyMMddHHmmss'
$backups     = @()
$createdSass = $false

function Backup-Path($path) {
    if (Test-Path $path) {
        $backup = "$path.build-miniapp-$stamp"
        Move-Item -LiteralPath $path -Destination $backup -Force
        $script:backups += [pscustomobject]@{ Original = $path; Backup = $backup }
    }
}

# Remove-Item on a junction can trip recycle-bin based delete guards, so handle
# reparse points through the .NET API: it removes the link only, never the target.
function Remove-PathSafely($path) {
    if (-not (Test-Path -LiteralPath $path)) { return }
    $item = Get-Item -LiteralPath $path -Force
    $isLink = [bool]($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint)
    if ($isLink) {
        if ($item.PSIsContainer) { [System.IO.Directory]::Delete($path) }
        else { [System.IO.File]::Delete($path) }
        return
    }
    if ($item.PSIsContainer) { [System.IO.Directory]::Delete($path, $true) }
    else { [System.IO.File]::Delete($path) }
}

function Restore-Path($path) {
    Remove-PathSafely $path
    $entry = $script:backups | Where-Object { $_.Original -eq $path } | Select-Object -First 1
    if ($entry) {
        Move-Item -LiteralPath $entry.Backup -Destination $entry.Original -Force
    }
}

# PowerShell 5.1 writes a BOM with -Encoding UTF8; Node refuses JSON files that start with one.
function Write-TextFile($path, $content) {
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, $content, $utf8NoBom)
}

try {
    # package.json: copy the compiler dependency list so platform plugins resolve
    Backup-Path $packageJson
    $compilerPkg = Get-Content (Join-Path $cliVite 'package.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $deps = [ordered]@{}
    foreach ($prop in $compilerPkg.dependencies.PSObject.Properties) { $deps[$prop.Name] = $prop.Value }
    foreach ($extra in @('vite', 'vue', '@vitejs/plugin-vue', '@vitejs/plugin-vue-jsx', 'sass')) {
        if (-not $deps.Contains($extra)) { $deps[$extra] = '*' }
    }
    $projectPkg = [ordered]@{
        name        = 'tcpms-user'
        version     = '1.0.0'
        private     = $true
        description = 'Temporary manifest for CLI compilation, generated by build-miniapp.ps1'
        dependencies = $deps
    }
    $projectPkg | ConvertTo-Json -Depth 5 | ForEach-Object { Write-TextFile $packageJson $_ }

    # vite.config.js
    Backup-Path $viteConfig
    Write-TextFile $viteConfig @'
import { defineConfig } from 'vite'
import uni from '@dcloudio/vite-plugin-uni'

export default defineConfig({
  plugins: [uni()]
})
'@

    # node_modules junction pointing at the compiler dependencies
    Backup-Path $nodeModules
    New-Item -ItemType Junction -Path $nodeModules -Target (Join-Path $cliVite 'node_modules') | Out-Null

    # sass is shipped in a sibling plugin folder, link it so the CSS preprocessor resolves
    if (-not (Test-Path $sassLink) -and (Test-Path $dartSass)) {
        New-Item -ItemType Junction -Path $sassLink -Target $dartSass | Out-Null
        $createdSass = $true
    }

    # ------------------------------------------------------------ run compiler
    # Remove the previous manifest so watch mode cannot mistake a stale file for a fresh build.
    $staleAppJson = Join-Path $outputDir 'app.json'
    if (Test-Path $staleAppJson) { Remove-Item -LiteralPath $staleAppJson -Force }

    $outFile = Join-Path $env:TEMP "tcpms-build-$stamp.log"
    $errFile = Join-Path $env:TEMP "tcpms-build-$stamp.err.log"

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $nodeExe
    $psi.Arguments = "`"$uniJs`" build -p $platform --mode development" + $(if ($Mode -eq 'dev') { ' --watch' } else { '' })
    $psi.WorkingDirectory = $projectRoot
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true
    # Windows PowerShell 5.1 直接读写 ProcessStartInfo.EnvironmentVariables 会抛
    # “无法对 Null 数组进行索引”，改为设置当前进程环境变量，子进程启动后立即恢复。
    $envKeys = @('UNI_PLATFORM', 'UNI_INPUT_DIR', 'UNI_OUTPUT_DIR', 'NODE_ENV')
    $envBackup = @{}
    foreach ($key in $envKeys) {
        $envBackup[$key] = [System.Environment]::GetEnvironmentVariable($key, 'Process')
    }
    [System.Environment]::SetEnvironmentVariable('UNI_PLATFORM', $platform, 'Process')
    [System.Environment]::SetEnvironmentVariable('UNI_INPUT_DIR', $projectRoot, 'Process')
    [System.Environment]::SetEnvironmentVariable('UNI_OUTPUT_DIR', $outputDir, 'Process')
    if ($Mode -eq 'prod') { [System.Environment]::SetEnvironmentVariable('NODE_ENV', 'production', 'Process') }

    Write-Step 'Compiling...'
    $process = [System.Diagnostics.Process]::Start($psi)
    foreach ($key in $envKeys) {
        [System.Environment]::SetEnvironmentVariable($key, $envBackup[$key], 'Process')
    }

    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()

    if ($Mode -eq 'prod') {
        if (-not $process.WaitForExit(300000)) {
            $process.Kill()
            throw 'Compile timed out after 5 minutes.'
        }
        $out = $stdout.Result
        $err = $stderr.Result
        $out | Set-Content -LiteralPath $outFile -Encoding UTF8
        $err | Set-Content -LiteralPath $errFile -Encoding UTF8
        if ($process.ExitCode -ne 0) {
            Write-Host $out
            Write-Host $err
            throw "Compile failed with exit code $($process.ExitCode)."
        }
    }
    else {
        # watch mode never exits: wait for the "Build complete" marker, then stop it
        $deadline = (Get-Date).AddMinutes(5)
        $done = $false
        while ((Get-Date) -lt $deadline) {
            Start-Sleep -Milliseconds 700
            if ($stdout.IsCompleted -or $process.HasExited) { break }
            if (Test-Path (Join-Path $outputDir 'app.json')) {
                # app.json only appears after a successful mp-weixin emit
                Start-Sleep -Seconds 2
                $done = $true
                break
            }
        }
        if (-not $process.HasExited) { $process.Kill() }
        if (-not $done -and -not (Test-Path (Join-Path $outputDir 'app.json'))) {
            Write-Host $stdout.Result
            Write-Host $stderr.Result
            throw 'Compile did not produce app.json.'
        }
    }

    # ------------------------------------------------------------ verify output
    $appJson = Join-Path $outputDir 'app.json'
    if (-not (Test-Path $appJson)) { throw "app.json missing in $outputDir" }

    $app = Get-Content $appJson -Raw -Encoding UTF8 | ConvertFrom-Json
    $pageCount = $app.pages.Count
    foreach ($sub in $app.subPackages) { $pageCount += $sub.pages.Count }

    Write-Step "Done. $pageCount pages -> $outputDir"
    Write-Host ''
    Write-Host 'Next step: WeChat DevTools -> import this folder:' -ForegroundColor Green
    Write-Host "  $outputDir" -ForegroundColor Green
    Write-Host 'If pages were added or removed, close and reopen the project in DevTools:' -ForegroundColor DarkYellow
    Write-Host 'clearing cache alone keeps its stale file tree and reports ENOENT on removed .wxml/.wxss.' -ForegroundColor DarkYellow
}
finally {
    if (-not $KeepTemp) {
        Restore-Path $nodeModules
        Restore-Path $viteConfig
        Restore-Path $packageJson
        if ($createdSass -and (Test-Path $sassLink)) {
            Remove-PathSafely $sassLink
        }
    }
    else {
        Write-Warn 'KeepTemp set: package.json / vite.config.js / node_modules left in place.'
    }
}
