# 进程扩展 ProcessHelper

## 概述

`ProcessHelper` 是 NewLife.Core 中的进程管理工具类，提供进程查找、进程控制、命令行执行等功能。支持 Windows 和 Linux 双平台，能够正确识别 dotnet/java 宿主进程的真实名称。

**命名空间**：`NewLife`  
**文档地址**：https://newlifex.com/core/process_helper

## 核心特性

- **跨平台支持**：同时支持 Windows 和 Linux 系统
- **宿主进程识别**：正确识别 dotnet/java 托管进程的真实程序名
- **安全进程控制**：提供温和退出和强制终止两种方式
- **命令行执行**：支持同步/异步执行、输出捕获、超时控制

## 快速开始

```csharp
using NewLife;
using System.Diagnostics;

// 获取进程真实名称（支持 dotnet/java 宿主）
var process = Process.GetCurrentProcess();
var name = process.GetProcessName();

// 执行命令并获取输出
var output = "ipconfig".Execute("/all");

// 以隐藏窗口执行命令
"notepad.exe".Run("test.txt", 0);

// 安全退出进程
process.SafetyKill();

// 强制终止进程树
process.ForceKill();
```

## API 参考

### 进程查找

#### GetProcessName

```csharp
public static String GetProcessName(this Process process)
```

获取进程的逻辑名称。对于 dotnet/java 宿主进程，返回真正的目标程序集/入口 Jar 名称（不含扩展名）。

**应用场景**：
- 进程监控和管理
- 服务发现
- 日志记录中识别真实应用名

**示例**：
```csharp
// 普通进程
var notepad = Process.GetProcessesByName("notepad")[0];
notepad.GetProcessName()         // "notepad"

// dotnet 宿主进程（运行 MyApp.dll）
// 命令行：dotnet /path/to/MyApp.dll --arg1 value1
var dotnetProcess = ...;
dotnetProcess.GetProcessName()   // "MyApp"

// java 宿主进程（运行 app.jar）
// 命令行：java -jar /path/to/app.jar
var javaProcess = ...;
javaProcess.GetProcessName()     // "app"
```

#### GetCommandLine

```csharp
public static String? GetCommandLine(Int32 processId)
```

获取指定进程的完整命令行字符串。

**平台实现**：
- **Linux**：读取 `/proc/{pid}/cmdline` 文件
- **Windows**：通过 `NtQueryInformationProcess` 读取进程 PEB

**示例**：
```csharp
var cmdLine = ProcessHelper.GetCommandLine(1234);
// Windows: "C:\Program Files\dotnet\dotnet.exe" MyApp.dll --env Production
// Linux: /usr/bin/dotnet MyApp.dll --env Production
```

#### GetCommandLineArgs

```csharp
public static String[]? GetCommandLineArgs(Int32 processId)
```

获取指定进程的命令行参数数组。

**示例**：
```csharp
var args = ProcessHelper.GetCommandLineArgs(1234);
// ["dotnet", "MyApp.dll", "--env", "Production"]
```

### 进程控制

#### SafetyKill

```csharp
public static Process? SafetyKill(this Process process, Int32 msWait = 5_000, Int32 times = 50, Int32 interval = 200)
```

安全退出进程（温和方式）。发送正常终止信号，让进程有机会执行清理代码。

**参数说明**：
- `msWait`：发送信号后的初始等待时间，默认 5000 毫秒
- `times`：轮询检测次数，默认 50 次
- `interval`：轮询间隔，默认 200 毫秒

**平台实现**：
- **Linux**：发送 `kill` 信号（默认 SIGTERM）
- **Windows**：执行 `taskkill -pid {id}`

**示例**：
```csharp
var process = Process.Start("MyApp.exe");

// 温和关闭，等待最多 10 秒
process.SafetyKill(msWait: 10_000);

// 检查是否成功退出
if (!process.GetHasExited())
{
    // 进程未在规定时间内退出，可能需要强制终止
    process.ForceKill();
}
```

#### ForceKill

```csharp
public static Process? ForceKill(this Process process, Int32 msWait = 5_000)
```

强制终止进程树，包括所有子进程。

**平台实现**：
- **Linux**：发送 `kill -9` 信号（SIGKILL）
- **Windows**：执行 `taskkill /t /f /pid {id}`
- **.NET Core 3.0+**：使用 `Process.Kill(true)` 终止进程树

**示例**：
```csharp
// 强制终止进程及其所有子进程
process.ForceKill();

// 或者指定更长的等待时间
process.ForceKill(msWait: 10_000);
```

#### GetHasExited

```csharp
public static Boolean GetHasExited(this Process process)
```

安全获取进程是否已终止。当进程句柄不可访问时返回 `true`（视为已退出）。

**示例**：
```csharp
if (process.GetHasExited())
{
    Console.WriteLine("进程已退出");
}
```

### 命令行执行

#### Run

```csharp
public static Int32 Run(
    this String cmd, 
    String? arguments = null, 
    Int32 msWait = 0, 
    Action<String?>? output = null, 
    Action<Process>? onExit = null, 
    String? working = null)
```

以隐藏窗口执行命令行。

**参数说明**：
- `cmd`：可执行文件名或路径
- `arguments`：命令行参数
- `msWait`：等待时间（0=不等待，<0=无限等待，>0=最长等待毫秒数）
- `output`：输出回调委托（需 msWait > 0）
- `onExit`：进程退出回调
- `working`：工作目录

**返回值**：进程退出代码；未等待或超时返回 -1

**示例**：
```csharp
// 不等待，后台执行
"notepad.exe".Run("test.txt");

// 等待执行完成，获取退出码
var exitCode = "ping".Run("localhost -n 4", 30_000);

// 捕获输出
var output = new StringBuilder();
"ipconfig".Run("/all", 5_000, line => output.AppendLine(line));
Console.WriteLine(output.ToString());

// 带工作目录
"npm".Run("install", 60_000, working: @"C:\Projects\MyApp");

// 进程退出时回调
"MyApp.exe".Run(onExit: p => Console.WriteLine($"退出码: {p.ExitCode}"));
```

#### ShellExecute

```csharp
public static Process ShellExecute(
    this String fileName, 
    String? arguments = null, 
    String? workingDirectory = null)
```

在 Shell 上执行命令。目标进程不是当前进程的子进程，不会随本进程退出。

**应用场景**：
- 打开文件（使用系统默认程序）
- 打开 URL（使用默认浏览器）
- 启动独立应用程序

**示例**：
```csharp
// 用默认程序打开文件
"document.pdf".ShellExecute();

// 用默认浏览器打开网址
"https://newlifex.com".ShellExecute();

// 以管理员身份运行
// 注意：需要在 ProcessStartInfo 中设置 Verb = "runas"
"cmd.exe".ShellExecute("/k echo Hello");

// 指定工作目录
"MyApp.exe".ShellExecute("--config app.json", @"C:\Apps");
```

#### Execute

```csharp
public static String? Execute(
    this String cmd, 
    String? arguments = null, 
    Int32 msWait = 0, 
    Boolean returnError = false)
```

执行命令并返回标准输出内容。

**参数说明**：
- `msWait`：等待时间（0=阻塞直到退出，>0=超时后强杀）
- `returnError`：无标准输出时是否返回错误输出

**示例**：
```csharp
// 获取 IP 配置
var ipConfig = "ipconfig".Execute("/all");

// 获取 Git 版本
var gitVersion = "git".Execute("--version");

// 执行 Linux 命令
var diskUsage = "df".Execute("-h");

// 超时控制
var result = "ping".Execute("localhost", 5_000);

// 失败时返回错误信息
var output = "invalid_cmd".Execute(returnError: true);
```

## 使用场景

### 1. 服务管理

```csharp
public class ServiceManager
{
    public void StopService(String serviceName)
    {
        var processes = Process.GetProcesses()
            .Where(p => p.GetProcessName().EqualIgnoreCase(serviceName));
        
        foreach (var process in processes)
        {
            // 先尝试温和关闭
            process.SafetyKill(msWait: 10_000);
            
            // 如果还没退出，强制终止
            if (!process.GetHasExited())
            {
                process.ForceKill();
            }
        }
    }
}
```

### 2. 脚本执行

```csharp
public class ScriptRunner
{
    public String RunPowerShell(String script)
    {
        return "powershell".Execute($"-ExecutionPolicy Bypass -Command \"{script}\"", 60_000);
    }
    
    public String RunBash(String script)
    {
        return "bash".Execute($"-c \"{script}\"", 60_000);
    }
}
```

### 3. 进程监控

```csharp
public class ProcessMonitor
{
    public void Monitor(String appName)
    {
        while (true)
        {
            var processes = Process.GetProcesses()
                .Where(p => p.GetProcessName().EqualIgnoreCase(appName))
                .ToList();
            
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {appName} 进程数: {processes.Count}");
            
            foreach (var p in processes)
            {
                var cmdLine = ProcessHelper.GetCommandLine(p.Id);
                Console.WriteLine($"  PID={p.Id}, CommandLine={cmdLine}");
            }
            
            Thread.Sleep(5000);
        }
    }
}
```

## 最佳实践

### 1. 优雅关闭优先

```csharp
// 推荐：先温和，再强制
public void StopProcess(Process process)
{
    // 温和关闭，给进程清理的机会
    process.SafetyKill(msWait: 5_000);
    
    // 如果还没退出，强制终止
    if (!process.GetHasExited())
    {
        process.ForceKill();
    }
}
```

### 2. 合理设置超时

```csharp
// 根据命令特点设置超时
var quickResult = "echo".Execute("Hello", msWait: 1_000);      // 快速命令
var longResult = "npm".Execute("install", msWait: 300_000);   // 耗时操作
```

### 3. 处理输出回调

```csharp
// 实时处理大量输出
var lines = new List<String>();
"find".Run("/", 60_000, line =>
{
    if (!line.IsNullOrEmpty())
    {
        lines.Add(line);
        if (lines.Count % 1000 == 0)
            Console.WriteLine($"已处理 {lines.Count} 行");
    }
});
```

## 平台差异

| 功能 | Windows | Linux |
|------|---------|-------|
| GetCommandLine | NtQueryInformationProcess | /proc/{pid}/cmdline |
| SafetyKill | taskkill | kill (SIGTERM) |
| ForceKill | taskkill /f /t | kill -9 (SIGKILL) |
| ShellExecute | Shell 执行 | 需要设置 UseShellExecute=true |

## 注意事项

1. **权限要求**：部分操作可能需要管理员/root 权限
2. **进程树终止**：`ForceKill` 会终止所有子进程，请谨慎使用
3. **命令行长度**：Windows 命令行长度限制约 8191 字符
4. **编码问题**：执行命令时注意输出编码，可通过 `Encoding` 参数指定

## 相关链接

- [运行时信息 Runtime](runtime-运行时信息Runtime.md)
- [轻量级应用主机 Host](host-轻量级应用主机Host.md)
- [服务管理 NewLife.Agent](https://newlifex.com/core/agent)
