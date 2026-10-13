# 全量单元测试并行跑测脚本
# 构建一次后，把全部测试宿主（4 个工程 × 目标框架，共 10 个）并行跑完，输出汇总表。
# Windows 上 testhost 偶发跑完不退出，脚本带超时兜底（按 PID 递归终止进程树）。
# 仅支持 Windows（Compat-net462 宿主需要 .NET Framework）；Windows PowerShell 5.1 与 PowerShell 7（pwsh）均可运行。
#
# 用法：
#   pwsh -File run-tests.ps1                      # 构建 Release 后并行跑全部宿主
#   pwsh -File run-tests.ps1 -Configuration Debug # 指定构建配置
#   pwsh -File run-tests.ps1 -SkipBuild           # 跳过构建（要求各宿主产物已存在）
#   pwsh -File run-tests.ps1 -Only "Core-*"       # 只跑名称匹配的宿主（正则）
#   pwsh -File run-tests.ps1 -TimeoutSec 1200     # 单宿主超时秒数（默认 900）
#   pwsh -File run-tests.ps1 -ThrottleLimit 5     # 限制同时运行的宿主数（默认 0=不限）
#   pwsh -File run-tests.ps1 -Repeat 10           # 稳定性验收：连续跑 10 轮，任一轮失败即退出码非零
#   pwsh -File run-tests.ps1 -List                # 列出全部宿主后退出
#
# 结果：TestResults\<时间戳>\<宿主>.trx（用例明细，可与 CI 矩阵产物对照）+ <宿主>.log（控制台输出）
param(
    [ValidateSet("Debug", "Release")]
    [String]$Configuration = "Release",
    [Switch]$SkipBuild,
    [String]$Only = "",
    [Int32]$TimeoutSec = 900,
    [Int32]$ThrottleLimit = 0,
    [Int32]$Repeat = 1,
    [Switch]$List
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$root = $PSScriptRoot

# 宿主表：名称与 .github/workflows/test.yml 的矩阵项一致，便于与 CI 结果对照
$hosts = @(
    [pscustomobject]@{ Name = "Core-net8.0";          Project = "XUnitTest.Core\XUnitTest.Core.csproj";                       Framework = "net8.0";          OutDir = "Bin\UnitTest\net8.0" }
    [pscustomobject]@{ Name = "Core-net8.0-windows";  Project = "XUnitTest.Core\XUnitTest.Core.csproj";                       Framework = "net8.0-windows";  OutDir = "Bin\UnitTest\net8.0-windows" }
    [pscustomobject]@{ Name = "Core-net9.0";          Project = "XUnitTest.Core\XUnitTest.Core.csproj";                       Framework = "net9.0";          OutDir = "Bin\UnitTest\net9.0" }
    [pscustomobject]@{ Name = "Core-net10.0";         Project = "XUnitTest.Core\XUnitTest.Core.csproj";                       Framework = "net10.0";         OutDir = "Bin\UnitTest\net10.0" }
    [pscustomobject]@{ Name = "Core-net10.0-windows"; Project = "XUnitTest.Core\XUnitTest.Core.csproj";                       Framework = "net10.0-windows"; OutDir = "Bin\UnitTest\net10.0-windows" }
    [pscustomobject]@{ Name = "Net6-net6.0";          Project = "XUnitTest.Net6\XUnitTest.Net6.csproj";                       Framework = "net6.0";          OutDir = "Bin\UnitTest6\net6.0" }
    [pscustomobject]@{ Name = "Net6-net6.0-windows";  Project = "XUnitTest.Net6\XUnitTest.Net6.csproj";                       Framework = "net6.0-windows";  OutDir = "Bin\UnitTest6\net6.0-windows" }
    [pscustomobject]@{ Name = "Compat-net462";        Project = "XUnitTest.Compat\XUnitTest.Compat.csproj";                   Framework = "net462";          OutDir = "Bin\UnitTestCompat\net462" }
    [pscustomobject]@{ Name = "Compat-net8.0";        Project = "XUnitTest.Compat\XUnitTest.Compat.csproj";                   Framework = "net8.0";          OutDir = "Bin\UnitTestCompat\net8.0" }
    [pscustomobject]@{ Name = "SourceGenerator";      Project = "XUnitTest.SourceGenerator\XUnitTest.SourceGenerator.csproj"; Framework = "net10.0";         OutDir = "Bin\UnitTestSG\net10.0" }
)

if ($List)
{
    $hosts | Select-Object Name, Framework, OutDir | Format-Table -AutoSize
    return
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw "未找到 dotnet 命令" }
if ($env:OS -ne "Windows_NT") { Write-Warning "当前非 Windows 环境，Compat-net462 宿主不可用，可用 -Only 过滤排除" }

if ($Only)
{
    $hosts = @($hosts | Where-Object { $_.Name -match $Only })
    if ($hosts.Count -eq 0) { throw "-Only '$Only' 未匹配到宿主，可用 -List 查看宿主名" }
}

# 阶段一：构建一次（逐工程；工程内多 TFM 由 MSBuild 并行）。
# 不做并发构建：同工程多 TFM 并发 restore/build 会竞争 obj 下的共享中间文件。
if (-not $SkipBuild)
{
    Write-Host "== 构建（$Configuration）=="
    foreach ($project in @($hosts | Select-Object -ExpandProperty Project -Unique))
    {
        Write-Host "-> $project"
        & dotnet build (Join-Path $root $project) -c $Configuration -m --nologo -v q
        if ($LASTEXITCODE -ne 0) { throw "构建失败（退出码 $LASTEXITCODE）：$project" }
    }
}
else
{
    foreach ($h in $hosts)
    {
        if (-not (Test-Path (Join-Path $root $h.OutDir))) { throw "-SkipBuild 但产物目录不存在：$($h.OutDir)，请先去掉 -SkipBuild 构建一次" }
    }
}

# 递归终止进程树：宿主进程为 cmd → dotnet test → testhost（.dll/.exe），必须沿子进程递归清理
function Stop-ProcessTree([Int32]$ProcessId)
{
    foreach ($child in @(Get-CimInstance Win32_Process -Filter "ParentProcessId=$ProcessId" -ErrorAction SilentlyContinue))
    {
        Stop-ProcessTree $child.ProcessId
    }
    Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
}

function Start-HostRun($h)
{
    $log = Join-Path $resultDir "$($h.Name).log"

    # 经 cmd 把输出重定向到文件（.NET 启动方式没有“重定向到文件”能力，用管道又有缓冲区死锁风险），
    # 并用 [Process]::Start 启动：Windows PowerShell 5.1 的 Start-Process -PassThru 取不到 ExitCode，
    # 会导致按退出码判定宿主成败失效。chcp 65001 让子进程按 UTF-8 输出，日志中文可读。
    $inner = 'chcp 65001>nul & dotnet test "' + (Join-Path $root $h.Project) + '" -f "' + $h.Framework + '" -c "' + $Configuration + '"' +
    ' --no-build --no-restore --nologo --logger "trx;LogFileName=' + $h.Name + '.trx" --results-directory "' + $resultDir + '"' +
    ' 1> "' + $log + '" 2> "' + $log + '.err"'

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = if ($env:ComSpec) { $env:ComSpec } else { 'cmd.exe' }
    $psi.Arguments = '/d /s /c "' + $inner + '"'
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.WorkingDirectory = $root
    $proc = [System.Diagnostics.Process]::Start($psi)

    [pscustomobject]@{
        Host      = $h
        Process   = $proc
        Log       = $log
        StartTime = Get-Date
        Done      = $false
        ExitCode  = $null
        TimedOut  = $false
        Duration  = [TimeSpan]::Zero
    }
}

# 解析 dotnet test 写入的 trx 结果；缺文件（如超时被杀）返回 $null
function Get-TrxCounters([String]$Path)
{
    if (-not (Test-Path $Path)) { return $null }
    try
    {
        $xml = New-Object System.Xml.XmlDocument
        $xml.Load($Path)
        $c = $xml.TestRun.ResultSummary.Counters
        if (-not $c) { return $null }
        # xunit 跳过用例记为 outcome=NotExecuted，且不填 Counters@notExecuted，需按结果节点单独统计
        $skipped = 0
        foreach ($r in $xml.GetElementsByTagName("UnitTestResult"))
        {
            if ($r.outcome -eq "NotExecuted") { $skipped++ }
        }
        [pscustomobject]@{
            Total   = [Int32]$c.total
            Passed  = [Int32]$c.passed
            Failed  = [Int32]$c.failed
            Skipped = $skipped
        }
    }
    catch { $null }
}

# 单轮跑测：并行执行全部宿主，返回失败宿主数。每轮生成独立的结果目录
function Invoke-TestRound([Int32]$RoundIndex)
{
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    if ($Repeat -gt 1) { $stamp += "-r$RoundIndex" }
    $resultDir = Join-Path $root "TestResults\$stamp"
    New-Item -ItemType Directory -Force -Path $resultDir | Out-Null

# 阶段二：并行跑测（窗口调度：默认全部并发，-ThrottleLimit 限流）
$limit = if ($ThrottleLimit -gt 0) { [Math]::Min($ThrottleLimit, $hosts.Count) } else { $hosts.Count }
Write-Host "== 并行跑测（宿主 $($hosts.Count) 个，并发上限 $limit，单宿主超时 ${TimeoutSec}s）=="
Write-Host "结果目录：$resultDir"

$scriptStart = Get-Date
$runs = New-Object System.Collections.Generic.List[object]
$pending = New-Object System.Collections.Queue
foreach ($h in $hosts) { $pending.Enqueue($h) }

while ($pending.Count -gt 0 -or @($runs | Where-Object { -not $_.Done }).Count -gt 0)
{
    while ($pending.Count -gt 0 -and @($runs | Where-Object { -not $_.Done }).Count -lt $limit)
    {
        $h = $pending.Dequeue()
        $run = Start-HostRun $h
        $runs.Add($run)
        Write-Host ("  启动 {0}（{1}，PID {2}）" -f $h.Name, $h.Framework, $run.Process.Id)
    }

    $waiting = $false
    foreach ($run in @($runs | Where-Object { -not $_.Done }))
    {
        if ($run.Process.HasExited)
        {
            $run.Duration = (Get-Date) - $run.StartTime
            $run.ExitCode = $run.Process.ExitCode
            $run.Done = $true
            $run.Process.Dispose()
            $status = if ($run.ExitCode -eq 0) { "PASS" } else { "FAIL" }
            Write-Host ("  [{0}] {1}，用时 {2:N1}s" -f $status, $run.Host.Name, $run.Duration.TotalSeconds)
        }
        elseif (((Get-Date) - $run.StartTime).TotalSeconds -gt $TimeoutSec)
        {
            Write-Warning ("  [{0}] 超时（{1}s），终止进程树（PID {2}）" -f $run.Host.Name, $TimeoutSec, $run.Process.Id)
            Stop-ProcessTree $run.Process.Id
            $run.Duration = (Get-Date) - $run.StartTime
            $run.TimedOut = $true
            $run.Done = $true
            try { $run.Process.Dispose() } catch { }
        }
        else { $waiting = $true }
    }

    if ($waiting) { Start-Sleep -Seconds 2 }
}

# 汇总
$wall = (Get-Date) - $scriptStart
Write-Host ""
Write-Host ("== 汇总（{0}，总墙钟 {1:N1}s）==" -f $Configuration, $wall.TotalSeconds)

$rows = New-Object System.Collections.Generic.List[object]
$failed = 0
foreach ($run in $runs)
{
    $verdict = if ($run.TimedOut) { "TIMEOUT" } elseif ($run.ExitCode -ne 0) { "FAIL" } else { "PASS" }
    if ($verdict -ne "PASS") { $failed++ }

    $total = "-"; $passed = "-"; $trxFailed = "-"; $skipped = "-"
    $counters = Get-TrxCounters (Join-Path $resultDir "$($run.Host.Name).trx")
    if ($counters)
    {
        $total = $counters.Total; $passed = $counters.Passed; $trxFailed = $counters.Failed; $skipped = $counters.Skipped
    }

    $rows.Add([pscustomobject]@{
        Host    = $run.Host.Name
        Result  = $verdict
        Total   = $total
        Passed  = $passed
        Failed  = $trxFailed
        Skipped = $skipped
        Seconds = [Math]::Round($run.Duration.TotalSeconds, 1)
    })
}
$rows | Format-Table -AutoSize | Out-Host

foreach ($run in $runs)
{
    $verdict = if ($run.TimedOut) { "TIMEOUT" } elseif ($run.ExitCode -ne 0) { "FAIL" } else { "PASS" }
    if ($verdict -ne "PASS")
    {
        Write-Host ""
        Write-Host ("---- {0}（{1}）控制台日志尾部：{2} ----" -f $run.Host.Name, $verdict, $run.Log)
        Get-Content $run.Log -Tail 30 -ErrorAction SilentlyContinue | ForEach-Object { Write-Host $_ }
    }
}

    if ($failed -gt 0)
    {
        Write-Warning "有 $failed 个宿主未通过，trx 与日志见 $resultDir"
    }
    else
    {
        Write-Host "全部 $($runs.Count) 个宿主通过，trx 与日志见 $resultDir"
    }

    return $failed
}

# 阶段三：轮次驱动。Repeat>1 为稳定性验收模式：任一轮出现失败，整体退出码非零
$roundsFailed = 0
for ($round = 1; $round -le $Repeat; $round++)
{
    if ($Repeat -gt 1) { Write-Host ""; Write-Host "######## 第 $round/$Repeat 轮 ########" }

    $roundFailed = Invoke-TestRound $round
    if ($roundFailed -gt 0) { $roundsFailed++ }
}

if ($Repeat -gt 1)
{
    Write-Host ""
    if ($roundsFailed -eq 0)
    {
        Write-Host ("== 稳定性验收通过：{0} 轮全部零失败 ==" -f $Repeat)
    }
    else
    {
        Write-Warning ("== 稳定性验收失败：{0} 轮中 {1} 轮出现失败 ==" -f $Repeat, $roundsFailed)
    }
}

if ($roundsFailed -gt 0) { exit 1 }
exit 0
