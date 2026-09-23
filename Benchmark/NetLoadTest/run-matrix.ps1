# 裸 Socket 层性能矩阵跑批脚本
# 按矩阵顺序启动独立服务端进程 + 客户端，采集吞吐/延迟/分配，输出 results.md 与 results.json
#
# 用法：
#   pwsh -File run-matrix.ps1                 # 正式跑全矩阵（窗口 10s）
#   pwsh -File run-matrix.ps1 -Quick          # 冒烟（窗口 3s）
#   pwsh -File run-matrix.ps1 -SkipBuild      # 跳过编译
#   pwsh -File run-matrix.ps1 -Only "tcp-1KB" # 只跑名称匹配的场景（正则）
param(
    [switch]$Quick,
    [switch]$SkipBuild,
    [string]$OutDir = "",
    [string]$Only = ""
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$proj = Join-Path $root "Benchmark\NetLoadTest\NetLoadTest.csproj"
$exe = Join-Path $root "Benchmark\NetLoadTest\bin\Release\net10.0\NetLoadTest.exe"

$seconds = if ($Quick) { 3 } else { 10 }
$warmup = if ($Quick) { 1 } else { 2 }

if (-not $SkipBuild) {
    Write-Host "== 编译 Release =="
    & dotnet build $proj -c Release -v q --nologo | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "构建失败" }
}

if (-not $OutDir) { $OutDir = Join-Path $root ("Benchmark\NetLoadTest\results\" + (Get-Date -Format "yyyyMMdd-HHmmss")) }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
Write-Host "== 输出目录：$OutDir（窗口 ${seconds}s，预热 ${warmup}s）=="

$script:results = New-Object System.Collections.Generic.List[object]
$script:port = 7900
$script:count = 0

function Invoke-Scenario {
    param(
        [string]$Name,
        [string]$Mode,
        [string]$Proto,
        [int]$Size,
        [int]$Clients,
        [int]$Frame = 0,
        [string]$RecvMode = "sync",
        [switch]$UdpConnect
    )
    if ($Only -and ($Name -notmatch $Only)) { return }
    $script:count++
    $script:port++
    $p = $script:port
    $isUdp = $Proto -eq "udp"
    $isOneway = $Mode -eq "oneway"
    $srvLog = Join-Path $OutDir "$Name.server.log"
    $cliLog = Join-Path $OutDir "$Name.client.log"

    Write-Host ("[{0}] {1}（{2} {3}B {4}C{5}{6}{7}）..." -f $script:count, $Name, $Proto, $Size, $Clients, $(if ($Frame -gt 0) { " frame=$Frame" } else { "" }), $(if ($RecvMode -ne "sync") { " recv=$RecvMode" } else { "" }), $(if ($UdpConnect) { " udp-conn" } else { "" }))

    # 服务端：oneway 场景不回显（只计数）；echo/roundtrip 场景回显
    # 端口偶发占用（前序进程残留/TIME_WAIT）时换端口重试
    $srv = $null
    $ready = $false
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        $srvArgs = @("--server", "--port", "$p", "--size", "$Size", "--warmup", "$warmup")
        if ($isUdp) { $srvArgs += "--udp" }
        if ($isOneway) { $srvArgs += "--oneway" }
        if ($Frame -gt 0) { $srvArgs += @("--frame", "$Frame") }
        $srv = Start-Process $exe -ArgumentList $srvArgs -RedirectStandardOutput $srvLog -PassThru -NoNewWindow

        for ($i = 0; $i -lt 100; $i++) {
            Start-Sleep -Milliseconds 100
            if ((Test-Path $srvLog) -and (Select-String -Path $srvLog -Pattern "READY" -Quiet)) { $ready = $true; break }
        }
        if ($ready) { break }

        $srv.Kill()
        $script:port++
        $p = $script:port
        Write-Warning "$Name 服务端未就绪（端口占用？），换端口重试"
    }
    if (-not $ready) {
        Write-Warning "$Name 重试仍失败，跳过"
        return
    }

    # 客户端
    $cliArgs = @("--mode", "$Mode", "--remote", "127.0.0.1:$p", "--clients", "$Clients", "--size", "$Size", "--seconds", "$seconds", "--warmup", "$warmup")
    if ($isUdp) { $cliArgs += "--udp" }
    if ($isOneway) { $cliArgs += "--oneway" }
    if ($Frame -gt 0) { $cliArgs += @("--frame", "$Frame") }
    if ($RecvMode -ne "sync") { $cliArgs += @("--recvmode", "$RecvMode") }
    if ($UdpConnect) { $cliArgs += "--udpconnect" }
    & $exe @cliArgs 2>&1 | Out-File $cliLog -Encoding UTF8

    # 等服务端自动退出（静默 3 秒 + 余量），超时强杀
    if (-not $srv.WaitForExit(90000)) {
        $srv.Kill()
        Write-Warning "$Name 服务端超时强杀"
    }

    # 解析客户端 SUMMARY
    $summary = $null
    $sline = Select-String -Path $cliLog -Pattern "^SUMMARY:" | Select-Object -Last 1
    if ($sline) { $summary = $sline.Line.Substring(8).Trim() | ConvertFrom-Json }

    # 解析服务端 STEADY（稳态：中位数带宽 + Gbps + 包率 + 帧率 + 服务端分配 B/msg + GC）
    $steady = $null; $gbps = $null; $pktps = $null; $framesPerSec = $null; $allocServer = $null; $gcText = ""
    $tline = Select-String -Path $srvLog -Pattern "STEADY (\d+[\d,\.]*) MB/s" | Select-Object -Last 1
    if ($tline) {
        $steadLine = $tline.Line
        $steady = [double]([regex]::Match($steadLine, "STEADY ([\d,\.]+) MB/s").Groups[1].Value -replace ",", "")
        $m = [regex]::Match($steadLine, "Gbps=([\d,\.]+)"); if ($m.Success) { $gbps = [double]($m.Groups[1].Value -replace ",", "") }
        $m = [regex]::Match($steadLine, "pkt=([\d,]+) pkt/s"); if ($m.Success) { $pktps = [double]($m.Groups[1].Value -replace ",", "") }
        $m = [regex]::Match($steadLine, "frame=([\d,]+) frame/s"); if ($m.Success) { $framesPerSec = [double]($m.Groups[1].Value -replace ",", "") }
        $m = [regex]::Match($steadLine, "alloc=([\d,\.]+) B/msg"); if ($m.Success) { $allocServer = [double]($m.Groups[1].Value -replace ",", "") }
        $m = [regex]::Match($steadLine, "gc=(\d+)/(\d+)/(\d+)"); if ($m.Success) { $gcText = "$($m.Groups[1].Value)/$($m.Groups[2].Value)/$($m.Groups[3].Value)" }
    }

    $row = [ordered]@{
        name         = $Name
        mode         = $Mode
        proto        = $Proto
        size         = $Size
        clients      = $Clients
        frame        = $Frame
        recvmode     = $RecvMode
        udpconnect   = $UdpConnect.IsPresent
        serverMBps   = $steady
        gbps         = $gbps
        pktps        = $pktps
        framesPerSec = $framesPerSec
        allocServer  = $allocServer
        gc           = $gcText
    }
    if ($summary) {
        # UDP 超大包会被程序钳制到协议上限（65507），以实际执行值为准
        if ($summary.size) { $row["size"] = $summary.size }
        if ($Mode -eq "roundtrip") {
            $row["samples"] = $summary.latencyCount
            $row["p50"] = $summary.p50
            $row["p95"] = $summary.p95
            $row["p99"] = $summary.p99
            $row["max"] = $summary.max
            $row["allocPerMsg"] = $summary.allocPerMsg
            $row["roundtripsPerSec"] = if ($summary.elapsed -gt 0) { [Math]::Round($summary.latencyCount / $summary.elapsed, 0) } else { 0 }
        }
        else {
            $row["msgsPerSec"] = $summary.msgsPerSec
            $row["mbps"] = $summary.mbps
            $row["framesPerSec"] = $summary.framesPerSec
            $row["lossPct"] = $summary.lossPct
            $row["integrityDiff"] = $summary.integrityDiff
            $row["allocPerMsg"] = $summary.allocPerMsg
        }
        # UDP 单向：服务端稳态速率与客户端发送速率之差 = 丢包率
        if ($isUdp -and $isOneway -and $steady -and $summary.sentBytes -gt 0 -and $summary.elapsed -gt 0) {
            $sendRate = $summary.sentBytes / $summary.elapsed
            $recvRate = $steady * 1048576
            $loss = [Math]::Round((1 - $recvRate / $sendRate) * 100, 1)
            if ($loss -lt 0) { $loss = 0 }
            $row["lossPct"] = $loss
        }
    }
    $script:results.Add([pscustomobject]$row)
    Write-Host ("    -> server={0} Gbps  client={1}" -f $(if ($gbps) { $gbps } else { "?" }), $(if ($summary.mbps) { "$([Math]::Round($summary.mbps,1)) MiB/s" } elseif ($summary.p50) { "P50=$($summary.p50)us" } else { "?" }))
}

# ===== 吞吐：尺寸曲线（单向；TCP 8 客户端 / UDP 4 客户端） =====
Invoke-Scenario -Name "tcp-32B"      -Mode oneway -Proto tcp -Size 32       -Clients 8
Invoke-Scenario -Name "tcp-1KB"      -Mode oneway -Proto tcp -Size 1024     -Clients 8
Invoke-Scenario -Name "tcp-64KB"     -Mode oneway -Proto tcp -Size 65536    -Clients 8
Invoke-Scenario -Name "tcp-1MB"      -Mode oneway -Proto tcp -Size 1048576  -Clients 8
Invoke-Scenario -Name "tcp-1KB-f24"  -Mode oneway -Proto tcp -Size 1024     -Clients 8 -Frame 24
Invoke-Scenario -Name "tcp-64KB-f24" -Mode oneway -Proto tcp -Size 65536    -Clients 8 -Frame 24
Invoke-Scenario -Name "udp-32B"      -Mode oneway -Proto udp -Size 32       -Clients 4
Invoke-Scenario -Name "udp-256B"     -Mode oneway -Proto udp -Size 256      -Clients 4
Invoke-Scenario -Name "udp-1KB"      -Mode oneway -Proto udp -Size 1024     -Clients 4
Invoke-Scenario -Name "udp-4KB"      -Mode oneway -Proto udp -Size 4096     -Clients 4
Invoke-Scenario -Name "udp-16KB"     -Mode oneway -Proto udp -Size 16384    -Clients 4
Invoke-Scenario -Name "udp-64KB"     -Mode oneway -Proto udp -Size 65536    -Clients 4
Invoke-Scenario -Name "udp-1KB-f24"  -Mode oneway -Proto udp -Size 1024     -Clients 4 -Frame 24
Invoke-Scenario -Name "udp-64KB-f24" -Mode oneway -Proto udp -Size 65536    -Clients 4 -Frame 24

# ===== 吞吐：并发扫描（单向） =====
Invoke-Scenario -Name "tcp-1KB-c1"   -Mode oneway -Proto tcp -Size 1024  -Clients 1
Invoke-Scenario -Name "tcp-1KB-c4"   -Mode oneway -Proto tcp -Size 1024  -Clients 4
Invoke-Scenario -Name "tcp-1KB-c16"  -Mode oneway -Proto tcp -Size 1024  -Clients 16
Invoke-Scenario -Name "tcp-1KB-c32"  -Mode oneway -Proto tcp -Size 1024  -Clients 32
Invoke-Scenario -Name "tcp-1KB-c64"  -Mode oneway -Proto tcp -Size 1024  -Clients 64
Invoke-Scenario -Name "tcp-64KB-c1"  -Mode oneway -Proto tcp -Size 65536 -Clients 1
Invoke-Scenario -Name "tcp-64KB-c4"  -Mode oneway -Proto tcp -Size 65536 -Clients 4
Invoke-Scenario -Name "tcp-64KB-c16" -Mode oneway -Proto tcp -Size 65536 -Clients 16
Invoke-Scenario -Name "tcp-64KB-c32" -Mode oneway -Proto tcp -Size 65536 -Clients 32
Invoke-Scenario -Name "udp-1KB-c1"   -Mode oneway -Proto udp -Size 1024  -Clients 1
Invoke-Scenario -Name "udp-1KB-c2"   -Mode oneway -Proto udp -Size 1024  -Clients 2
Invoke-Scenario -Name "udp-1KB-c8"   -Mode oneway -Proto udp -Size 1024  -Clients 8
Invoke-Scenario -Name "udp-1KB-c16"  -Mode oneway -Proto udp -Size 1024  -Clients 16
Invoke-Scenario -Name "udp-1KB-c32"  -Mode oneway -Proto udp -Size 1024  -Clients 32
Invoke-Scenario -Name "udp-1KB-c64"  -Mode oneway -Proto udp -Size 1024  -Clients 64
Invoke-Scenario -Name "udp-64KB-c1"  -Mode oneway -Proto udp -Size 65536 -Clients 1
Invoke-Scenario -Name "udp-64KB-c2"  -Mode oneway -Proto udp -Size 65536 -Clients 2
Invoke-Scenario -Name "udp-64KB-c8"  -Mode oneway -Proto udp -Size 65536 -Clients 8
Invoke-Scenario -Name "udp-64KB-c16" -Mode oneway -Proto udp -Size 65536 -Clients 16
Invoke-Scenario -Name "udp-64KB-c32" -Mode oneway -Proto udp -Size 65536 -Clients 32
Invoke-Scenario -Name "udp-16KB-c8"  -Mode oneway -Proto udp -Size 16384 -Clients 8

# ===== 吞吐：UDP Connect 化对照（客户端实验，验证零分配与包率） =====
Invoke-Scenario -Name "udp-1KB-conn"  -Mode oneway -Proto udp -Size 1024  -Clients 4 -UdpConnect
Invoke-Scenario -Name "udp-64KB-conn" -Mode oneway -Proto udp -Size 65536 -Clients 4 -UdpConnect

# ===== 回显流水线（分离进程；TCP 8 客户端 / UDP 4 客户端） =====
Invoke-Scenario -Name "tcp-1KB-pipe-sync"      -Mode pipeline -Proto tcp -Size 1024  -Clients 8 -RecvMode sync
Invoke-Scenario -Name "tcp-1KB-pipe-asyncpull" -Mode pipeline -Proto tcp -Size 1024  -Clients 8 -RecvMode asyncpull
Invoke-Scenario -Name "tcp-1KB-pipe-event"     -Mode pipeline -Proto tcp -Size 1024  -Clients 8 -RecvMode event
Invoke-Scenario -Name "tcp-64KB-pipe-sync"     -Mode pipeline -Proto tcp -Size 65536 -Clients 8 -RecvMode sync
Invoke-Scenario -Name "tcp-64KB-pipe-asyncpull" -Mode pipeline -Proto tcp -Size 65536 -Clients 8 -RecvMode asyncpull
Invoke-Scenario -Name "tcp-64KB-pipe-event"    -Mode pipeline -Proto tcp -Size 65536 -Clients 8 -RecvMode event

# ===== 往返延迟（分离进程） =====
Invoke-Scenario -Name "tcp-32B-rt-sync-4C"    -Mode roundtrip -Proto tcp -Size 32      -Clients 4
Invoke-Scenario -Name "tcp-1KB-rt-sync-4C"    -Mode roundtrip -Proto tcp -Size 1024    -Clients 4
Invoke-Scenario -Name "tcp-64KB-rt-sync-4C"   -Mode roundtrip -Proto tcp -Size 65536   -Clients 4
Invoke-Scenario -Name "tcp-1MB-rt-sync-4C"    -Mode roundtrip -Proto tcp -Size 1048576 -Clients 4
Invoke-Scenario -Name "tcp-1KB-rt-sync-64C"   -Mode roundtrip -Proto tcp -Size 1024    -Clients 64
Invoke-Scenario -Name "tcp-64KB-rt-sync-64C"  -Mode roundtrip -Proto tcp -Size 65536   -Clients 64
Invoke-Scenario -Name "tcp-1KB-rt-async-4C"   -Mode roundtrip -Proto tcp -Size 1024    -Clients 4  -RecvMode asyncpull
Invoke-Scenario -Name "tcp-1KB-rt-async-64C"  -Mode roundtrip -Proto tcp -Size 1024    -Clients 64 -RecvMode asyncpull
Invoke-Scenario -Name "tcp-64KB-rt-async-4C"  -Mode roundtrip -Proto tcp -Size 65536   -Clients 4  -RecvMode asyncpull
Invoke-Scenario -Name "tcp-64KB-rt-async-64C" -Mode roundtrip -Proto tcp -Size 65536   -Clients 64 -RecvMode asyncpull
Invoke-Scenario -Name "tcp-1KB-rt-event-4C"   -Mode roundtrip -Proto tcp -Size 1024    -Clients 4  -RecvMode event
Invoke-Scenario -Name "tcp-1KB-rt-event-64C"  -Mode roundtrip -Proto tcp -Size 1024    -Clients 64 -RecvMode event
Invoke-Scenario -Name "tcp-64KB-rt-event-4C"  -Mode roundtrip -Proto tcp -Size 65536   -Clients 4  -RecvMode event
Invoke-Scenario -Name "tcp-64KB-rt-event-64C" -Mode roundtrip -Proto tcp -Size 65536   -Clients 64 -RecvMode event
Invoke-Scenario -Name "udp-32B-rt-sync-1C"    -Mode roundtrip -Proto udp -Size 32      -Clients 1
Invoke-Scenario -Name "udp-1KB-rt-sync-1C"    -Mode roundtrip -Proto udp -Size 1024    -Clients 1
Invoke-Scenario -Name "udp-64KB-rt-sync-1C"   -Mode roundtrip -Proto udp -Size 65536   -Clients 1
Invoke-Scenario -Name "udp-1KB-rt-event-1C"   -Mode roundtrip -Proto udp -Size 1024    -Clients 1 -RecvMode event

# ===== 汇总输出 =====
$jsonPath = Join-Path $OutDir "results.json"
$script:results | ConvertTo-Json -Depth 5 | Out-File $jsonPath -Encoding UTF8

$md = New-Object System.Collections.Generic.List[string]
$md.Add("# 裸 Socket 层跑批结果")
$md.Add("")
$md.Add("- 时间：$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')　窗口 ${seconds}s　预热 ${warmup}s　机器：[本机]")
$md.Add("- 服务端=异步接收环（SAEA）；客户端接收模式 sync=同步拉取 / asyncpull=异步拉取 / event=事件接收")
$md.Add("")

$md.Add("## 一、吞吐（单向，服务端只计数不回发；均为服务端稳态口径）")
$md.Add("")
$md.Add("| 场景 | 协议 | 包大小 | 并发 | 连接 | 服务端带宽 | 包率 | 帧率 | 丢包率 | 服务端分配 B/msg | GC 0/1/2 |")
$md.Add("|---|---|---:|---:|---|---:|---:|---:|---:|---:|---:|")
foreach ($r in $script:results | Where-Object { $_.mode -eq "oneway" }) {
    $bw = if ($r.gbps -ne $null) { "$($r.gbps) Gbps" } else { "" }
    $pr = if ($r.pktps -ne $null) { if ($r.pktps -ge 1e6) { "{0:N2} Mpps" -f ($r.pktps / 1e6) } else { "{0:N1} kpps" -f ($r.pktps / 1e3) } } else { "" }
    $fr = if ($r.framesPerSec -ne $null) { "{0:N2} M帧/s" -f ($r.framesPerSec / 1e6) } else { "" }
    $loss = if ($r.lossPct -ne $null) { "$($r.lossPct)%" } elseif ($r.proto -eq "tcp") { "0" } else { "" }
    $conn = if ($r.udpconnect) { "已连接" } else { "" }
    $md.Add("| $($r.name) | $($r.proto) | $($r.size) | $($r.clients) | $conn | $bw | $pr | $fr | $loss | $($r.allocServer) | $($r.gc) |")
}
$md.Add("")

$md.Add("## 二、回显流水线（客户端接收方式对照；分配为服务端口径）")
$md.Add("")
$md.Add("| 场景 | 协议 | 包大小 | 并发 | 接收模式 | 回显 msg/s | 带宽 | 服务端分配 B/msg | 完整性差 |")
$md.Add("|---|---|---:|---:|---|---:|---:|---:|---:|")
foreach ($r in $script:results | Where-Object { $_.mode -eq "pipeline" }) {
    $bw = if ($r.mbps -ne $null) { "{0:N2} Gbps" -f ($r.mbps * 0.008388608) } else { "" }
    $md.Add("| $($r.name) | $($r.proto) | $($r.size) | $($r.clients) | $($r.recvmode) | $($r.msgsPerSec) | $bw | $($r.allocServer) | $($r.integrityDiff) |")
}
$md.Add("")

$md.Add("## 三、往返延迟（客户端观察 RTT；分配为服务端口径）")
$md.Add("")
$md.Add("| 场景 | 协议 | 包大小 | 并发 | 接收模式 | 样本数 | P50 µs | P95 µs | P99 µs | max µs | 服务端分配 B/msg |")
$md.Add("|---|---|---:|---:|---|---:|---:|---:|---:|---:|---:|")
foreach ($r in $script:results | Where-Object { $_.mode -eq "roundtrip" }) {
    $md.Add("| $($r.name) | $($r.proto) | $($r.size) | $($r.clients) | $($r.recvmode) | $($r.samples) | $($r.p50) | $($r.p95) | $($r.p99) | $($r.max) | $($r.allocServer) |")
}
$md.Add("")

$mdPath = Join-Path $OutDir "results.md"
$md | Out-File $mdPath -Encoding UTF8

Write-Host ""
Write-Host "== 完成：$($script:results.Count) 个场景 =="
Write-Host "results.md  : $mdPath"
Write-Host "results.json: $jsonPath"
