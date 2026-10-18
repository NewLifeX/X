# 检查异常消息是否全部为英文
# 规则：throw new 之后的字符串中不得出现 CJK 字符；注释中的中文不受限制。
# 用法：pwsh -File check-english-exceptions.ps1
param(
    [String]$Root = $PSScriptRoot
)

$ErrorActionPreference = "Stop"

$dirs = @("NewLife.Core", "NewLife.Core.SourceGenerator") |
    ForEach-Object { Join-Path $Root $_ } |
    Where-Object { Test-Path $_ }

$hits = @()

foreach ($dir in $dirs) {
    foreach ($file in Get-ChildItem $dir -Recurse -Filter *.cs) {
        $lines = Get-Content $file.FullName -Encoding UTF8
        $inBlock = $false
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i].Trim()

            if ($line -match "/\*") { $inBlock = $true }
            if ($inBlock) {
                if ($line -match "\*/") { $inBlock = $false }
                continue
            }
            if ($line.StartsWith("//")) { continue }

            if (($line -match "throw new" -or $line -match ": base\(") -and $line -match "[\u4e00-\u9fa5]") {
                $rel = $file.FullName.Substring($Root.Length).TrimStart("\", "/")
                $hits += [pscustomobject]@{ File = $rel; Line = $i + 1; Text = $line }
            }
        }
    }
}

if ($hits.Count -gt 0) {
    Write-Host "发现 $($hits.Count) 处中文异常消息，请改为英文：" -ForegroundColor Red
    foreach ($hit in $hits) {
        Write-Host ("  {0}:{1}: {2}" -f $hit.File, $hit.Line, $hit.Text)
    }
    exit 1
}

Write-Host "OK: 未发现中文异常消息。"
exit 0
