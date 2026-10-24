# 审计多语言元数据覆盖：扫描源码中的属性/程序集元数据键，报告各语言缺键与覆盖矩阵
# 同时算出「非源码来源键」（词表里存在但源码找不到，如语言名），它们是 metadata-keys.json 的内容。
#
# 用法：
#   pwsh -File Tools/Scripts/collect-metadata-keys.ps1                     # 只审计，不写清单
#   pwsh -File Tools/Scripts/collect-metadata-keys.ps1 -Update             # 重写 metadata-keys.json
#   pwsh -File Tools/Scripts/collect-metadata-keys.ps1 -OutFile coverage.csv
param(
    [String]$LocaleDir = "NewLife.Core\Locales",
    [String[]]$SourceDir = @("NewLife.Core", "NewLife.Core.SourceGenerator"),
    [String]$Output = "metadata-keys.json",
    [Switch]$Update,
    [String]$OutFile
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$localeDir = if ([IO.Path]::IsPathRooted($LocaleDir)) { $LocaleDir } else { Join-Path $root $LocaleDir }
if (!(Test-Path $localeDir)) {
    Write-Host "未找到语言目录：$localeDir" -ForegroundColor Red
    exit 1
}
$outputPath = if ([IO.Path]::IsPathRooted($Output)) { $Output } else { Join-Path $localeDir $Output }

$pattern = '^\s*"((?:[^"\\]|\\.)*)"\s*:\s*"((?:[^"\\]|\\.)*)"\s*$'
function Unescape-Yaml([string]$text) {
    return [regex]::Replace($text, '\\(u[0-9a-fA-F]{4}|.)', {
        param($m)
        $v = $m.Groups[1].Value
        if ($v.Length -eq 5) { return [string][char][Convert]::ToInt32($v.Substring(1), 16) }
        switch -CaseSensitive ($v) {
            'n' { return "`n" }
            'r' { return "`r" }
            't' { return "`t" }
            '\\' { return '\' }
            '"' { return '"' }
            default { return $m.Value }
        }
    })
}

# 扫描源码：特性参数与 csproj 的程序集元数据
$sourceDirs = @($SourceDir | ForEach-Object {
    if ([IO.Path]::IsPathRooted($_)) { $_ } else { Join-Path $root $_ }
} | Where-Object { Test-Path $_ })

$attributeKeys = [Collections.Generic.HashSet[String]]::new([StringComparer]::Ordinal)
foreach ($file in ($sourceDirs | ForEach-Object { Get-ChildItem $_ -Recurse -Include *.cs, *.csproj } |
    Where-Object { $_.FullName -notmatch '\\(obj|bin|Locales)\\' })) {
    if ($file.Extension -eq ".csproj") {
        $text = [IO.File]::ReadAllText($file.FullName)
        # 构建期分析器（Roslyn 组件）不对用户展示，其程序集描述不参与本地化
        if ($text.Contains("<IsRoslynComponent>true</IsRoslynComponent>")) { continue }
        foreach ($m in [regex]::Matches($text, '<(?:AssemblyTitle|Description)>([^<]+)</(?:AssemblyTitle|Description)>')) {
            $value = [System.Net.WebUtility]::HtmlDecode($m.Groups[1].Value.Trim())
            if ($value.Length -gt 0) { [void]$attributeKeys.Add($value) }
        }
        continue
    }

    # 逐行扫描并跳过注释行，避免把注释掉的特性当成源文
    $inBlock = $false
    foreach ($line in Get-Content -LiteralPath $file.FullName -Encoding UTF8) {
        $trimmed = $line.Trim()
        if ($inBlock) {
            if ($trimmed.Contains("*/")) { $inBlock = $false }
            continue
        }
        if ($trimmed.StartsWith("/*")) { $inBlock = !$trimmed.Contains("*/"); continue }
        if ($trimmed.StartsWith("//")) { continue }

        foreach ($m in [regex]::Matches($trimmed, '\[(?:DisplayName|Description)\("((?:[^"\\]|\\.)*)"')) {
            $value = $m.Groups[1].Value.Replace('\"', '"').Replace('\\', '\')
            if ($value.Length -gt 0) { [void]$attributeKeys.Add($value) }
        }
    }
}

# 读取各语言词条
$tables = [ordered]@{}
foreach ($file in Get-ChildItem $localeDir -Filter *.yml | Sort-Object Name) {
    $entries = [Collections.Generic.HashSet[String]]::new([StringComparer]::Ordinal)
    foreach ($line in Get-Content -LiteralPath $file.FullName -Encoding UTF8) {
        $m = [regex]::Match($line, $pattern)
        if ($m.Success) { [void]$entries.Add((Unescape-Yaml $m.Groups[1].Value)) }
    }
    $tables[$file.BaseName] = $entries
}

# 基准语言（zh-Hans.yml）必须覆盖全部元数据键
$baselineName = "zh-Hans"
$baseline = $tables[$baselineName]
if ($null -eq $baseline) { $baseline = $tables[(Get-ChildItem $localeDir -Filter *.yml | Select-Object -First 1).BaseName] }

$missingInBaseline = @($attributeKeys | Where-Object { !$baseline.Contains($_) } | Sort-Object)

# 词表里存在、但源码中找不到的键：这些才是需要登记到 metadata-keys.json 的「非源码来源键」
$sourceText = ($sourceDirs | ForEach-Object { Get-ChildItem $_ -Recurse -Include *.cs, *.csproj } |
    Where-Object { $_.FullName -notmatch '\\(obj|bin|Locales)\\' } |
    ForEach-Object { [IO.File]::ReadAllText($_.FullName) }) -join "`n"
$registryKeys = [Collections.Generic.SortedSet[String]]::new([StringComparer]::Ordinal)
foreach ($key in $baseline) {
    $escaped = $key.Replace('\', '\\').Replace('"', '\"').Replace("`t", '\t').Replace("`r", '\r').Replace("`n", '\n')
    if (!$sourceText.Contains($key) -and !$sourceText.Contains($escaped)) { [void]$registryKeys.Add($key) }
}

if ($Update) {
    $json = [ordered]@{
        "comment" = "非源码来源键（语言名、外部元数据等），豁免孤儿键判定；词表仍要求全语言齐全。由 collect-metadata-keys.ps1 -Update 生成。"
        "updated" = (Get-Date -Format "yyyy-MM-dd")
        "keys"    = @($registryKeys)
    }
    [IO.File]::WriteAllText($outputPath, ($json | ConvertTo-Json -Depth 3), [Text.UTF8Encoding]::new($false))
    Write-Host "已写入非源码来源键清单：$($registryKeys.Count) 条 → $outputPath"
}
else {
    Write-Host "非源码来源键候选：$($registryKeys.Count) 条（加 -Update 写入清单）"
}

if ($OutFile) {
    $rows = @()
    foreach ($key in $attributeKeys) {
        $row = [ordered]@{ Key = $key }
        foreach ($culture in $tables.Keys) { $row[$culture] = if ($tables[$culture].Contains($key)) { "Y" } else { "" } }
        $rows += [pscustomobject]$row
    }
    $rows | Export-Csv -LiteralPath $OutFile -NoTypeInformation -Encoding UTF8
    Write-Host "已导出覆盖矩阵：$OutFile"
}

Write-Host "源码元数据键：$($attributeKeys.Count) 条"
if ($missingInBaseline.Count -gt 0) {
    Write-Host "基准语言缺少以下元数据键，请补入 $baselineName.yml：" -ForegroundColor Yellow
    foreach ($key in $missingInBaseline) { Write-Host "  $key" -ForegroundColor Yellow }
}

# 全语言覆盖检查：元数据键必须出现在每一种语言
$incomplete = @{}
foreach ($key in $attributeKeys) {
    if (!$baseline.Contains($key)) { continue }
    foreach ($culture in $tables.Keys) {
        if ($culture -eq "en") { continue }
        if (!$tables[$culture].Contains($key)) {
            if (!$incomplete.ContainsKey($key)) { $incomplete[$key] = @() }
            $incomplete[$key] += $culture
        }
    }
}
if ($incomplete.Count -gt 0) {
    Write-Host "以下元数据键未覆盖全部语言：" -ForegroundColor Yellow
    foreach ($key in ($incomplete.Keys | Sort-Object)) { Write-Host ("  {0} → 缺 {1}" -f $key, ($incomplete[$key] -join ',')) -ForegroundColor Yellow }
}
else {
    Write-Host "元数据键全语言覆盖：通过（$($attributeKeys.Count) 条）"
}
