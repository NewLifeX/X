# 检查 AOT 注解书写规范（规则见 Doc/AOT兼容性分析.md §3.3）
# 规则：
#   1) DynamicallyAccessedMembers / DynamicDependency 不得内联 DynamicallyAccessedMemberTypes 位或，改用 NewLife.Dam 预设
#   2) 不得用 #pragma warning disable IL2xxx/IL3xxx 抑制裁剪告警，ILC 只认 UnconditionalSuppressMessage
#
# 用法：
#   pwsh -File Tools/Scripts/check-aot-annotations.ps1           # 失败退出码 1
#   pwsh -File Tools/Scripts/check-aot-annotations.ps1 -Dir src  # 下游复用
param(
    # 默认仓库根：脚本位于 Tools\Scripts，在其上两级；下游复用可显式传入 -Root
    [String]$Root = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),
    [String[]]$Dir
)

$ErrorActionPreference = "Stop"

if (!$Dir -or $Dir.Count -eq 0) { $Dir = @("NewLife.Core") }
$dirs = @()
foreach ($item in $Dir) {
    $path = if ([IO.Path]::IsPathRooted($item)) { $item } else { Join-Path $Root $item }
    if (Test-Path -LiteralPath $path) { $dirs += $path }
}
if ($dirs.Count -eq 0) { throw "未找到待检查目录：$($Dir -join ', ')" }

$rules = @(
    @{ Name = "内联标志位组合"; Regex = "(?:DynamicallyAccessedMembers|DynamicDependency)\s*\([^)]*\|[^)]*\)" },
    @{ Name = "pragma 抑制裁剪告警"; Regex = "#pragma\s+warning\s+disable[^\r\n]*\bIL[23]\d{3}\b" }
)

$hits = @()

foreach ($dir in $dirs) {
    foreach ($file in Get-ChildItem $dir -Recurse -Filter *.cs) {
        $rel = if ($file.FullName.StartsWith($Root, [StringComparison]::OrdinalIgnoreCase)) {
            $file.FullName.Substring($Root.Length).TrimStart("\", "/")
        }
        else { $file.FullName }

        if ($rel -match '\\(obj|bin)\\') { continue }
        # 预设定义处（Aot）与低版本垫片（Stub）不适用本规则
        if ($rel -match '\\(Aot|Stub)\\') { continue }

        $text = [IO.File]::ReadAllText($file.FullName)
        foreach ($rule in $rules) {
            foreach ($match in [regex]::Matches($text, $rule.Regex)) {
                $line = $text.Substring(0, $match.Index).Split("`n").Count
                $hits += [pscustomobject]@{ File = $rel; Line = $line; Rule = $rule.Name; Text = $match.Value.Trim() }
            }
        }
    }
}

if ($hits.Count -gt 0) {
    Write-Host "发现 $($hits.Count) 处 AOT 注解不合规，请按 Doc/AOT兼容性分析.md §3.3 修正：" -ForegroundColor Red
    foreach ($hit in $hits) {
        $text = if ($hit.Text.Length -gt 120) { $hit.Text.Substring(0, 120) + "..." } else { $hit.Text }
        Write-Host ("  {0}:{1} [{2}] {3}" -f $hit.File, $hit.Line, $hit.Rule, $text)
    }
    exit 1
}

Write-Host "OK: AOT 注解书写符合规范。"
exit 0