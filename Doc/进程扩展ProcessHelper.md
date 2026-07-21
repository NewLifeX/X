# 进程扩展 ProcessHelper

## 概述

`ProcessHelper` 是 NewLife.Core 中的进程管理工具类，提供进程查找、进程控制、命令执行等功能。支持 Windows 和 Linux 双平台，能够准确识别 dotnet/java/node/python/deno/bun 等宿主进程的真实应用名称。

**命名空间**：`NewLife`  
**文档地址**：https://newlifex.com/core/process_helper

## 功能特点

- **跨平台支持**：同时支持 Windows 和 Linux 系统
- **托管进程识别**：准确识别 dotnet/java/node/python/deno/bun 等托管进程的实际应用名称
- **安全进程控制**：提供温和退出与强制终止两种方式
- **命令执行**：支持同步/异步执行、超时控制、输出回调

## 快速开始

```csharp
using NewLife;
using System.Diagnostics;

// 获取进程真实名称，支持 dotnet/java/node/python 等宿主
var process = Process.GetCurrentProcess();
var name = process.GetProcessName();

// 执行命令并获取输出
var output = "ipconfig".Execute("/all");

// 以隐藏窗口执行命令
"notepad.exe".Run("test.txt", 0);

// 安全退出进程
process.SafetyKill();

// 强制终止进程
process.ForceKill();
```

## API 参考

### 进程查找

#### GetProcessName

```csharp
public static String GetProcessName(this Process process)
```

获取进程的逻辑名称。对于 dotnet/java/node/python/deno/bun 等宿主进程，返回其真实目标程序集/入口脚本名称（不含扩展名）。

**适用场景**：
- 进程监控和管理
- 任务管理器
- 日志记录中标识真实应用

**示例**：
```csharp
// 普通进程
var notepad = Process.GetProcessesByName("notepad")[0];
notepad.GetProcessName()         // "notepad"

// dotnet 宿主进程，运行 MyApp.dll
// 命令行：dotnet /path/to/MyApp.dll --arg1 value1
var dotnetProcess = ...;
dotnetProcess.GetProcessName()   // "MyApp"

// java 宿主进程，运行 app.jar
// 命令行：java -jar /path/to/app.jar
var javaProcess = ...;
javaProcess.GetProcessName()     // "app"

// node 宿主进程，运行 server.js
// 命令行：node /path/to/server.js
var nodeProcess = ...;
nodeProcess.GetProcessName()     // "server"

// deno 宿主进程，运行 app.ts
// 命令行：deno run /path/to/app.ts
var denoProcess = ...;
denoProcess.GetProcessName()     // "app"

// python 宿主进程，运行 script.py
// 命令行：python /path/to/script.py
var pythonProcess = ...;
pythonProcess.GetProcessName()   // "script"
```

#### GetCommandLine

```csharp
public static String? GetCommandLine(Int32 processId)
```

获取指定进程的命令行原始字符串。

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

安全退出进程，温和方式。目标进程收到正常终止信号，有机会执行清理代码。

**参数说明**：
- `msWait`：发送信号后的初始等待时间，默认 5000 毫秒
- `times`：轮询检测次数，默认 50 次
- `interval`：轮询间隔毫秒，默认 200 毫秒

**平台实现**：
- **Linux**：发送 `kill` 信号，默认 SIGTERM
- **Windows**：执行 `taskkill -pid {id}`

**示例**：
```csharp
var process = Process.Start("MyApp.exe");

// 温和关闭，等待最长 10 秒
process.SafetyKill(msWait: 10_000);

// 检查是否成功退出
if (!process.GetHasExited())
{
    // 仍未在规定时间内退出，需要强制终止
    process.ForceKill();
}
```

#### ForceKill

```csharp
public static Process? ForceKill(this Process process, Int32 msWait = 5_000)
```

强制终止进程树，包含其全部子进程。

**平台实现**：
- **Linux**：发送 `kill -9` 信号（SIGKILL）
- **Windows**：执行 `taskkill /t /f /pid {id}`
- **.NET Core 3.0+**：使用 `Process.Kill(true)` 终止子树

**示例**：
```csharp
// 强制终止进程及其子进程
process.ForceKill();

// 指定更长的等待时间
process.ForceKill(msWait: 10_000);
```

#### GetHasExited

```csharp
public static Boolean GetHasExited(this Process process)
```

安全获取进程是否已终止。进程句柄不可访问时返回 `true`，视为已退出。

**示例**：
```csharp
if (process.GetHasExited())
{
    Console.WriteLine("进程已退出");
}
```

### 命令执行

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
- `cmd`：可执行文件路径
- `arguments`：命令行参数
- `msWait`：等待时间（0=不等待，<0=无限等待，>0=最长等待毫秒数）
- `output`：输出回调委托（需 msWait > 0）
- `onExit`：进程退出回调

**示例**：
```csharp
// 不等待退出
"notepad.exe".Run("test.txt", 0);

// 等待完成并获取输出
var code = "ping".Run("127.0.0.1", 10_000, output: s => Console.WriteLine(s));

// 带退出回调
"dotnet".Run("MyApp.dll", -1, onExit: p => Console.WriteLine($"退出码：{p.ExitCode}"));
```

#### Execute

```csharp
public static String? Execute(this String cmd, String? arguments = null, Int32 msWait = 0, Boolean returnError = false)
```

执行命令并等待返回（读取标准输出）。

**示例**：
```csharp
// 简单执行
var ip = "ipconfig".Execute("/all");
Console.WriteLine(ip);

// 指定超时
var result = "ping".Execute("127.0.0.1", 5_000);
if (result == null) Console.WriteLine("超时");

// 错误输出回退
var output = "invalid_cmd".Execute("", 5_000, returnError: true);
```

#### ShellExecute

```csharp
public static Process ShellExecute(this String fileName, String? arguments = null, String? workingDirectory = null)
```

在 Shell 上执行命令。目标进程不是当前进程子进程，不会随本进程退出。

**示例**：
```csharp
// 打开文档
ProcessHelper.ShellExecute("document.pdf");

// 打开网址
ProcessHelper.ShellExecute("https://newlifex.com");
```

## 辅助方法

### StopService / StartService

停止/启动 Windows 服务。

```csharp
public static void StopService(String serviceName)
public static void StartService(String serviceName)
```

### RunAsync / RunAsync<T>

异步执行命令并捕获输出。

### 进程监控和管理

#### GetProcesses

```csharp
public static IEnumerable<Process> GetProcesses(String name)
```

根据名称模糊查找进程（支持宿主进程名匹配）。

```csharp
foreach (var p in ProcessHelper.GetProcesses("MyApp"))
{
    Console.WriteLine($"PID={p.Id}, Name={p.GetProcessName()}");
}
```

#### StopProcess

```csharp
public static Process[]? StopProcess(String processName, Int32 msWait = 5_000)
```

根据名称停止进程。

```csharp
ProcessHelper.StopProcess("notepad");
```

---

## 平台兼容性

| API | Windows | Linux |
|-----|---------|-------|
| GetProcessName | ✅ | ✅ |
| GetCommandLine | ✅ NtQueryInformationProcess | ✅ /proc/{pid}/cmdline |
| SafetyKill | ✅ taskkill | ✅ kill (SIGTERM) |
| ForceKill | ✅ taskkill /t /f | ✅ kill -9 (SIGKILL) |
| Run | ✅ | ✅ |
| Execute | ✅ | ✅ |
| ShellExecute | ✅ | ✅ |
