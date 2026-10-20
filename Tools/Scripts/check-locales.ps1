# 多语言词表一致性校验：键集合、占位符、格式说明符
# 以基准语言文件（默认 zh-Hans.yml）为准，所有语言必须提供完全相同的键集合
#
# 用法：
#   pwsh -File Tools/Scripts/check-locales.ps1                                      # Core 默认目录
#   pwsh -File Tools/Scripts/check-locales.ps1 -LocaleDir src\Lib\Locales -SourceDir src\Lib
#   pwsh -File Tools/Scripts/check-locales.ps1 -LocaleDir src\Lib\Locales -SkipOrphan   # 下游只想校验词表内部一致性
param(
    [String]$LocaleDir = "NewLife.Core\Locales",
    [String]$Baseline = "zh-Hans.yml",
    [String[]]$SourceDir = @("NewLife.Core", "NewLife.Core.SourceGenerator"),
    [Switch]$SkipOrphan
)

$ErrorActionPreference = "Stop"

# 脚本位于 Tools\Scripts，仓库根目录在其上两级
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$localeDir = if ([IO.Path]::IsPathRooted($LocaleDir)) { $LocaleDir } else { Join-Path $root $LocaleDir }
if (!(Test-Path $localeDir)) {
    Write-Host "未找到语言目录：$localeDir" -ForegroundColor Red
    exit 1
}

$pattern = '^\s*"((?:[^"\\]|\\.)*)"\s*:\s*"((?:[^"\\]|\\.)*)"\s*$'
$placeholder = '\{(\d+)(?:,-?\d+)?(:[^}]*)?\}'
$languageCode = '\b[a-z]{2}-[A-Za-z]{2,4}\b'

function Unescape-Yaml([string]$text) {
    # 与源生成器保持一致：还原 \\ \" \n \r \t \uXXXX
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

function Get-Placeholders([string]$text) {
    $set = [System.Collections.Generic.HashSet[int]]::new()
    foreach ($m in [regex]::Matches($text, $placeholder)) {
        [void]$set.Add([int]$m.Groups[1].Value)
    }
    return ,$set
}

function Get-PlaceholderSpecs([string]$text) {
    $map = @{}
    foreach ($m in [regex]::Matches($text, $placeholder)) {
        $index = [int]$m.Groups[1].Value
        if (!$map.ContainsKey($index)) { $map[$index] = $m.Groups[2].Value }
    }
    return $map
}

# 语言清单条目：译文中的语言代码集合必须与英文 key 一致，防止译者漏码
function Test-LanguageCodeList([string]$key, [string]$value) {
    if (!$key.StartsWith("UI language. Values:")) { return $true }
    $expected = @([regex]::Matches($key, $languageCode) | ForEach-Object { $_.Value } | Sort-Object -Unique)
    $actual = @([regex]::Matches($value, $languageCode) | ForEach-Object { $_.Value } | Sort-Object -Unique)
    return !(Compare-Object $expected $actual)
}

function Read-Locale([string]$path) {
    $entries = [ordered]@{}
    $errors = @()
    $lineNo = 0
    foreach ($line in Get-Content -LiteralPath $path -Encoding UTF8) {
        $lineNo++
        if ($line.Trim().Length -eq 0 -or $line.TrimStart().StartsWith("#")) { continue }
        $m = [regex]::Match($line, $pattern)
        if (!$m.Success) {
            $errors += "第 $lineNo 行格式非法：$line"
            continue
        }
        $key = Unescape-Yaml $m.Groups[1].Value
        if ($entries.Contains($key)) {
            $errors += "第 $lineNo 行键重复：$key"
            continue
        }
        $entries[$key] = Unescape-Yaml $m.Groups[2].Value
    }
    return @{ Entries = $entries; Errors = $errors }
}

$referencePath = Join-Path $localeDir $Baseline
if (!(Test-Path -LiteralPath $referencePath)) {
    Write-Host "未找到基准语言文件：$referencePath" -ForegroundColor Red
    exit 1
}
$reference = Read-Locale $referencePath
$allErrors = @($reference.Errors | ForEach-Object { "$($Baseline): $_" })
$allWarnings = @()

foreach ($key in $reference.Entries.Keys) {
    if (!(Test-LanguageCodeList $key $reference.Entries[$key])) {
        $allErrors += "$($Baseline): 语言代码清单不一致 → $key"
    }
}

foreach ($file in Get-ChildItem $localeDir -Filter *.yml | Sort-Object Name) {
    if ($file.Name -eq $Baseline) { continue }
    $locale = Read-Locale $file.FullName
    foreach ($e in $locale.Errors) { $allErrors += "$($file.Name): $e" }

    foreach ($key in $reference.Entries.Keys) {
        if (!$locale.Entries.Contains($key)) {
            $allErrors += "$($file.Name): 缺少键 $key"
        }
    }
    foreach ($key in $locale.Entries.Keys) {
        if (!$reference.Entries.Contains($key)) {
            $allErrors += "$($file.Name): 多余键 $key"
            continue
        }

        $value = $locale.Entries[$key]
        $expected = Get-Placeholders $key
        $actual = Get-Placeholders $value
        if (!$expected.SetEquals($actual)) {
            $allErrors += "$($file.Name): 占位符不一致 → $key"
            continue
        }

        # 格式说明符只在译文保留该占位符时比对，缺失时仅告警
        $enSpecs = Get-PlaceholderSpecs $key
        $trSpecs = Get-PlaceholderSpecs $value
        foreach ($index in $enSpecs.Keys) {
            if ($trSpecs.ContainsKey($index) -and $trSpecs[$index] -ne $enSpecs[$index]) {
                $allWarnings += "$($file.Name): 格式说明符不一致 → $key"
                break
            }
        }

        if (!(Test-LanguageCodeList $key $value)) {
            $allErrors += "$($file.Name): 语言代码清单不一致 → $key"
        }
    }
}

foreach ($e in $allErrors) { Write-Host $e -ForegroundColor Red }
foreach ($w in $allWarnings) { Write-Host $w -ForegroundColor Yellow }

# 孤儿键：基准表中的键在源码里完全找不到（改文案后遗留的死条目）
$orphans = @()
if (!$SkipOrphan) {
    $sourceDirs = @($SourceDir | ForEach-Object {
        if ([IO.Path]::IsPathRooted($_)) { $_ } else { Join-Path $root $_ }
    } | Where-Object { Test-Path $_ })

    $sourceText = ($sourceDirs | ForEach-Object { Get-ChildItem $_ -Recurse -Filter *.cs } |
        Where-Object { $_.FullName -notmatch '\\(obj|bin|Locales)\\' } |
        ForEach-Object { [IO.File]::ReadAllText($_.FullName) }) -join "`n"

    foreach ($key in $reference.Entries.Keys) {
        # 源码中的字面量形式：制表符写作 \t，引号写作 \"
        $escaped = $key.Replace('\', '\\').Replace('"', '\"').Replace("`t", '\t').Replace("`r", '\r').Replace("`n", '\n')
        if (!$sourceText.Contains($key) -and !$sourceText.Contains($escaped)) {
            $orphans += $key
        }
    }
    foreach ($o in $orphans) { $allErrors += "孤儿键（源码中找不到）：$o" }
}

if ($allErrors.Count -gt 0) {
    foreach ($o in $orphans) { Write-Host "孤儿键（源码中找不到）：$o" -ForegroundColor Red }
}

$count = (Get-ChildItem $localeDir -Filter *.yml).Count
if ($allErrors.Count -gt 0) {
    Write-Host "多语言词表校验未通过：$count 个语言，$($allErrors.Count) 个错误" -ForegroundColor Red
    exit 1
}

Write-Host "多语言词表校验通过：$count 个语言，基准 $($reference.Entries.Count) 条" -ForegroundColor Green
