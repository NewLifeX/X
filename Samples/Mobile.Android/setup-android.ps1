#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$SdkPath = "$env:LOCALAPPDATA\Android\Sdk",
    [string]$JdkPath = "",
    [int]$ApiLevel = 36,
    [switch]$WithEmulator,
    [switch]$StartEmulator
)

$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "Mobile.Android.csproj"

function Find-Jdk {
    if ($env:JAVA_HOME -and (Test-Path (Join-Path $env:JAVA_HOME "bin\java.exe"))) { return $env:JAVA_HOME }

    $patterns = @(
        "$env:ProgramFiles\Microsoft\jdk-21*",
        "$env:ProgramFiles\Eclipse Adoptium\jdk-21*",
        "$env:ProgramFiles\Java\jdk-21*"
    )
    foreach ($pattern in $patterns) {
        $dir = Get-ChildItem $pattern -Directory -ErrorAction SilentlyContinue | Sort-Object Name -Descending | Select-Object -First 1
        if ($dir) { return $dir.FullName }
    }

    return $null
}

if (-not $JdkPath) { $JdkPath = Find-Jdk }
if (-not $JdkPath) {
    Write-Host "Installing Microsoft OpenJDK 21 via winget..."
    winget install --id Microsoft.OpenJDK.21 --accept-source-agreements --accept-package-agreements --silent
    $JdkPath = Find-Jdk
}
if (-not $JdkPath) { throw "JDK 21 not found. Install Microsoft.OpenJDK.21 or pass -JdkPath." }

$env:JAVA_HOME = $JdkPath
$env:ANDROID_HOME = $SdkPath
$env:ANDROID_SDK_ROOT = $SdkPath
New-Item -ItemType Directory -Path $SdkPath -Force | Out-Null

Write-Host "Installing Android SDK dependencies into $SdkPath ..."
dotnet build $project -t:InstallAndroidDependencies -f net10.0-android `
    -p:AndroidSdkDirectory="$SdkPath" `
    -p:JavaSdkDirectory="$JdkPath" `
    -p:AcceptAndroidSDKLicenses=true `
    --nologo
if ($LASTEXITCODE -ne 0) { throw "InstallAndroidDependencies failed." }

$cmdlineTools = Join-Path $SdkPath "cmdline-tools\latest"
$sdkManager = Join-Path $cmdlineTools "bin\sdkmanager.bat"
$avdManager = Join-Path $cmdlineTools "bin\avdmanager.bat"

if (-not (Test-Path $sdkManager)) {
    $zip = Join-Path $env:TEMP "commandlinetools-win.zip"
    $url = "https://dl.google.com/android/repository/commandlinetools-win-13114758_latest.zip"
    Write-Host "Downloading Android command-line tools ..."
    Invoke-WebRequest -Uri $url -OutFile $zip

    $tmp = Join-Path $env:TEMP ("cmdline-tools-" + [Guid]::NewGuid().ToString("N"))
    Expand-Archive -Path $zip -DestinationPath $tmp -Force
    New-Item -ItemType Directory -Path (Split-Path $cmdlineTools -Parent) -Force | Out-Null
    Move-Item (Join-Path $tmp "cmdline-tools") $cmdlineTools -Force
    Remove-Item $zip -Force
    Remove-Item $tmp -Recurse -Force
}

Write-Host "Accepting Android SDK licenses ..."
cmd.exe /c "echo y|`"$sdkManager`" --licenses" | Out-Null

Write-Host "Installing platform-tools ..."
& $sdkManager "platform-tools"
if ($LASTEXITCODE -ne 0) { throw "sdkmanager platform-tools failed." }

if ($WithEmulator) {
    $driveName = (Split-Path $SdkPath -Qualifier).TrimEnd(':')
    $free = (Get-PSDrive $driveName).Free
    if ($free -lt 3GB) {
        throw "Android emulator/system image requires at least 3 GB free on ${driveName}:; current free space is $([Math]::Round($free / 1GB, 1)) GB."
    }

    $image = "system-images;android-$ApiLevel;google_apis;x86_64"
    Write-Host "Installing emulator and $image ..."
    & $sdkManager "emulator" $image
    if ($LASTEXITCODE -ne 0) { throw "sdkmanager emulator/system image failed." }

    Write-Host "Creating AVD Mobile ..."
    cmd.exe /c "echo no|`"$avdManager`" create avd -n Mobile -k `"$image`" --force" | Out-Null
}

[Environment]::SetEnvironmentVariable("ANDROID_HOME", $SdkPath, "User")
[Environment]::SetEnvironmentVariable("ANDROID_SDK_ROOT", $SdkPath, "User")
[Environment]::SetEnvironmentVariable("JAVA_HOME", $JdkPath, "User")

Write-Host ""
Write-Host "Android toolchain ready."
Write-Host "  JAVA_HOME=$JdkPath"
Write-Host "  ANDROID_HOME=$SdkPath"

if ($WithEmulator) {
    Write-Host "Start emulator: `"$SdkPath\emulator\emulator.exe`" -avd Mobile`"
    if ($StartEmulator) {
        Start-Process (Join-Path $SdkPath "emulator\emulator.exe") -ArgumentList "-avd", "Mobile"
    }
}

Write-Host "Connect a phone with USB debugging, then run: $PSScriptRoot\run-android.ps1"

