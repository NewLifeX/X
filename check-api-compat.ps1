# 二进制兼容门禁本地检查（Microsoft Package Validation）
# 以「同一 dev 线（12.0.*）最新已发布版本」为基线，对当前源码打出的包做 API 兼容校验；
# 与 CI（publish-beta.yml 观察期步骤）使用同一机制。跨 TFM 存量差异走抑制白名单：
# NewLife.Core\CompatibilitySuppressions.xml（治理规则见 Doc\架构设计.md §7 收口记录）。
#
# 用法：
#   pwsh -File check-api-compat.ps1                        # 基线自动取最新 12.0.* 已发布版本
#   pwsh -File check-api-compat.ps1 -BaselineVersion 12.0.2026.1016-beta0911
#
# 结果：输出包到 .tmp\api-compat（已 gitignore），退出码即校验结果（0=通过）。
param([String]$BaselineVersion)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
Set-Location $root

if (-not $BaselineVersion) {
    $v = (Invoke-RestMethod "https://api.nuget.org/v3-flatcontainer/newlife.core/index.json").versions
    $BaselineVersion = $v | Where-Object { $_ -like "12.0.*" } | Select-Object -Last 1
    if (-not $BaselineVersion) { throw "未能从 NuGet 获取 12.0.* 基线版本" }
}
Write-Host "Package validation baseline = $BaselineVersion"

# 关键：清理 ApiCompat 增量 semaphore，否则更换基线版本会被静默跳过（试点实测陷阱）
Get-ChildItem NewLife.Core\obj -Recurse -Filter "Microsoft.NET.ApiCompat.ValidatePackage.semaphore" -ErrorAction SilentlyContinue | Remove-Item -Force

dotnet pack NewLife.Core\NewLife.Core.csproj -c Release -o .tmp\api-compat `
  -p:EnablePackageValidation=true `
  -p:PackageValidationBaselineVersion=$BaselineVersion
exit $LASTEXITCODE
