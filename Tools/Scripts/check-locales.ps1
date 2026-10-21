# 多语言词表一致性校验：键集合、占位符、格式说明符、孤儿键
# 以基准语言文件（默认 zh-Hans.yml）为准，所有语言必须提供完全相同的键集合；
# 登记在 metadata-keys.json 的可选键（元数据、语言名等）允许只出现在部分语言，缺失时回退源文。
# en.yml 为可选：只需列出源文非英文的键，缺失的消息键自动回退英文源文。
#
# 用法：
#   pwsh -File Tools/Scripts/check-locales.ps1                                      # Core 默认目录
#   pwsh -File Tools/Scripts/check-locales.ps1 -LocaleDir src\Lib\Locales -SourceDir src\Lib
#   pwsh -File Tools/Scripts/check-locales.ps1 -LocaleDir src\Lib\Locales -SkipOrphan   # 下游只想校验词表内部一致性
#   pwsh -File Tools/Scripts/check-locales.ps1 -MetadataKeys src\Lib\Locales\metadata-keys.json
param(
    [String]$LocaleDir = "NewLife.Core\Locales",
    [String]$Baseline = "zh-Hans.yml",
    [String[]]$SourceDir = @("NewLife.Core", "NewLife.Core.SourceGenerator"),
    [String]$MetadataKeys,
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

# 可选键：只出现在部分语言也合法，缺失时回退源文（元数据、语言名等）
if (!$MetadataKeys) {
    $candidate = Join-Path $localeDir "metadata-keys.json"
    if (Test-Path -LiteralPath $candidate) { $MetadataKeys = $candidate }
}
$optionalKeys = [Collections.Generic.HashSet[String]]::new([StringComparer]::Ordinal)
if ($MetadataKeys) {
    $metadataPath = if ([IO.Path]::IsPathRooted($MetadataKeys)) { $MetadataKeys } else { Join-Path $root $MetadataKeys }
    if (!(Test-Path -LiteralPath $metadataPath)) {
        Write-Host "未找到可选键清单：$metadataPath" -ForegroundColor Red
        exit 1
    }
    foreach ($item in (Get-Content -LiteralPath $metadataPath -Encoding UTF8 -Raw | ConvertFrom-Json).keys) {
        if (![String]::IsNullOrEmpty($item)) { [void]$optionalKeys.Add($item) }
    }
}

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
    # en 为可选词表：元数据源文非英文时用它提供英文译文，未列出的消息键回退英文源文
    $isEnglish = $file.BaseName -eq "en"
    $locale = Read-Locale $file.FullName
    foreach ($e in $locale.Errors) { $allErrors += "$($file.Name): $e" }

    if (!$isEnglish) {
        foreach ($key in $reference.Entries.Keys) {
            if ($locale.Entries.Contains($key) -or $optionalKeys.Contains($key)) { continue }
            $allErrors += "$($file.Name): 缺少键 $key"
        }
    }
    foreach ($key in $locale.Entries.Keys) {
        if (!$reference.Entries.Contains($key) -and !$optionalKeys.Contains($key)) {
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

# 孤儿键：基准表中的键在源码里完全找不到（改文案后遗留的死条目）
# 源文范围含 .cs 与 .csproj：程序集标题/描述等元数据写在 csproj 中，同样是词表源文
$orphans = @()
$sourceDirs = @()
if (!$SkipOrphan) {
    $sourceDirs = @($SourceDir | ForEach-Object {
        if ([IO.Path]::IsPathRooted($_)) { $_ } else { Join-Path $root $_ }
    } | Where-Object { Test-Path $_ })

    $sourceText = ($sourceDirs | ForEach-Object { Get-ChildItem $_ -Recurse -Include *.cs, *.csproj } |
        Where-Object { $_.FullName -notmatch '\\(obj|bin|Locales)\\' } |
        ForEach-Object { [IO.File]::ReadAllText($_.FullName) }) -join "`n"

    $orphans = @()
    foreach ($key in $reference.Entries.Keys) {
        # 可选键（元数据、语言名）由外部数据或其它来源引用，不参与源码归属判定
        if ($optionalKeys.Contains($key)) { continue }

        # 源码中的字面量形式：制表符写作 \t，引号写作 \"
        $escaped = $key.Replace('\', '\\').Replace('"', '\"').Replace("`t", '\t').Replace("`r", '\r').Replace("`n", '\n')
        if (!$sourceText.Contains($key) -and !$sourceText.Contains($escaped)) {
            $orphans += $key
        }
    }
    foreach ($o in $orphans) { $allErrors += "孤儿键（源码中找不到）：$o" }
}

# 源码消息回填：显式调用 L.T / Localization.Translate 的字面量必须有词条，否则该处永远只能得到英文
if (!$SkipOrphan) {
    $callPattern = '(?:L\.T|L\.TLog|Localization\.Translate(?:ForLog)?)\s*\(\s*(?:[A-Za-z_][\w\.]*\s*,\s*)?"((?:[^"\\]|\\.)*)"'
    $missingMessages = [Collections.Generic.SortedSet[String]]::new([StringComparer]::Ordinal)
    foreach ($file in ($sourceDirs | ForEach-Object { Get-ChildItem $_ -Recurse -Filter *.cs } |
        Where-Object { $_.FullName -notmatch '\\(obj|bin|Locales)\\' })) {
        $text = [IO.File]::ReadAllText($file.FullName)
        foreach ($m in [regex]::Matches($text, $callPattern)) {
            $literal = $m.Groups[1].Value.Replace('\"', '"').Replace('\\', '\').Replace('\n', "`n").Replace('\r', "`r").Replace('\t', "`t")
            if ($literal.Length -eq 0 -or $reference.Entries.Contains($literal)) { continue }
            [void]$missingMessages.Add($literal)
        }
    }
    foreach ($key in $missingMessages) { $allErrors += "缺少词条（源码已调用 L.T）：$key" }
}

foreach ($e in $allErrors) { Write-Host $e -ForegroundColor Red }
foreach ($w in $allWarnings) { Write-Host $w -ForegroundColor Yellow }

$count = (Get-ChildItem $localeDir -Filter *.yml).Count
if ($allErrors.Count -gt 0) {
    Write-Host "多语言词表校验未通过：$count 个语言，$($allErrors.Count) 个错误" -ForegroundColor Red
    exit 1
}

Write-Host "多语言词表校验通过：$count 个语言，基准 $($reference.Entries.Count) 条" -ForegroundColor Green
