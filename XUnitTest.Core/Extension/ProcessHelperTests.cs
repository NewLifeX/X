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
        Assert.NotNull(raw);
        Assert.Contains(p.ProcessName, raw!, System.StringComparison.OrdinalIgnoreCase);

        var args = ProcessHelper.GetCommandLineArgs(p.Id);
        // Windows 失败时可能返回空数组；Linux 可能为 null（读取异常）
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) Assert.NotNull(args);
        if (args != null && args.Length > 0)
        {
            // 第 0 个通常是可执行文件或解释器
            Assert.False(string.IsNullOrEmpty(args[0]));
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
            if (pn.Equals("dotnet", StringComparison.OrdinalIgnoreCase) || pn.EndsWith("/dotnet", StringComparison.OrdinalIgnoreCase))
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

    /// <summary>GetHasExited 已退出进程返回 true</summary>
    [Fact]
    public void GetHasExited_ExitedProcess()
    {
        Process? p;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            p = Process.Start(new ProcessStartInfo("cmd", "/c exit 0") { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden });
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
}