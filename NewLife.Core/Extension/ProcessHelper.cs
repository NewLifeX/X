using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using NewLife.Configuration;
using NewLife.Log;

namespace NewLife;

/// <summary>进程助手类</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/process_helper
/// </remarks>
public static class ProcessHelper
{
    #region 进程查找
    /// <summary>获取二级进程名。默认返回一层，如果是 dotnet/java 宿主，则返回其真正目标程序集/入口Jar 名称（不含扩展名）</summary>
    /// <param name="process">进程实例</param>
    /// <returns>进程逻辑名称</returns>
    public static String GetProcessName(this Process process)
    {
        // 原始进程名（宿主名）
        var pname = process.ProcessName;

        // dotnet / java 宿主进程需进一步解析真实入口
        // 常见场景：dotnet xxx.dll  或  java -jar app.jar
        var isManagedHost = pname == "dotnet" || "*/dotnet".IsMatch(pname) || pname == "java" || "*/java".IsMatch(pname);
        if (!isManagedHost) return pname;

        // 读取命令行参数。避免重复读取，统一处理
        var args = GetCommandLineArgs(process.Id);
        if (args == null || args.Length == 0) return pname;

        // 针对 dotnet：形如  [dotnet] [path/app.dll] [...]
        if ((pname == "dotnet" || "*/dotnet".IsMatch(pname)) && args.Length >= 2 && args[0].Contains("dotnet"))
        {
            // args[1] 可能是绝对或相对路径，截取文件名（不含扩展）
            return Path.GetFileNameWithoutExtension(args[1]);
        }
        // 针对 java：形如 [java] [-jar] [path/app.jar] [...]
        if ((pname == "java" || "*/java".IsMatch(pname)) && args.Length >= 3 && args[0].Contains("java"))
        {
            // 寻找 -jar 参数的下一个
            for (var i = 1; i < args.Length - 1; i++)
            {
                if (args[i] == "-jar") return Path.GetFileNameWithoutExtension(args[i + 1]);
            }
        }

        return pname;
    }

    /// <summary>获取二级进程名</summary>
    /// <param name="process"></param>
    /// <returns></returns>
    [Obsolete("=>GetProcessName", true)]
    public static String GetProcessName2(this Process process) => GetProcessName(process);

    ///// <summary>根据名称获取进程。支持dotnet/java</summary>
    ///// <param name="name"></param>
    ///// <returns></returns>
    //public static IEnumerable<Process> GetProcessByName(String name)
    //{
    //    // 跳过自己
    //    var sid = Process.GetCurrentProcess().Id;
    //    foreach (var p in Process.GetProcesses())
    //    {
    //        if (p.Id == sid) continue;

    //        var pname = p.ProcessName;
    //        if (pname == name)
    //            yield return p;
    //        else
    //        {
    //            if (GetProcessName2(p) == name) yield return p;
    //        }
    //    }
    //}

    /// <summary>获取指定进程的命令行（完整原始字符串）</summary>
    /// <param name="processId">进程Id</param>
    /// <returns>命令行原始字符串；失败返回 null</returns>
    public static String? GetCommandLine(Int32 processId)
    {
        if (Runtime.Linux)
        {
            try
            {
                var file = $"/proc/{processId}/cmdline";
                if (File.Exists(file))
                {
                    var lines = File.ReadAllText(file).Trim('\0', ' ').Split('\0');
                    return lines.Join(" ");
                }
            }
            catch { }
        }
        else if (Runtime.Windows)
        {
            return GetCommandLineOnWindows(processId);
        }

        return null;
    }

    /// <summary>获取指定进程的命令行参数数组</summary>
    /// <param name="processId">进程Id</param>
    /// <returns>参数数组；失败返回 null；Windows 失败时返回空数组以便后续逻辑更安全</returns>
    public static String[]? GetCommandLineArgs(Int32 processId)
    {
        if (Runtime.Linux)
        {
            try
            {
                var file = $"/proc/{processId}/cmdline";
                if (File.Exists(file))
                {
                    var lines = File.ReadAllText(file).Trim('\0', ' ').Split('\0');
                    //if (lines.Length > 1) return lines[1];
                    return lines;
                }
            }
            catch { }
        }
        else if (Runtime.Windows)
        {
            var str = GetCommandLineOnWindows(processId);
            if (str.IsNullOrEmpty()) return [];

            // 分割参数，特殊支持双引号
            return CommandParser.Split(str);
        }

        return null;
    }

    private static String? GetCommandLineOnWindows(Int32 processId)
    {
        var processHandle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, processId);
        if (processHandle == IntPtr.Zero) return null;

        try
        {
            var pbi = new PROCESS_BASIC_INFORMATION();
            var status = NtQueryInformationProcess(processHandle, 0, ref pbi, (UInt32)Marshal.SizeOf(pbi), out _);
            if (status != 0) return null;

            if (!ReadStruct<PEB>(processHandle, pbi.PebBaseAddress, out var peb)) return null;
            if (!ReadStruct<RtlUserProcessParameters>(processHandle, peb.ProcessParameters, out var upp)) return null;
            if (!ReadStringUni(processHandle, upp.CommandLine, out var commandLine)) return null;

            return commandLine?.TrimEnd('\0');
        }
        finally
        {
            CloseHandle(processHandle);
        }
    }

    private static Boolean ReadStruct<T>(IntPtr hProcess, IntPtr lpBaseAddress, out T val)
    {
        val = default!;
        var size = Marshal.SizeOf(typeof(T));
        var ptr = Marshal.AllocHGlobal(size);
        try
        {
            if (ReadProcessMemory(hProcess, lpBaseAddress, ptr, (UInt32)size, out var len) && len == size)
            {
                val = (T)Marshal.PtrToStructure(ptr, typeof(T))!;
                return true;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        return false;
    }

    private static Boolean ReadStringUni(IntPtr hProcess, UNICODE_STRING us, out String? val)
    {
        // 某些进程可能返回 0，或异常的超大值（损坏/竞争）；做基本防御
        val = default;
        var size = us.MaximumLength;
        if (size <= 0) return false;

        var ptr = Marshal.AllocHGlobal(size);
        try
        {
            if (ReadProcessMemory(hProcess, us.Buffer, ptr, size, out var len) && len == size)
            {
                val = Marshal.PtrToStringUni(ptr);
                return true;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        return false;
    }
    #endregion

    #region 进程控制
    /// <summary>安全退出进程（温和尝试）。目标进程收到正常终止信号/指令，有机会执行清理代码。</summary>
    /// <remarks>
    /// Linux: 发送 kill 信号（默认 SIGTERM）。
    /// Windows: taskkill 普通终止。
    /// 若在等待期内未退出，不再附加强杀（交由调用方二次处理或使用 ForceKill）。
    /// </remarks>
    /// <param name="process">目标进程</param>
    /// <param name="msWait">初次发送信号后等待的毫秒数。默认 5000</param>
    /// <param name="times">轮询检测次数</param>
    /// <param name="interval">轮询间隔毫秒</param>
    /// <returns>进程对象（便于链式）</returns>
    public static Process? SafetyKill(this Process process, Int32 msWait = 5_000, Int32 times = 50, Int32 interval = 200)
    {
        if (process == null || process.GetHasExited()) return process;

        var span = DefaultSpan.Current;
        //XTrace.WriteLine("安全，温柔一刀！PID={0}/{1}", process.Id, process.ProcessName);
        span?.AppendTag($"SafetyKill，温柔一刀！PID={process.Id}/{process.ProcessName}");

        // 杀进程，如果命令未成功则马上退出（后续强杀），否则循环检测并等待
        try
        {
            if (Runtime.Linux)
            {
                using var p = Process.Start("kill", process.Id.ToString());
                if (p != null && p.WaitForExit(msWait) && p.ExitCode != 0) return process;

                for (var i = 0; i < times && !process.GetHasExited(); i++)
                {
                    Thread.Sleep(interval);
                }
            }
            else if (Runtime.Windows)
            {
                using var p = Process.Start("taskkill", $"-pid {process.Id}");
                if (p != null && p.WaitForExit(msWait) && p.ExitCode != 0) return process;

                for (var i = 0; i < times && !process.GetHasExited(); i++)
                {
                    Thread.Sleep(interval);
                }
            }
        }
        catch (Exception ex)
        {
            span?.AppendTag(ex.Message);
        }

        //if (!process.GetHasExited()) process.Kill();

        return process;
    }

    /// <summary>强制结束进程树（大力出奇迹）。包含其全部子进程。</summary>
    /// <param name="process">目标进程</param>
    /// <param name="msWait">等待退出的时间。默认 5000 毫秒</param>
    /// <returns>进程对象（可能已退出）</returns>
    public static Process? ForceKill(this Process process, Int32 msWait = 5_000)
    {
        if (process == null || process.GetHasExited()) return process;

        var span = DefaultSpan.Current;
        //XTrace.WriteLine("强杀，大力出奇迹！PID={0}/{1}", process.Id, process.ProcessName);
        span?.AppendTag($"ForceKill，大力出奇迹！PID={process.Id}/{process.ProcessName}");

        // 终止指定的进程及启动的子进程,如nginx等
        // 在Core 3.0, Core 3.1, 5, 6, 7, 8, 9 中支持此重载
        // https://learn.microsoft.com/zh-cn/dotnet/api/system.diagnostics.process.kill?view=net-8.0#system-diagnostics-process-kill(system-boolean)
        try
        {
#if NETCOREAPP
            process.Kill(true);
#else
            process.Kill();
#endif
        }
        catch (Exception ex)
        {
            span?.AppendTag(ex.Message);
        }

        if (process.GetHasExited()) return process;

        try
        {
            if (Runtime.Linux)
            {
                //-9 SIGKILL 强制终止信号
                using var p = Process.Start("kill", $"-9 {process.Id}");
                p?.WaitForExit(msWait);
            }
            else if (Runtime.Windows)
            {
                // /f 指定强制终止进程，有子进程时只能强制
                // /t 终止指定的进程和由它启用的子进程 
                using var p = Process.Start("taskkill", $"/t /f /pid {process.Id}");
                p?.WaitForExit(msWait);
            }
        }
        catch (Exception ex)
        {
            span?.AppendTag(ex.Message);
        }

        // 兜底再来一次
        if (!process.GetHasExited())
        {
            try
            {
#if NETCOREAPP
                process.Kill(true);
#else
                process.Kill();
#endif
            }
            catch (Exception ex)
            {
                span?.AppendTag(ex.Message);
            }
        }

        return process;
    }

    /// <summary>获取进程是否终止。捕获进程句柄不可访问的异常并视为已退出。</summary>
    public static Boolean GetHasExited(this Process process)
    {
        try
        {
            return process.HasExited;
        }
        catch (Win32Exception) { return true; }
    }
    #endregion

    #region 执行命令行

    /// <summary>以隐藏窗口执行命令行（快速版）。不等待退出 msWait=0 返回 -1。</summary>
    /// <param name="cmd">文件名</param>
    /// <param name="arguments">命令参数</param>
    /// <param name="msWait">等待毫秒数。0 不等待；&lt;0 无限等待；&gt;0 最长等待指定毫秒</param>
    /// <param name="output">输出回调。指定 msWait &gt; 0 时才会异步捕获</param>
    /// <param name="onExit">进程退出回调</param>
    /// <param name="working">工作目录</param>
    /// <returns>退出代码；未等待或超时返回 -1</returns>
    public static Int32 Run(this String cmd, String? arguments = null, Int32 msWait = 0, Action<String?>? output = null, Action<Process>? onExit = null, String? working = null) => RunNew(cmd, arguments, msWait, output, Encoding.UTF8, onExit, working);

    /// <summary>以隐藏窗口执行命令行，支持指定输出编码与退出回调</summary>
    /// <param name="cmd">文件名</param>
    /// <param name="arguments">命令参数</param>
    /// <param name="msWait">等待毫秒数。0 不等待；&lt;0 无限等待；&gt;0 最长等待指定毫秒</param>
    /// <param name="output">进程输出回调</param>
    /// <param name="encoding">输出编码（默认 UTF8）</param>
    /// <param name="onExit">退出回调</param>
    /// <param name="working">工作目录</param>
    /// <returns>退出代码；未等待或超时返回 -1</returns>
    public static Int32 RunNew(this String cmd, String? arguments = null, Int32 msWait = 0, Action<String?>? output = null, Encoding? encoding = null, Action<Process>? onExit = null, String? working = null)
    {
        if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteLine("Run {0} {1} {2}", cmd, arguments, msWait);

        // 修正文件路径（保持原逻辑，不主动拼接 working，避免破坏既有行为）
        var fileName = cmd;
        //if (!Path.IsPathRooted(fileName) && !working.IsNullOrEmpty()) fileName = working.CombinePath(fileName);

        encoding ??= Encoding.UTF8;

        var p = new Process();
        var si = p.StartInfo;
        si.FileName = fileName;
        if (arguments != null) si.Arguments = arguments;
        si.WindowStyle = ProcessWindowStyle.Hidden;
        si.CreateNoWindow = true;
        if (!String.IsNullOrWhiteSpace(working)) si.WorkingDirectory = working;

        // 需要捕获输出时关闭 shell 执行并重定向。
        if (msWait > 0)
        {
            si.UseShellExecute = false;
            si.RedirectStandardOutput = true;
            si.RedirectStandardError = true;
            si.StandardOutputEncoding = encoding;
            si.StandardErrorEncoding = encoding;
            if (output != null)
            {
                p.OutputDataReceived += (s, e) => output(e.Data);
                p.ErrorDataReceived += (s, e) => output(e.Data);
            }
            else
            {
                p.OutputDataReceived += (s, e) => { if (e.Data != null) XTrace.WriteLine(e.Data); };
                p.ErrorDataReceived += (s, e) => { if (e.Data != null) XTrace.Log.Error(e.Data); };
            }
        }
        if (onExit != null)
        {
            p.EnableRaisingEvents = true; // 原代码未设置，导致 Exited 不触发
            p.Exited += (s, e) => { if (s is Process proc) onExit(proc); };
        }

        p.Start();
        if (msWait > 0)
        {
            // BeginRead 必须在 Start 后
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
        }

        if (msWait == 0) return -1; // 不等待，无法取得退出码

        // 负值：无限等待；正值：超时后强杀
        if (msWait < 0)
            p.WaitForExit();
        else if (!p.WaitForExit(msWait))
        {
#if NETCOREAPP
            p.Kill(true);
#else
            p.Kill();
#endif
            return -1;
        }

        return p.ExitCode;
    }

    /// <summary>在 Shell 上执行命令。目标进程不是当前进程子进程，不会随本进程退出。</summary>
    /// <param name="fileName">文件名</param>
    /// <param name="arguments">参数</param>
    /// <param name="workingDirectory">工作目录</param>
    /// <returns>启动的进程对象</returns>
    public static Process ShellExecute(this String fileName, String? arguments = null, String? workingDirectory = null)
    {
        if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteLine("ShellExecute {0} {1} {2}", fileName, arguments, workingDirectory);

        //// 修正文件路径
        //if (!Path.IsPathRooted(fileName) && !workingDirectory.IsNullOrEmpty()) fileName = workingDirectory.CombinePath(fileName);

        var p = new Process();
        var si = p.StartInfo;
        si.UseShellExecute = true;
        si.FileName = fileName;
        if (arguments != null) si.Arguments = arguments;
        if (workingDirectory != null) si.WorkingDirectory = workingDirectory;

        p.Start();

        return p;
    }

    /// <summary>执行命令并等待返回（读取标准输出）。</summary>
    /// <param name="cmd">命令</param>
    /// <param name="arguments">命令参数</param>
    /// <param name="msWait">等待退出的时间。默认 0 不等待（仍会在 ReadToEnd 阻塞到退出）</param>
    /// <param name="returnError">没有标准输出时，是否返回错误内容</param>
    /// <returns>标准输出或（可选）错误输出</returns>
    public static String? Execute(this String cmd, String? arguments = null, Int32 msWait = 0, Boolean returnError = false) => Execute(cmd, arguments, msWait, returnError, null);

    /// <summary>执行命令并等待返回</summary>
    /// <param name="cmd">命令</param>
    /// <param name="arguments">命令参数</param>
    /// <param name="msWait">等待退出时间；0 立即读取输出（阻塞到结束）；&gt;0 超时后强杀；&lt;0 无限等待</param>
    /// <param name="returnError">无标准输出时是否返回错误输出</param>
    /// <param name="outputEncoding">输出编码</param>
    /// <returns>输出字符串；失败/null</returns>
    public static String? Execute(this String cmd, String? arguments, Int32 msWait, Boolean returnError, Encoding? outputEncoding)
    {
        try
        {
            if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteLine("Execute {0} {1}", cmd, arguments);

            var psi = new ProcessStartInfo(cmd, arguments ?? String.Empty)
            {
                // UseShellExecute 必须 false，以便于后续重定向输出流
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                StandardOutputEncoding = outputEncoding,
                StandardErrorEncoding = outputEncoding,
            };
            var process = Process.Start(psi);
            if (process == null) return null;

            if (msWait > 0 && !process.WaitForExit(msWait))
            {
#if NETCOREAPP
                process.Kill(true);
#else
                process.Kill();
#endif
                return null;
            }

            var rs = process.StandardOutput.ReadToEnd();
            if (rs.IsNullOrEmpty() && returnError) rs = process.StandardError.ReadToEnd();

            return rs;
        }
        catch (Exception ex)
        {
            if (XTrace.Log.Level <= LogLevel.Debug) XTrace.Log.Error(ex.Message);
            return null;
        }
    }
    #endregion

    #region 原生方法
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(UInt32 processAccess, Boolean bInheritHandle, Int32 processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern Boolean CloseHandle(IntPtr hObject);

    [DllImport("ntdll.dll")]
    private static extern Int32 NtQueryInformationProcess(IntPtr processHandle, Int32 processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, UInt32 processInformationLength, out UInt32 returnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern Boolean ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] Byte[] lpBuffer, UInt32 size, out UInt32 lpNumberOfBytesRead);

    [DllImport("kernel32.dll")]
    private static extern Boolean ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, UInt32 nSize, out UInt32 lpNumberOfBytesRead);

    const UInt32 PROCESS_QUERY_INFORMATION = 0x0400;
    const UInt32 PROCESS_VM_READ = 0x0010;

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_BASIC_INFORMATION
    {
        public IntPtr Reserved1;
        public IntPtr PebBaseAddress;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public IntPtr[] Reserved2;
        public IntPtr UniqueProcessId;
        public IntPtr Reserved3;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct UNICODE_STRING
    {
        public UInt16 Length;
        public UInt16 MaximumLength;
        public IntPtr Buffer;
    }

    // This is not the real struct!
    // I faked it to get ProcessParameters address.
    // Actual struct definition:
    // https://learn.microsoft.com/en-us/windows/win32/api/winternl/ns-winternl-peb
    // 这里只保留需要的字段，获取 ProcessParameters 地址
    [StructLayout(LayoutKind.Sequential)]
    private struct PEB
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public IntPtr[] Reserved;
        public IntPtr ProcessParameters;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RtlUserProcessParameters
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public Byte[] Reserved1;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public IntPtr[] Reserved2;
        public UNICODE_STRING ImagePathName;
        public UNICODE_STRING CommandLine;
    }
    #endregion
}