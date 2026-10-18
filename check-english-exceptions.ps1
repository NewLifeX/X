# 检查运行时消息（异常、日志、控制台、界面提示、埋点标签）是否全部为英文
# 规则：调用点字符串中不得出现 CJK 字符；注释中的中文不受限制。
# 用法：pwsh -File check-english-exceptions.ps1
param(
    [String]$Root = $PSScriptRoot
)

$ErrorActionPreference = "Stop"

$dirs = @("NewLife.Core", "NewLife.Core.SourceGenerator") |
    ForEach-Object { Join-Path $Root $_ } |
    Where-Object { Test-Path $_ }

# 消息型调用点：异常、日志、控制台、界面提示、埋点标签
$messageCall = 'throw new|: base\(|Console\.Write|MessageBox\.Show|AppendTag\(|NewError\(|NewSpan\(|Debug\.Fail\(|WriteLog\(|OnWrite\(|XTrace\.|Log\.(Write|Error|Info|Debug|Warn|Fatal)'

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

            # 去掉行尾注释，避免把注释里的中文算作运行时消息
            $code = $line -replace "//.*$", ""
            # 特性参数（Description/DisplayName/Obsolete/RequiresXxx/Justification 等）属元数据，不在本检查范围
            if ($code -match "^\s*\[") { continue }
            if ($code -match $messageCall -and $code -match "[\u4e00-\u9fa5]") {
                $rel = $file.FullName.Substring($Root.Length).TrimStart("\", "/")
                $hits += [pscustomobject]@{ File = $rel; Line = $i + 1; Text = $code }
            }
        }
    }
}

if ($hits.Count -gt 0) {
    Write-Host "发现 $($hits.Count) 处中文运行时消息，请改为英文：" -ForegroundColor Red
    foreach ($hit in $hits) {
        Write-Host ("  {0}:{1}: {2}" -f $hit.File, $hit.Line, $hit.Text)
    }
    exit 1
}

Write-Host "OK: 未发现中文运行时消息。"
exit 0
