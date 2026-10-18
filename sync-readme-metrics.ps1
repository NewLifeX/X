# README 指标同步与校验
#
# 单一事实源：readme-metrics.json。AOT 冒烟场景数、TFM 数量、内置语言数等
# 会随发布变化的数字，不再散落在 11 份 README 里手工维护。
#
# 用法：
#   pwsh -File sync-readme-metrics.ps1 -Migrate   # 首次迁移：给旧数字套上标记，并生成徽章块
#   pwsh -File sync-readme-metrics.ps1            # 按配置写入标记块与数字
#   pwsh -File sync-readme-metrics.ps1 -Check     # 只校验不写入，不一致退出码 1（CI 使用）
param(
    [Switch]$Check,
    [Switch]$Migrate,
    [String]$Root = $PSScriptRoot,
    [String]$Config = "readme-metrics.json"
)

$ErrorActionPreference = "Stop"
if ($Check -and $Migrate) { throw "-Check 与 -Migrate 不能同时使用" }

$configPath = Join-Path $Root $Config
if (!(Test-Path -LiteralPath $configPath)) { throw "未找到指标配置：$configPath" }

$metrics = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json
$values = @{
    "aot-smoke"     = [String]$metrics.values.aotSmoke
    "aot-scenarios" = [String]$metrics.values.aotScenarios
    "tfm-count"     = [String]$metrics.values.tfmCount
    "locale-count"  = [String]$metrics.values.localeCount
}

$files = @(Get-ChildItem -Path $Root -File | Where-Object {
    $_.Name -match '^(README|Readme)(\.[A-Za-z-]+)?\.md$' -and $_.Name -notmatch '\.template\.'
} | Sort-Object Name)
if ($files.Count -eq 0) { throw "根目录未找到 README 文件" }

$StartMarker = "<!-- metrics:start -->"
$EndMarker = "<!-- metrics:end -->"
$BlockPattern = "(?s)" + [regex]::Escape($StartMarker) + ".*?" + [regex]::Escape($EndMarker)

# AOT 冒烟徽章里的斜杠需要 URL 编码，否则 shields.io 会把斜杠当路径分隔符
$smokeEncoded = [Uri]::EscapeDataString($values["aot-smoke"])

function Get-MetricsBlock([String]$newline) {
    $lines = @(
        $StartMarker
        "[![AOT smoke](https://img.shields.io/badge/AOT%20smoke-$smokeEncoded-brightgreen?logo=dotnet)](Doc/AOT兼容性分析.md)"
        "[![TFM](https://img.shields.io/badge/TFM-$($values["tfm-count"])%20%28net45%20%E2%86%92%20net10.0%29-512BD4?logo=dotnet)](https://www.nuget.org/packages/NewLife.Core)"
        "[![locales](https://img.shields.io/badge/locales-$($values["locale-count"])-2ea44f?logo=dotnet)](Doc/多语言本地化.md)"
        $EndMarker
    )
    return $lines -join $newline
}

function Set-MarkerValue([String]$text, [String]$name, [String]$value) {
    $pattern = "(?s)(<!--\s*" + [regex]::Escape($name) + ":start\s*-->).*?(<!--\s*" + [regex]::Escape($name) + ":end\s*-->)"
    return [regex]::Replace($text, $pattern, [System.Text.RegularExpressions.MatchEvaluator]{
        param($m)
        return $m.Groups[1].Value + $value + $m.Groups[2].Value
    })
}

function Convert-Migrate([String]$text, [String]$file) {
    $newline = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    if ($text.Contains($StartMarker) -and $text.Contains($EndMarker)) {
        # 已迁移过，只更新徽章块
        return [regex]::Replace($text, $BlockPattern, [System.Text.RegularExpressions.MatchEvaluator]{
            param($m) return (Get-MetricsBlock $newline)
        })
    }

    # 旧徽章：AOT smoke 与 TFM 两行，替换为统一指标块
    $lines = $text -split "\r?\n"
    $aotIndex = -1
    $tfmIndex = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($aotIndex -lt 0 -and $lines[$i] -match '\[!\[AOT smoke\]\(') { $aotIndex = $i }
        if ($tfmIndex -lt 0 -and $lines[$i] -match '\[!\[TFM\]\(') { $tfmIndex = $i }
    }
    if ($aotIndex -lt 0) { throw "$file 未找到 AOT smoke 徽章，无法迁移" }

    $insertAt = $aotIndex
    $remove = @($aotIndex)
    if ($tfmIndex -ge 0) { $remove += $tfmIndex }
    $newLines = @()
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($remove -contains $i) {
            if ($i -eq $insertAt) { $newLines += (Get-MetricsBlock $newline) }
            continue
        }
        $newLines += $lines[$i]
    }
    $text = $newLines -join $newline

    # 先处理 29/29，再处理单独的 29；避免把分子分母拆开
    if (!$text.Contains('<!--aot-smoke:start-->')) {
        $text = [regex]::Replace($text, '(?<![\w%])29/29(?![\w%])', '<!--aot-smoke:start-->29/29<!--aot-smoke:end-->')
    }
    if (!$text.Contains('<!--aot-scenarios:start-->')) {
        $text = [regex]::Replace($text, '(?<![\w%])29(?=\s|\p{IsHangulSyllables})', '<!--aot-scenarios:start-->29<!--aot-scenarios:end-->')
    }
    return $text
}

function Convert-Sync([String]$text) {
    if (!$text.Contains($StartMarker) -or !$text.Contains($EndMarker)) { return $text }
    $newline = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $text = [regex]::Replace($text, $BlockPattern, [System.Text.RegularExpressions.MatchEvaluator]{
        param($m) return (Get-MetricsBlock $newline)
    })
    foreach ($name in $values.Keys) {
        $text = Set-MarkerValue $text $name $values[$name]
    }
    return $text
}

$errors = 0
$changed = 0
foreach ($file in $files) {
    $path = $file.FullName
    $text = [IO.File]::ReadAllText($path)

    if ($Migrate) {
        $newText = Convert-Sync (Convert-Migrate $text $file.Name)
    }
    elseif (!$text.Contains($StartMarker) -or !$text.Contains($EndMarker)) {
        throw "$($file.Name) 缺少指标标记块，请先运行 pwsh -File sync-readme-metrics.ps1 -Migrate"
    }
    else {
        $newText = Convert-Sync $text
    }

    if ($Check -and ($newText -notmatch '<!--aot-smoke:start-->' -or $newText -notmatch '<!--aot-scenarios:start-->')) {
        $errors++
        Write-Host "✗ $($file.Name)"
        Write-Host "  缺少 aot-smoke / aot-scenarios 标记，请运行 pwsh -File sync-readme-metrics.ps1 -Migrate"
        continue
    }

    if ($text -eq $newText) { continue }

    if ($Check) {
        $errors++
        Write-Host "✗ $($file.Name)"
        Write-Host "  指标块或标记数字与 readme-metrics.json 不一致，请运行 pwsh -File sync-readme-metrics.ps1"
    }
    else {
        [IO.File]::WriteAllText($path, $newText, (New-Object System.Text.UTF8Encoding($false)))
        $changed++
        Write-Host "✓ $($file.Name)"
    }
}

if ($Check) {
    if ($errors -gt 0) {
        Write-Host "`nREADME 指标不一致：$errors / $($files.Count) 个文件。"
        exit 1
    }
    Write-Host "README 指标一致（$($files.Count) 个文件，AOT $($values["aot-smoke"])，TFM $($values["tfm-count"])，语言 $($values["locale-count"])）"
}
elseif ($changed -eq 0) {
    Write-Host "README 指标已是最新（$($files.Count) 个文件）"
}
