# 检查运行时消息（异常、日志、控制台、界面提示、埋点标签）是否全部为英文
# 规则：调用点字符串中不得出现 CJK 字符；注释中的中文不受限制。
#
# 用法：
#   pwsh -File Tools/Scripts/check-english-exceptions.ps1                         # Core 默认目录，失败退出码 1
#   pwsh -File Tools/Scripts/check-english-exceptions.ps1 -Dir src -List          # 下游盘点中文消息，不失败
#   pwsh -File Tools/Scripts/check-english-exceptions.ps1 -Dir src -Csv hits.csv  # 导出待翻译清单
param(
    # 默认仓库根：脚本位于 Tools\Scripts，在其上两级；下游复用可显式传入 -Root
    [String]$Root = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),
    [String[]]$Dir,
    [String[]]$Exclude = @(),
    [String]$MessageCall = 'throw new|: base\(|Console\.Write|MessageBox\.Show|AppendTag\(|NewError\(|NewSpan\(|Debug\.Fail\(|WriteLog\(|OnWrite\(|XTrace\.|Log\.(Write|Error|Info|Debug|Warn|Fatal)|(Info|Error|Warn|Debug|Fatal)Prefixed\(|WritePrefixed\(|L\.T\(|Localization\.Translate|ApiException\(|HttpException\(',
    [Switch]$List,
    [String]$Csv
)

$ErrorActionPreference = "Stop"

if (!$Dir -or $Dir.Count -eq 0) { $Dir = @("NewLife.Core", "NewLife.Core.SourceGenerator") }
$dirs = @()
foreach ($item in $Dir) {
    $path = if ([IO.Path]::IsPathRooted($item)) { $item } else { Join-Path $Root $item }
    if (Test-Path -LiteralPath $path) { $dirs += $path }
}
if ($dirs.Count -eq 0) { throw "未找到待检查目录：$($Dir -join ', ')" }

$hits = @()

foreach ($dir in $dirs) {
    foreach ($file in Get-ChildItem $dir -Recurse -Filter *.cs) {
        $rel = if ($file.FullName.StartsWith($Root, [StringComparison]::OrdinalIgnoreCase)) {
            $file.FullName.Substring($Root.Length).TrimStart("\", "/")
        }
        else { $file.FullName }

        if ($rel -match '\\(obj|bin)\\') { continue }
        $excluded = $false
        foreach ($pattern in $Exclude) {
            if ($rel -like $pattern -or $file.FullName -like $pattern) { $excluded = $true; break }
        }
        if ($excluded) { continue }

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
            if ($code -match $MessageCall -and $code -match "[\u4e00-\u9fa5]") {
                $hits += [pscustomobject]@{ File = $rel; Line = $i + 1; Text = $code }
            }
        }
    }
}

if ($Csv) {
    $hits | Export-Csv -LiteralPath $Csv -NoTypeInformation -Encoding UTF8
    Write-Host "已导出 $($hits.Count) 条中文运行时消息到 $Csv"
    exit 0
}

if ($List) {
    Write-Host "盘点模式：发现 $($hits.Count) 条中文运行时消息" -ForegroundColor Yellow
    foreach ($hit in $hits) {
        Write-Host ("  {0}:{1}: {2}" -f $hit.File, $hit.Line, $hit.Text)
    }
    exit 0
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
