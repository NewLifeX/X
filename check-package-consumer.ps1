# NuGet 包消费级验证
#
# 验证"下游只加 PackageReference 就能用多语言"：
#   1. 打包 NewLife.Core；
#   2. 检查 nupkg 必须包含分析器、lib、README 与图标；
#   3. 创建临时项目，仅用 PackageReference 引用刚打的包；
#   4. 放入 Locales/ta.yml，运行 Translate / L.T，断言泰米尔语与英文回退。
#
# 用法：
#   pwsh -File check-package-consumer.ps1
#   pwsh -File check-package-consumer.ps1 -Keep     # 保留临时目录便于排查
param(
    [String]$Configuration = "Release",
    [String]$Framework = "net10.0",
    [String]$Version,
    [Switch]$Keep
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$work = Join-Path $root ".tmp\package-consumer"
$out = Join-Path $work "out"
$consumer = Join-Path $work "Consumer"

if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
New-Item -ItemType Directory -Force -Path $out | Out-Null

Write-Host "1/4 打包 NewLife.Core ..."
# 每次用唯一版本号，避免 NuGet 全局缓存命中上一次同版本包导致验证的是旧分析器
if (!$Version) { $Version = "12.0." + (Get-Date -Format "yyyy.MMdd") + "-beta" + (Get-Date -Format "HHmmss") }
& dotnet pack (Join-Path $root "NewLife.Core\NewLife.Core.csproj") -c $Configuration -o $out -p:Version=$Version --nologo
if ($LASTEXITCODE -ne 0) { throw "打包失败，退出码 $LASTEXITCODE" }

$package = Get-ChildItem -LiteralPath $out -Filter "NewLife.Core.$Version.nupkg" | Select-Object -First 1
if (!$package) { throw "未找到 NewLife.Core nupkg" }

Write-Host "   包：$($package.Name)（版本 $Version）"

Write-Host "2/4 检查 nupkg 内容 ..."
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($package.FullName)
try {
    $entries = @($zip.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
    $required = @(
        "analyzers/dotnet/cs/NewLife.Core.SourceGenerator.dll",
        "lib/$Framework/NewLife.Core.dll",
        "Readme.MD",
        "leaf.png"
    )
    foreach ($item in $required) {
        if ($entries -notcontains $item) { throw "nupkg 缺少 $item" }
    }
    Write-Host "   必需项齐全（分析器 / lib / README / 图标）"
}
finally {
    $zip.Dispose()
}

Write-Host "3/4 创建消费项目（仅 PackageReference）..."
& dotnet new console -n Consumer -o $consumer -f $Framework --no-restore | Out-Null
if ($LASTEXITCODE -ne 0) { throw "创建临时项目失败" }

$nugetConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$out" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@
[IO.File]::WriteAllText((Join-Path $consumer "NuGet.config"), $nugetConfig, (New-Object System.Text.UTF8Encoding($false)))

& dotnet add (Join-Path $consumer "Consumer.csproj") package NewLife.Core --version $version --no-restore | Out-Null
if ($LASTEXITCODE -ne 0) { throw "添加 NewLife.Core 包引用失败" }

# 模拟下游组件：声明语言文件
$locales = Join-Path $consumer "Locales"
New-Item -ItemType Directory -Force -Path $locales | Out-Null
$yaml = @"
"Downstream probe message: {0}.": "கீழ்நிலை ஆய்வுச் செய்தி: {0}."
"Downstream probe without arguments.": "கீழ்நிலை ஆய்வு, அளவுரு இல்லை."
"@
[IO.File]::WriteAllText((Join-Path $locales "ta.yml"), $yaml, (New-Object System.Text.UTF8Encoding($false)))

$csproj = Join-Path $consumer "Consumer.csproj"
# 不修改 csproj：验证主包 build/NewLife.Core.props 的 Locales/*.yml 自动挂载
$xml = [IO.File]::ReadAllText($csproj)
if ($xml -match '<AdditionalFiles') { throw "消费项目不应手动声明 AdditionalFiles，自动挂载验证失效" }

$program = @"
using System;
using System.Globalization;
using NewLife;

Localization.Culture = CultureInfo.GetCultureInfo("ta-IN");
const String Key = "Downstream probe message: {0}.";
const String Expected = "கீழ்நிலை ஆய்வுச் செய்தி: 7.";

if (Localization.Translate(Key, 7) != Expected)
{
    Console.Error.WriteLine("Translate mismatch: " + Localization.Translate(Key, 7));
    return 1;
}

if (L.T(Key, 7) != Expected)
{
    Console.Error.WriteLine("L.T mismatch: " + L.T(Key, 7));
    return 1;
}

Localization.Culture = CultureInfo.GetCultureInfo("sw-KE");
if (Localization.Translate(Key, 7) != "Downstream probe message: 7.")
{
    Console.Error.WriteLine("Fallback mismatch: " + Localization.Translate(Key, 7));
    return 1;
}

Console.WriteLine("PASS");
return 0;
"@
[IO.File]::WriteAllText((Join-Path $consumer "Program.cs"), $program, (New-Object System.Text.UTF8Encoding($false)))

Write-Host "4/4 编译并运行消费项目 ..."
$output = & dotnet run --project $csproj -c $Configuration 2>&1
$exit = $LASTEXITCODE
$output | ForEach-Object { Write-Host "   $_" }
if ($exit -ne 0) { throw "消费项目运行失败，退出码 $exit" }
if (($output -join "`n") -notmatch "PASS") { throw "消费项目未输出 PASS" }

Write-Host "通过：下游仅用 PackageReference + Locales/*.yml 即可复用多语言" -ForegroundColor Green

if (!$Keep) { Remove-Item -LiteralPath $work -Recurse -Force }
else { Write-Host "临时目录已保留：$work" }
