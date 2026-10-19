# README NuGet 包版生成与校验
#
# 源：README.md（English，默认落地页）
# 目标：README.nuget.md（NuGet 包内展示，所有相对链接改写为 GitHub 绝对地址）
# NuGet 不支持相对链接，因此包内 README 必须独立生成，不能直接嵌入 README.md。
#
# 用法：
#   pwsh -File sync-readme-package.ps1           # 生成
#   pwsh -File sync-readme-package.ps1 -Check    # 只校验不写入，不一致退出码 1（CI 使用）
#   pwsh -File sync-readme-package.ps1 -Root D:\src\MyLib
param(
    [Switch]$Check,
    [String]$Root = $PSScriptRoot,
    [String]$Config = "readme-package.json"
)

$ErrorActionPreference = "Stop"

$configPath = Join-Path $Root $Config
if (!(Test-Path -LiteralPath $configPath)) { throw "未找到配置：$configPath" }

$data = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json
$repository = ([String]$data.repository).TrimEnd('/')
$branch = [String]$data.branch
if (!$repository -or !$branch) { throw "$Config 缺少 repository 或 branch" }

$items = @($data.items)
if ($items.Count -eq 0) { throw "$Config 未配置 items" }

function Convert-Link([String]$target) {
    $value = $target.Trim()
    if ($value -match '^(https?://|mailto:|#|//)') { return $target }

    $anchor = ""
    $hash = $value.IndexOf('#')
    if ($hash -ge 0) {
        $anchor = $value.Substring($hash)
        $value = $value.Substring(0, $hash)
    }
    if ($value -eq "") { return $anchor }

    $value = $value -replace '^\./', ''
    $value = $value -replace '\\', '/'
    return "$repository/blob/$branch/$value$anchor"
}

function Convert-Readme([String]$text) {
    # 只改写 Markdown 链接/图片目标，保留绝对地址与页内锚点；跳过围栏代码块
    $pattern = '\]\(([^)\s]+)(\s+(?:"[^"]*"|''[^'']*''))?\)'
    $lines = [regex]::Split($text, "(`r`n|`n)")
    $inFence = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match '^\s*(```|~~~)') {
            $inFence = !$inFence
            continue
        }
        if ($inFence -or $line -notmatch '\]\(') { continue }

        $lines[$i] = [regex]::Replace($line, $pattern, [System.Text.RegularExpressions.MatchEvaluator]{
            param($m)
            $title = if ($m.Groups[2].Success) { $m.Groups[2].Value } else { "" }
            return "](" + (Convert-Link $m.Groups[1].Value) + $title + ")"
        })
    }
    return ($lines -join "")
}

# 大小写敏感的路径存在性检查，避免 Windows 上大小写不一致却误判为存在
function Test-ExactPath([String]$path) {
    if (!(Test-Path -LiteralPath $path)) { return $false }

    $dir = Split-Path -Parent $path
    $name = Split-Path -Leaf $path
    if (!$dir) { $dir = "." }

    return [bool](Get-ChildItem -LiteralPath $dir -File -ErrorAction SilentlyContinue | Where-Object { $_.Name -ceq $name })
}

$errors = 0
$changed = 0
foreach ($item in $items) {
    $source = Join-Path $Root $item.source
    $output = Join-Path $Root $item.output
    if (!(Test-Path -LiteralPath $source)) { throw "源文件不存在：$source" }

    $text = [IO.File]::ReadAllText($source)
    $converted = Convert-Readme $text
    $header = "<!-- generated from $($item.source) by sync-readme-package.ps1; do not edit -->"
    $newline = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $result = $header + $newline + $newline + $converted
    if (!$result.EndsWith($newline)) { $result += $newline }

    $existing = if (Test-Path -LiteralPath $output) { [IO.File]::ReadAllText($output) } else { $null }
    if ($existing -ne $result) {
        $changed++
        if ($Check) {
            $errors++
            Write-Host "✗ $($item.output) 与 $($item.source) 的生成结果不一致"
        }
        else {
            $dir = Split-Path -Parent $output
            if (!(Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
            [IO.File]::WriteAllText($output, $result, (New-Object System.Text.UTF8Encoding($false)))
            Write-Host "✓ $($item.output)"
        }
    }
}

# 校验所有 csproj 的 PackageReadmeFile 都有对应的打包项，且源文件真实存在
$projects = @(Get-ChildItem -Path $Root -Recurse -Filter *.csproj -File -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })
foreach ($project in $projects) {
    $content = [IO.File]::ReadAllText($project.FullName)
    $match = [regex]::Match($content, '<PackageReadmeFile>\s*([^<]+?)\s*</PackageReadmeFile>')
    if (!$match.Success) { continue }

    $readmeName = Split-Path -Leaf ($match.Groups[1].Value -replace '\\', '/')
    [xml]$xml = $content
    $found = $false
    foreach ($group in @($xml.Project.ItemGroup)) {
        foreach ($tag in @('None', 'Content')) {
            foreach ($node in @($group.$tag)) {
                if (!$node -or !$node.Include) { continue }
                $include = [String]$node.Include
                if ((Split-Path -Leaf ($include -replace '\\', '/')) -cne $readmeName) { continue }

                $full = Join-Path $project.DirectoryName ($include -replace '\\', '/')
                if (Test-ExactPath $full) { $found = $true; break }
            }
            if ($found) { break }
        }
        if ($found) { break }
    }

    if (!$found) {
        $errors++
        Write-Host "✗ $($project.FullName)：PackageReadmeFile=$readmeName 没有对应的打包项或文件不存在"
    }
}

# 生成物不得残留相对链接
foreach ($item in $items) {
    $output = Join-Path $Root $item.output
    if (!(Test-Path -LiteralPath $output)) { continue }

    $text = [IO.File]::ReadAllText($output)
    $relative = [regex]::Matches($text, '\]\((?!https?://|mailto:|#|//)[^)\s]+')
    if ($relative.Count -gt 0) {
        $errors++
        Write-Host "✗ $($item.output) 仍含 $($relative.Count) 个相对链接"
        $relative | Select-Object -First 5 | ForEach-Object { Write-Host "  $($_.Value)" }
    }
}

if ($Check) {
    if ($errors -gt 0) {
        Write-Host "`nREADME 包版校验失败：$errors 项。请运行 pwsh -File sync-readme-package.ps1 后提交。"
        exit 1
    }
    Write-Host "README 包版一致（$($items.Count) 个文件）"
}
elseif ($changed -eq 0) {
    Write-Host "README 包版已是最新（$($items.Count) 个文件）"
}
