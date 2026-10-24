using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NewLife;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Extension;

/// <summary>ProcessHelper 进程查找/命令行 相关测试</summary>
public class ProcessHelperTests
{
    /// <summary>当前进程命令行解析</summary>
    [Fact]
    public void GetCommandLine_CurrentProcess()
    {
        var p = Process.GetCurrentProcess();

        var raw = ProcessHelper.GetCommandLine(p.Id);
        if (raw == null && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS 无 /proc 等价实现，产品读不到其它进程的命令行，环境缺失时跳过并留痕
            XTrace.WriteLine("macOS 未提供进程命令行，跳过断言");

            return;
        }
        Assert.NotNull(raw);
        Assert.Contains(p.ProcessName, raw!, System.StringComparison.OrdinalIgnoreCase);

        var args = ProcessHelper.GetCommandLineArgs(p.Id);
        // Windows 失败时可能返回空数组；Linux 可能为 null（读取异常）
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) Assert.NotNull(args);
        if (args != null && args.Length > 0)
        {
            // 第 0 个通常是可执行文件或解释器
            Assert.False(String.IsNullOrEmpty(args[0]));
        }
    }

    /// <summary>非托管宿主进程名称不做二级解析</summary>
    [Fact]
    public void GetProcessName_NormalProcess()
    {
        // 选择一个简单短命令：Windows 用 cmd /c timeout；Linux 用 sleep
        Process? child = null;
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                child = Process.Start(new ProcessStartInfo("cmd", "/c timeout /t 3 >nul") { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden });
            else
                child = Process.Start("sleep", "3");

            Assert.NotNull(child);
            // 等待进入运行状态
            Thread.Sleep(200);

            var name1 = child!.ProcessName;
            var name2 = child.GetProcessName();
            Assert.Equal(name1, name2); // 非 dotnet/java 不应被改变
        }
        finally
        {
            try { child?.Kill(); } catch { }
        }
    }

    /// <summary>dotnet 宿主二级进程名推断（若存在可用 dotnet 进程）</summary>
    [Fact]
    public void GetProcessName_DotnetHost()
    {
        // 寻找一个正在运行的 dotnet 宿主进程（排除当前测试进程自身）
        var currentId = Process.GetCurrentProcess().Id;
        Process? target = null;
        foreach (var p in Process.GetProcesses())
        {
            if (p.Id == currentId) continue;
            var pn = p.ProcessName;
            if (pn == "dotnet")
            {
                target = p;
                break;
            }
        }

        if (target == null)
        {
            XTrace.WriteLine("未找到正在运行的 dotnet 宿主进程，跳过验证（测试通过）");
            return; // 条件跳过（早退）
        }

        var args = ProcessHelper.GetCommandLineArgs(target.Id);
        if (args == null || args.Length < 2 || !args[0].Contains("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            XTrace.WriteLine("找到 dotnet 进程但命令行参数不足以验证逻辑，跳过（测试通过）");
            return;
        }

        var expected = Path.GetFileNameWithoutExtension(args[1]);
        var actual = target.GetProcessName();

        Assert.Equal(expected, actual);
    }

    #region ResolveHostName 单元测试（不依赖真实进程）

    [Fact]
    [DisplayName("ResolveHostName_dotnet_解析成功")]
    public void ResolveHostName_Dotnet()
    {
        var name = ProcessHelper.ResolveHostName("dotnet", ["dotnet", "/path/MyApp.dll", "--arg1", "val1"]);
        Assert.Equal("MyApp", name);

        // 仅宿主无参数，返回原始名
        name = ProcessHelper.ResolveHostName("dotnet", ["dotnet"]);
        Assert.Equal("dotnet", name);
    }

    [Fact]
    [DisplayName("ResolveHostName_java_解析成功")]
    public void ResolveHostName_Java()
    {
        var name = ProcessHelper.ResolveHostName("java", ["java", "-jar", "/path/app.jar", "--port", "8080"]);
        Assert.Equal("app", name);

        // 非 -jar 场景，返回原始名
        name = ProcessHelper.ResolveHostName("java", ["java", "-version"]);
        Assert.Equal("java", name);

        // -jar 出现在靠后位置
        name = ProcessHelper.ResolveHostName("java", ["java", "-Xmx2g", "-jar", "server.jar"]);
        Assert.Equal("server", name);
    }

    [Fact]
    [DisplayName("ResolveHostName_mono_解析成功")]
    public void ResolveHostName_Mono()
    {
        var name = ProcessHelper.ResolveHostName("mono", ["mono", "/path/App.exe"]);
        Assert.Equal("App", name);

        name = ProcessHelper.ResolveHostName("mono", ["/usr/bin/mono", "App.exe"]);
        Assert.Equal("App", name);
    }

    [Fact]
    [DisplayName("ResolveHostName_node_解析成功")]
    public void ResolveHostName_Node()
    {
        // 正常脚本
        var name = ProcessHelper.ResolveHostName("node", ["node", "/path/app.js", "--port", "3000"]);
        Assert.Equal("app", name);

        // 内联代码（-e），不应解析，返回原始名
        name = ProcessHelper.ResolveHostName("node", ["node", "-e", "console.log('hi')"]);
        Assert.Equal("node", name);

        // 仅宿主无参数
        name = ProcessHelper.ResolveHostName("node", ["node"]);
        Assert.Equal("node", name);

        // TypeScript 直接运行
        name = ProcessHelper.ResolveHostName("node", ["node", "server.mjs"]);
        Assert.Equal("server", name);
    }

    [Fact]
    [DisplayName("ResolveHostName_python_解析成功")]
    public void ResolveHostName_Python()
    {
        // 正常脚本
        var name = ProcessHelper.ResolveHostName("python", ["python", "/path/app.py"]);
        Assert.Equal("app", name);

        // python3
        name = ProcessHelper.ResolveHostName("python3", ["python3", "script.py"]);
        Assert.Equal("script", name);

        // 内联代码（-c），不应解析
        name = ProcessHelper.ResolveHostName("python", ["python", "-c", "print('hi')"]);
        Assert.Equal("python", name);

        // 模块执行（-m），不应解析
        name = ProcessHelper.ResolveHostName("python", ["python", "-m", "http.server", "8080"]);
        Assert.Equal("python", name);
    }

    [Fact]
    [DisplayName("ResolveHostName_php_解析成功")]
    public void ResolveHostName_Php()
    {
        var name = ProcessHelper.ResolveHostName("php", ["php", "/path/app.php"]);
        Assert.Equal("app", name);

        // 交互模式（-a），不应解析
        name = ProcessHelper.ResolveHostName("php", ["php", "-a"]);
        Assert.Equal("php", name);

        // 无参数
        name = ProcessHelper.ResolveHostName("php", ["php"]);
        Assert.Equal("php", name);
    }

    [Fact]
    [DisplayName("ResolveHostName_perl_解析成功")]
    public void ResolveHostName_Perl()
    {
        var name = ProcessHelper.ResolveHostName("perl", ["perl", "script.pl"]);
        Assert.Equal("script", name);

        // 内联代码（-e），不应解析
        name = ProcessHelper.ResolveHostName("perl", ["perl", "-e", "print 'hi'"]);
        Assert.Equal("perl", name);
    }

    [Fact]
    [DisplayName("ResolveHostName_ruby_解析成功")]
    public void ResolveHostName_Ruby()
    {
        var name = ProcessHelper.ResolveHostName("ruby", ["ruby", "app.rb"]);
        Assert.Equal("app", name);

        // 内联代码（-e），不应解析
        name = ProcessHelper.ResolveHostName("ruby", ["ruby", "-e", "puts 'hi'"]);
        Assert.Equal("ruby", name);
    }

    [Fact]
    [DisplayName("ResolveHostName_lua_解析成功")]
    public void ResolveHostName_Lua()
    {
        var name = ProcessHelper.ResolveHostName("lua", ["lua", "script.lua"]);
        Assert.Equal("script", name);

        // 交互模式（-i），不应解析
        name = ProcessHelper.ResolveHostName("lua", ["lua", "-i"]);
        Assert.Equal("lua", name);
    }

    [Fact]
    [DisplayName("ResolveHostName_deno_解析成功")]
    public void ResolveHostName_Deno()
    {
        // run 子命令
        var name = ProcessHelper.ResolveHostName("deno", ["deno", "run", "/path/app.ts", "--allow-net"]);
        Assert.Equal("app", name);

        // 直接执行（无子命令）
        name = ProcessHelper.ResolveHostName("deno", ["deno", "app.ts"]);
        Assert.Equal("app", name);

        // serve 子命令
        name = ProcessHelper.ResolveHostName("deno", ["deno", "serve", "server.ts"]);
        Assert.Equal("server", name);

        // 内联代码（eval），不应解析
        name = ProcessHelper.ResolveHostName("deno", ["deno", "eval", "console.log('hi')"]);
        Assert.Equal("deno", name);
    }

    [Fact]
    [DisplayName("ResolveHostName_bun_解析成功")]
    public void ResolveHostName_Bun()
    {
        // run 子命令
        var name = ProcessHelper.ResolveHostName("bun", ["bun", "run", "app.ts"]);
        Assert.Equal("app", name);

        // 直接执行
        name = ProcessHelper.ResolveHostName("bun", ["bun", "server.js"]);
        Assert.Equal("server", name);

        // 内联代码
        name = ProcessHelper.ResolveHostName("bun", ["bun", "-e", "console.log('hi')"]);
        Assert.Equal("bun", name);
    }

    [Fact]
    [DisplayName("ResolveHostName_非宿主_返回原始名")]
    public void ResolveHostName_NotHost()
    {
        var name = ProcessHelper.ResolveHostName("notepad", ["notepad", "file.txt"]);
        Assert.Equal("notepad", name);

        name = ProcessHelper.ResolveHostName("chrome", ["chrome", "--headless"]);
        Assert.Equal("chrome", name);
    }

    [Fact]
    [DisplayName("ResolveHostName_空参数_返回原始名")]
    public void ResolveHostName_NullArgs()
    {
        var name = ProcessHelper.ResolveHostName("dotnet", null);
        Assert.Equal("dotnet", name);

        name = ProcessHelper.ResolveHostName("node", []);
        Assert.Equal("node", name);
    }

    #endregion

    /// <summary>GetHasExited 已退出进程返回 true</summary>
    [Fact]
    public void GetHasExited_ExitedProcess()
    {
        Process? p;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            p = Process.Start(new ProcessStartInfo("cmd", "/c exit 0") { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden });
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            p = Process.Start("/usr/bin/true");   // macOS 无 /bin/true
        else
            p = Process.Start("/bin/true");

        Assert.NotNull(p);
        p!.WaitForExit();

        // 已退出
        Assert.True(p.GetHasExited());

        // 再调用一次，确保不会抛异常
        var again = p.GetHasExited();
        Assert.True(again);
    }

    /// <summary>returnError 在无标准输出时返回错误输出</summary>
    [Fact]
    public void Execute_ReturnError()
    {
        // 等待时间放宽到 30 秒：满载并行跑测时进程创建与退出本身可能停滞数秒（实测 5 秒偶发超时返回 null）。
        // 本用例验证的是 returnError 语义，超时值不是被测对象
        String? rs;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            rs = "cmd".Execute("/c echo err_out 1>&2", 30_000, true);
        else
            rs = "sh".Execute("-c \"echo err_out 1>&2\"", 30_000, true);

        Assert.NotNull(rs);
        Assert.Contains("err_out", rs);
    }

    #region 进程管控（ForceKill/SafetyKill/ShellExecute/RunNew）

    /// <summary>启动长命子进程。Windows 用 cmd+ping 回环（不访问外部网络），Unix 用 sleep</summary>
    private static Process StartLongProcess()
    {
        ProcessStartInfo psi;
        if (Runtime.Windows)
            psi = new ProcessStartInfo("cmd", "/c ping -n 30 127.0.0.1 >nul") { CreateNoWindow = true, UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden };
        else
            psi = new ProcessStartInfo("sleep", "30") { CreateNoWindow = true, UseShellExecute = false };

        var p = Process.Start(psi);
        Assert.NotNull(p);

        // 等待子进程真正启动并进入运行状态
        Thread.Sleep(200);
        Assert.False(p!.GetHasExited());

        return p;
    }

    [Fact]
    [DisplayName("ForceKill_长命子进程_立即终止且重复调用不抛")]
    public void ForceKill_LongRunning()
    {
        var p = StartLongProcess();
        try
        {
            var rs = p.ForceKill();
            Assert.Same(p, rs);
            Assert.True(p.WaitForExit(15_000), "ForceKill 后 15 秒内子进程未退出");

            // 已退出进程再次强杀：幂等，不抛异常
            var again = p.ForceKill();
            Assert.Same(p, again);
            Assert.True(p.GetHasExited());
        }
        finally
        {
            try { p.ForceKill(); } catch { }
            p.Dispose();
        }
    }

    [Fact]
    [DisplayName("SafetyKill_长命子进程_按平台语义渐进终止")]
    public void SafetyKill_LongRunning()
    {
        var p = StartLongProcess();
        try
        {
            var rs = p.SafetyKill(2_000, 20, 100);
            Assert.Same(p, rs);

            if (Runtime.Linux)
            {
                // Linux：kill 发送 SIGTERM，sleep 会立即退出
                Assert.True(p.WaitForExit(15_000), "SafetyKill 后 15 秒内子进程未退出");
            }
            else if (Runtime.Windows)
            {
                // Windows：taskkill 不带 /f，对 CreateNoWindow 启动的无窗口控制台子进程实测无法终止（taskkill 退出码 1），
                // 实现本身不保证退出，故只断言返回同一进程且不抛异常，由 finally 的 ForceKill 兜底清理
                XTrace.WriteLine("Windows 下 SafetyKill 不保证终止无窗口控制台子进程，跳过退出断言");
            }
            else
            {
                // macOS 等平台无信号分支，SafetyKill 为空操作
                XTrace.WriteLine("当前平台无 SafetyKill 信号实现，跳过退出断言");
            }
        }
        finally
        {
            try { p.ForceKill(); } catch { }
            p.Dispose();
        }
    }

    [Fact]
    [DisplayName("SafetyKill_ForceKill_已退出进程_返回自身且不抛")]
    public void Kill_ExitedProcess_Idempotent()
    {
        Process? p = null;
        try
        {
            if (Runtime.Windows)
                p = Process.Start(new ProcessStartInfo("cmd", "/c exit 0") { CreateNoWindow = true, UseShellExecute = false });
            else if (Runtime.OSX)
                p = Process.Start("/usr/bin/true");
            else
                p = Process.Start("/bin/true");

            Assert.NotNull(p);
            Assert.True(p!.WaitForExit(15_000));
            Assert.True(p.GetHasExited());

            Assert.Same(p, p.SafetyKill());
            Assert.Same(p, p.ForceKill());
            Assert.True(p.GetHasExited());
        }
        finally
        {
            try { p?.ForceKill(); } catch { }
            p?.Dispose();
        }
    }

    [Fact]
    [DisplayName("ShellExecute_短命令_返回已启动进程并退出")]
    public void ShellExecute_ShortCommand()
    {
        Process? p = null;
        try
        {
            if (Runtime.Windows)
                p = "cmd".ShellExecute("/c exit 0");
            else
                p = "sh".ShellExecute("-c \"exit 0\"");

            Assert.NotNull(p);
            Assert.True(p!.WaitForExit(15_000), "ShellExecute 启动的进程未在 15 秒内退出");
        }
        finally
        {
            try { p?.ForceKill(); } catch { }
            p?.Dispose();
        }
    }

    [Fact]
    [DisplayName("RunNew_短命令_捕获输出与退出码")]
    public void RunNew_CaptureOutputAndExitCode()
    {
        var lines = new ConcurrentQueue<String?>();
        var code = Runtime.Windows
            ? "cmd".RunNew("/c echo hello_runnew", 15_000, line => lines.Enqueue(line))
            : "sh".RunNew("-c \"echo hello_runnew\"", 15_000, line => lines.Enqueue(line));

        Assert.Equal(0, code);

        // RunNew 采用异步输出事件，返回时事件可能尚未派发完毕，短暂等待后断言
        var end = Runtime.TickCount64 + 5_000;
        while (lines.IsEmpty && Runtime.TickCount64 < end) Thread.Sleep(50);

        Assert.Contains(lines, e => e != null && e.Contains("hello_runnew"));
    }

    [Fact]
    [DisplayName("RunNew_非零退出码_原样返回")]
    public void RunNew_NonZeroExitCode()
    {
        var code = Runtime.Windows
            ? "cmd".RunNew("/c exit 3", 15_000)
            : "sh".RunNew("-c \"exit 3\"", 15_000);

        Assert.Equal(3, code);
    }

    [Fact]
    [DisplayName("RunNew_不等待_返回负一")]
    public void RunNew_NoWait()
    {
        var code = Runtime.Windows
            ? "cmd".RunNew("/c exit 0", 0)
            : "sh".RunNew("-c \"exit 0\"", 0);

        Assert.Equal(-1, code);
    }

    #endregion
}