#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet("Aot", "Jit", "NativeAot")]
    [string]$Mode = "Aot",
    [string]$Configuration = "Release",
    [string]$OutputDir = "",
    [string]$RuntimeIdentifier = "android-arm64",
    [switch]$Install,
    [string]$Device = "",
    [switch]$Stardust
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$project = Join-Path $PSScriptRoot "Mobile.Android.csproj"
if (-not $OutputDir) { $OutputDir = Join-Path $root "Bin/Mobile.Android/apk" }
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$verb = if ($Mode -eq "NativeAot") { "publish" } else { "build" }
$buildArgs = @($verb, $project, "-c", $Configuration, "-f", "net10.0-android", "-p:RuntimeIdentifier=$RuntimeIdentifier", "--nologo")
switch ($Mode) {
    "Jit" {
        $buildArgs += "-p:RunAOTCompilation=false"
        $buildArgs += "-p:AndroidEnableProfiledAot=false"
    }
    "Aot" {
        $buildArgs += "-p:RunAOTCompilation=true"
    }
    "NativeAot" {
        # NativeAOT 需要 Android NDK 的 clang/LLVM；优先使用 ANDROID_NDK_HOME，其次 Android SDK 下的 ndk/<version>
        $ndk = $env:ANDROID_NDK_HOME
        if (-not $ndk -and $env:ANDROID_HOME) {
            $ndkRoot = Join-Path $env:ANDROID_HOME "ndk"
            if (Test-Path $ndkRoot) {
                $ndk = Get-ChildItem $ndkRoot -Directory |
                    Sort-Object { [version]($_.Name -replace '[^0-9.].*$','') } -Descending |
                    Select-Object -First 1 -ExpandProperty FullName
            }
        }
        if (-not $ndk -or -not (Test-Path $ndk)) {
            throw "NativeAot 需要 Android NDK。请先执行: sdkmanager ""ndk;26.3.11579264""，或设置 ANDROID_NDK_HOME。"
        }

        $hostTag = if ($IsWindows) { "windows-x86_64" } elseif ($IsMacOS) { "darwin-x86_64" } else { "linux-x86_64" }
        $clangBin = Join-Path $ndk "toolchains/llvm/prebuilt/$hostTag/bin"
        if (-not (Test-Path $clangBin)) { throw "Android NDK 缺少 LLVM 工具链: $clangBin" }
        $env:PATH = "$clangBin$([IO.Path]::PathSeparator)$env:PATH"
        $clang = Join-Path $clangBin $(if ($IsWindows) { "clang.exe" } else { "clang" })
        $clangxx = Join-Path $clangBin $(if ($IsWindows) { "clang++.exe" } else { "clang++" })
        if (Test-Path $clang) { $env:CC = $clang }
        if (Test-Path $clangxx) { $env:CXX = $clangxx }

        $buildArgs += "-p:NativeAot=true"
        $buildArgs += "-p:AndroidNdkDirectory=$ndk"
    }
}
if ($env:ANDROID_HOME) { $buildArgs += "-p:AndroidSdkDirectory=$env:ANDROID_HOME" }
if ($env:JAVA_HOME) { $buildArgs += "-p:JavaSdkDirectory=$env:JAVA_HOME" }
if ($Stardust) {
    $buildArgs += "-p:IncludeStardust=true"
}

Write-Host "Building $Mode APK ..."
& dotnet @buildArgs
if ($LASTEXITCODE -ne 0) { throw "$Mode build failed." }

$binDir = Join-Path $root "Bin/Mobile.Android"
# 优先使用本次构建 RID 目录下的产物，避免误取其它模式（JIT/AOT）遗留的 APK
$apkPath = Join-Path $binDir "$RuntimeIdentifier/com.newlife.mobile-Signed.apk"
if (-not (Test-Path $apkPath)) {
    $apkFile = Get-ChildItem (Join-Path $binDir $RuntimeIdentifier) -Filter "*-Signed.apk" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($apkFile) { $apkPath = $apkFile.FullName }
}
if (-not (Test-Path $apkPath)) {
    $apkFile = Get-ChildItem $binDir -Filter "*.apk" -Recurse -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($apkFile) { $apkPath = $apkFile.FullName }
}
if (-not $apkPath -or -not (Test-Path $apkPath)) { throw "APK not found under $binDir." }

$suffix = if ($Stardust) { "-Stardust" } else { "" }
$dest = Join-Path $OutputDir ("Mobile-" + $Mode + $suffix + ".apk")
Copy-Item $apkPath $dest -Force
$hash = (Get-FileHash $dest -Algorithm SHA256).Hash
$size = (Get-Item $dest).Length

Write-Host ""
Write-Host "APK=$dest"
Write-Host ("Size={0:N2} MB" -f ($size / 1MB))
Write-Host "SHA256=$hash"

if ($Install) {
    function Find-Adb {
        $sdks = @()
        if ($env:ANDROID_HOME) { $sdks += $env:ANDROID_HOME }
        if ($env:ANDROID_SDK_ROOT) { $sdks += $env:ANDROID_SDK_ROOT }
        if ($IsWindows) {
            if ($env:LOCALAPPDATA) { $sdks += (Join-Path $env:LOCALAPPDATA "Android/Sdk") }
            $sdks += "C:/Android/Sdk"
        }

        $exe = if ($IsWindows) { "adb.exe" } else { "adb" }
        foreach ($sdk in $sdks) {
            $path = Join-Path $sdk "platform-tools/$exe"
            if (Test-Path $path) { return $path }
        }

        $cmd = Get-Command adb -ErrorAction SilentlyContinue
        if ($cmd) { return $cmd.Source }
        throw "adb not found. Run Samples/Mobile.Android/setup-android.ps1 first."
    }

    $adb = Find-Adb
    if ($Device) { & $adb -s $Device install -r $dest } else { & $adb install -r $dest }
    if ($LASTEXITCODE -ne 0) { throw "adb install failed." }
    Write-Host "Installed to device."
}


