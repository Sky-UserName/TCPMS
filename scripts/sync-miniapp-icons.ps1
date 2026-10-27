# 把用户端「民宿管理」图标同步到管理端。
# 图标唯一来源：src/TCPMS.User/static/prototype-icons/ui-*.png
# 目标目录：src/admin/src/assets/prototype-icons（管理端 vite 会打包这些 PNG）
# 只同步普通色版本，-white 版本是用户端深色背景专用，管理端不需要。
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root 'src/TCPMS.User/static/prototype-icons'
$target = Join-Path $root 'src/admin/src/assets/prototype-icons'

if (-not (Test-Path $source)) { throw "未找到用户端图标目录：$source" }
New-Item -ItemType Directory -Force -Path $target | Out-Null

$copied = 0
Get-ChildItem -Path $source -Filter 'ui-*.png' | Where-Object { $_.Name -notmatch '-white\.png$' -and $_.Name -ne 'ui-support-active.png' } | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $target $_.Name) -Force
    $copied++
}

# 清理管理端多余的旧图标，保证两边完全一致。
$sourceNames = (Get-ChildItem -Path $source -Filter 'ui-*.png' | Where-Object { $_.Name -notmatch '-white\.png$' -and $_.Name -ne 'ui-support-active.png' }).Name
$removed = 0
Get-ChildItem -Path $target -Filter 'ui-*.png' | Where-Object { $sourceNames -notcontains $_.Name } | ForEach-Object {
    Remove-Item -LiteralPath $_.FullName -Force
    $removed++
}

Write-Host "[sync-miniapp-icons] copied $copied, removed $removed -> $target"
