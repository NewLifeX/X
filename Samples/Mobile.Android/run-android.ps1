#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet("Aot", "Jit", "NativeAot")]
    [string]$Mode = "Aot",
    [string]$Configuration = "Release",
    [string]$Device = "",
    [int]$TimeoutSec = 180,
    [switch]$SkipBuild,
    [switch]$Smoke
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$pack = Join-Path $PSScriptRoot "pack-android.ps1"
$apkDir = Join-Path $root "Bin\Mobile.Android\apk"

function Find-Adb {
    $sdk = $env:ANDROID_HOME
    if (-not $sdk) { $sdk = $env:ANDROID_SDK_ROOT }

    $paths = @()
    if ($sdk) { $paths += (Join-Path $sdk "platform-tools\adb.exe") }
    $paths += (Join-Path $env:LOCALAPPDATA "Android\Sdk\platform-tools\adb.exe")
    $paths += "C:\Android\Sdk\platform-tools\adb.exe"

    foreach ($path in $paths) {
        if (Test-Path $path) { return $path }
    }

    $cmd = Get-Command adb -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    throw "adb not found. Run Samples/Mobile.Android/setup-android.ps1 first."
}

$adb = Find-Adb

function Get-Devices {
    $devices = @()
    foreach ($line in (& $adb devices)) {
        if ($line -match "^\s*(\S+)\s+device\s*$") { $devices += $Matches[1] }
    }
    return @($devices)
}

$devices = @(Get-Devices)
if (-not $Device) {
    if ($devices.Count -eq 0) { throw "No Android device/emulator online. Start one or connect a phone with USB debugging." }
    $Device = $devices[0]
}
if ($devices -notcontains $Device) { throw "Device $Device is not online. Online: $($devices -join ', ')" }
Write-Host "Using device: $Device"

$abi = (& $adb -s $Device shell getprop ro.product.cpu.abi).Trim()
$rid = switch -Regex ($abi) {
    "arm64" { "android-arm64" }
    "armeabi" { "android-arm" }
    "x86_64" { "android-x64" }
    "x86" { "android-x86" }
    default { throw "Unsupported ABI: $abi" }
}
Write-Host "ABI=$abi RID=$rid"

if (-not $SkipBuild) {
    & pwsh -NoProfile -File $pack -Mode $Mode -Configuration $Configuration -RuntimeIdentifier $rid
    if ($LASTEXITCODE -ne 0) { throw "Android build failed." }
}

$apk = Join-Path $apkDir ("Mobile-" + $Mode + ".apk")
if (-not (Test-Path $apk)) {
    $apk = Get-ChildItem (Join-Path $root "Bin\Mobile.Android") -Filter "*.apk" -Recurse -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
}
if (-not $apk -or -not (Test-Path $apk)) { throw "APK not found. Run pack-android.ps1 first." }

Write-Host "Installing $apk ..."
& $adb -s $Device install -r $apk
if ($LASTEXITCODE -ne 0) { throw "adb install failed." }

& $adb -s $Device logcat -c
& $adb -s $Device shell am force-stop com.newlife.mobile

if ($Smoke) {
    $activity = (& $adb -s $Device shell cmd package resolve-activity --brief -a android.intent.action.MAIN -c android.intent.category.LAUNCHER com.newlife.mobile | Select-Object -Last 1).Trim()
    if (-not $activity) { throw "Unable to resolve com.newlife.mobile launcher activity." }
    & $adb -s $Device shell am start -n $activity --ez smoke true | Out-Null
} else {
    & $adb -s $Device shell monkey -p com.newlife.mobile -c android.intent.category.LAUNCHER 1 | Out-Null
}

$pattern = if ($Smoke) { "SUMMARY total=\d+ pass=\d+ fail=\d+ skip=\d+" } else { "=== Info end ===" }
$deadline = (Get-Date).AddSeconds($TimeoutSec)
$match = $null
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 2
    $match = & $adb -s $Device logcat -d -s "Mobile:I" | Select-String -Pattern $pattern | Select-Object -Last 1
    if ($match) { break }
}

$artifactDir = Join-Path $root "Bin\Mobile.Android\artifacts"
New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null
$logFile = Join-Path $artifactDir ("android-" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".log")
$lines = & $adb -s $Device logcat -d -s "Mobile:I"
$lines | Out-File -FilePath $logFile -Encoding utf8
$lines | ForEach-Object { $_ -replace '^.*?Mobile\s*:\s?', '' }

if (-not $match) {
    Write-Host "Last logcat lines:"
    $lines | Select-Object -Last 80
    throw "Expected Mobile output not found within $TimeoutSec seconds."
}

Write-Host ""
Write-Host "log=$logFile"
if ($Smoke) {
    Write-Host $match.Line
    if ($match.Line -notmatch "fail=0") { throw "Mobile smoke failed: $($match.Line)" }
    Write-Host "Android smoke passed."
} else {
    Write-Host "Android info passed."
}
