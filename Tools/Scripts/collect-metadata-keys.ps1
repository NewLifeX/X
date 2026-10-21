# 收集允许部分语言缺失的可选键（元数据、语言名等），写入 metadata-keys.json
# 默认只输出「在部分语言中缺失」的键，并保留清单中已有的手工条目；
# 加 -All 输出源码中扫描到的全部元数据键（用于人工筛查）。
#
# 用法：
#   pwsh -File Tools/Scripts/collect-metadata-keys.ps1
#   pwsh -File Tools/Scripts/collect-metadata-keys.ps1 -LocaleDir src\Lib\Locales -SourceDir src\Lib\src
#   pwsh -File Tools/Scripts/collect-metadata-keys.ps1 -Output metadata-keys.json -OutFile metadata-coverage.csv
param(
    [String]$LocaleDir = "NewLife.Core\Locales",
    [String[]]$SourceDir = @("NewLife.Core", "NewLife.Core.SourceGenerator"),
    [String]$Output = "metadata-keys.json",
    [Switch]$All,
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

# 默认输出的键：基准语言里有，但不是每个语言都有
$partial = [Collections.Generic.SortedSet[String]]::new([StringComparer]::Ordinal)
foreach ($key in $attributeKeys) {
    if (!$baseline.Contains($key)) { continue }
    foreach ($culture in $tables.Keys) {
        if ($culture -eq "en") { continue }
        if (!$tables[$culture].Contains($key)) { [void]$partial.Add($key); break }
    }
}

# 保留清单中已有的条目（语言名等非特性来源的可选键）
$existing = [Collections.Generic.SortedSet[String]]::new([StringComparer]::Ordinal)
if (Test-Path -LiteralPath $outputPath) {
    foreach ($item in (Get-Content -LiteralPath $outputPath -Encoding UTF8 -Raw | ConvertFrom-Json).keys) {
        if (![String]::IsNullOrEmpty($item)) { [void]$existing.Add($item) }
    }
}

$keys = [Collections.Generic.SortedSet[String]]::new([StringComparer]::Ordinal)
if ($All) {
    foreach ($key in $attributeKeys) { [void]$keys.Add($key) }
}
else {
    foreach ($key in $partial) { [void]$keys.Add($key) }
}
foreach ($key in $existing) { [void]$keys.Add($key) }

$json = [ordered]@{
    "comment" = "允许只出现在部分语言的可选键（元数据、语言名等）。由 collect-metadata-keys.ps1 生成，缺失时回退源文。"
    "updated" = (Get-Date -Format "yyyy-MM-dd")
    "keys"    = @($keys)
}
[IO.File]::WriteAllText($outputPath, ($json | ConvertTo-Json -Depth 3), [Text.UTF8Encoding]::new($false))

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

Write-Host "可选键清单：$($keys.Count) 条 → $outputPath"
Write-Host "源码元数据键：$($attributeKeys.Count) 条"
if ($missingInBaseline.Count -gt 0) {
    Write-Host "基准语言缺少以下元数据键，请补入 $baselineName.yml：" -ForegroundColor Yellow
    foreach ($key in $missingInBaseline) { Write-Host "  $key" -ForegroundColor Yellow }
}
