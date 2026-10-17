#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Device = "",
    [int]$TimeoutSec = 180,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "MobileConsole.Android\MobileConsole.Android.csproj"
$artifacts = Join-Path $PSScriptRoot "artifacts"
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null

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

    throw "adb not found. Run Samples/MobileSmoke/setup-android.ps1 first."
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
    $buildArgs = @("build", $project, "-c", $Configuration, "-f", "net10.0-android", "-p:RuntimeIdentifier=$rid", "--nologo")
    if ($env:ANDROID_HOME) { $buildArgs += "-p:AndroidSdkDirectory=$env:ANDROID_HOME" }
    if ($env:JAVA_HOME) { $buildArgs += "-p:JavaSdkDirectory=$env:JAVA_HOME" }

    & dotnet @buildArgs
    if ($LASTEXITCODE -ne 0) { throw "Android build failed." }
}

$apk = Get-ChildItem (Join-Path $PSScriptRoot "MobileConsole.Android\bin\$Configuration\net10.0-android\$rid") -Filter "*.apk" -Recurse -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $apk) {
    $apk = Get-ChildItem (Join-Path $PSScriptRoot "MobileConsole.Android\bin\$Configuration\net10.0-android") -Filter "*.apk" -Recurse -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
}
if (-not $apk) { throw "APK not found under bin/$Configuration/net10.0-android." }

Write-Host "Installing $($apk.FullName) ..."
& $adb -s $Device install -r $apk.FullName
if ($LASTEXITCODE -ne 0) { throw "adb install failed." }

& $adb -s $Device logcat -c
& $adb -s $Device shell am force-stop com.newlife.mobileconsole
& $adb -s $Device shell monkey -p com.newlife.mobileconsole -c android.intent.category.LAUNCHER 1 | Out-Null

$deadline = (Get-Date).AddSeconds($TimeoutSec)
$finished = $false
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 2
    $finished = [bool](& $adb -s $Device logcat -d -s "MobileConsole:I" | Select-String -SimpleMatch "=== MobileConsole end ===")
    if ($finished) { break }
}

$logFile = Join-Path $artifacts ("android-" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".log")
$lines = & $adb -s $Device logcat -d -s "MobileConsole:I"
$lines | Out-File -FilePath $logFile -Encoding utf8

if (-not $finished) {
    Write-Host "Last logcat lines:"
    $lines | Select-Object -Last 80
    throw "MobileConsole output not finished within $TimeoutSec seconds."
}

$lines | ForEach-Object { $_ -replace '^.*?MobileConsole\s*:\s?', '' }
Write-Host ""
Write-Host "log=$logFile"
