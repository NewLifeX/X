# README 多语言切换行同步与校验
#
# 单一事实源：readme-languages.json。数组顺序即展示顺序，前两位固定为中文（权威版）与 English。
# 各 README 只有 <!-- lang:start --> 与 <!-- lang:end --> 之间的内容由本脚本生成，
# 并省略当前语种自身，与新语种追加到末尾的约定配套。
#
# 用法：
#   pwsh -File sync-readme-languages.ps1           # 按配置重写各 README 的切换块
#   pwsh -File sync-readme-languages.ps1 -Check    # 只校验不写入，不一致退出码 1（CI 使用）
#   pwsh -File sync-readme-languages.ps1 -Root D:\src\MyLib   # 下游在自己的仓根运行
param(
    [Switch]$Check,
    [String]$Root = $PSScriptRoot,
    [String]$Config = "readme-languages.json"
)

$ErrorActionPreference = "Stop"

$StartMarker = "<!-- lang:start -->"
$EndMarker = "<!-- lang:end -->"

$configPath = Join-Path $Root $Config
if (!(Test-Path -LiteralPath $configPath)) { throw "未找到语言配置：$configPath" }

$configData = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json
$langs = @($configData.languages)
if ($langs.Count -eq 0) { throw "$Config 未配置任何语种" }

$missing = @($langs | Where-Object { -not (Test-Path -Path (Join-Path $Root $_.file)) })
if ($missing.Count -gt 0) { throw "配置的语种文件不存在：$($missing.file -join ', ')" }

# 新增翻译若忘记登记，切换行会静默漏掉它，这里直接拦下
$registered = @($langs.file)
$unregistered = @(Get-ChildItem -Path $Root -File | Where-Object { $_.Name -match '^(README|Readme)(\.[A-Za-z-]+)?\.md$' -and $registered -notcontains $_.Name })
if ($unregistered.Count -gt 0) { throw "根目录存在未登记的 README：$($unregistered.Name -join ', ')，请先加入 $Config" }

# 生成某个 README 的切换行（省略自身）
function Get-SwitcherLine($current) {
    $parts = @()
    foreach ($item in $langs) {
        if ($item.file -eq $current.file) { continue }
        $parts += "[$($item.name)]($($item.file))"
    }
    return "🌐 " + ($parts -join " | ")
}

# 提取文件中现有的切换内容，用于 -Check 的差异提示
function Get-CurrentSwitcher($text, $pattern) {
    $m = [regex]::Match($text, $pattern)
    if (-not $m.Success) { return "<未找到切换行>" }
    $lines = @($m.Value -split "`r?`n" | Where-Object { $_ -notlike '<!-- lang:*' })
    return ($lines -join " / ")
}

$errors = 0
$changed = 0
foreach ($item in $langs) {
    $path = Join-Path $Root $item.file
    $text = [IO.File]::ReadAllText($path)
    $line = Get-SwitcherLine $item
    $newline = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $block = $StartMarker + $newline + $line + $newline + $EndMarker

    if ($text.Contains($StartMarker) -and $text.Contains($EndMarker)) {
        $pattern = "(?s)" + [regex]::Escape($StartMarker) + ".*?" + [regex]::Escape($EndMarker)
    }
    else {
        # 首次迁移：用标记块包住原切换行
        $pattern = "(?m)^🌐[^\r\n]*"
        if (-not [regex]::IsMatch($text, $pattern)) { throw "$($item.file) 既无 $StartMarker 标记，也找不到 🌐 切换行" }
    }

    # 替换文本中的 $ 需要转义，避免被当作正则替换组引用
    $updated = [regex]::Replace($text, $pattern, $block.Replace('$', '$$'))
    if ($updated -eq $text) { continue }

    $changed++
    if ($Check) {
        $errors++
        Write-Host "✗ $($item.file)"
        Write-Host "  期望：$line"
        Write-Host "  实际：$(Get-CurrentSwitcher $text $pattern)"
    }
    else {
        [IO.File]::WriteAllText($path, $updated, (New-Object System.Text.UTF8Encoding($false)))
        Write-Host "✓ $($item.file)"
    }
}

if ($Check) {
    if ($errors -gt 0) {
        Write-Host "`n语言切换行不一致：$errors / $($langs.Count) 个文件。请运行 pwsh -File sync-readme-languages.ps1 后提交。"
        exit 1
    }
    Write-Host "语言切换行一致（$($langs.Count) 个语种）"
}
elseif ($changed -eq 0) {
    Write-Host "语言切换行已是最新（$($langs.Count) 个语种）"
}
