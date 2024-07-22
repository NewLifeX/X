using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using NewLife.Configuration;
using NewLife.Log;

namespace NewLife;

/// <summary>进程助手类</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/string_helper
/// </remarks>
public static class ProcessHelper
{
    #region 进程查找
    /// <summary>获取进程名。dotnet/java进程取文件名，Windows系统中比较耗时</summary>
    /// <param name="process"></param>
    /// <returns></returns>
    public static String GetProcessName(this Process process)
    {
        var name = process.ProcessName;

        //if (Runtime.Linux)
        //{
        //    try
        //    {
        //        var file = $"/proc/{process.Id}/cmdline";
        //        if (File.Exists(file))
        //        {
        //            var lines = File.ReadAllText(file).Trim('\0', ' ').Split('\0');
        //            if (lines.Length > 1) name = Path.GetFileNameWithoutExtension(lines[1]);
        //        }
        //    }
        //    catch { }
        //}
        //else if (Runtime.Windows)
        //{
        //    try
        //    {
        //        var dic = MachineInfo.ReadWmic("process where processId=" + process.Id, "commandline");
        //        if (dic.TryGetValue("commandline", out var str) && !str.IsNullOrEmpty())
        //        {
        //            var ss = str.Split(' ').Select(e => e.Trim('\"')).ToArray();
        //            str = ss.FirstOrDefault(e => e.EndsWithIgnoreCase(".dll", ".jar"));
        //            if (!str.IsNullOrEmpty()) name = Path.GetFileNameWithoutExtension(str);
        //        }
        //    }
        //    catch { }
        //}

        var args = GetCommandLineArgs(process.Id);
        if (args != null)
        {

        }

        return name;
    }

    /// <summary>获取二级进程名。默认一级，如果是dotnet/java则取二级</summary>
    /// <param name="process"></param>
    /// <returns></returns>
    public static String GetProcessName2(this Process process)
    {
        var pname = process.ProcessName;
        if (
           pname == "dotnet" || "*/dotnet".IsMatch(pname) ||
           pname == "java" || "*/java".IsMatch(pname))
        {
            return GetProcessName(process);
        }

        return pname;
    }

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

    /// <summary>获取指定进程的命令行参数</summary>
    /// <param name="processId"></param>
    /// <returns></returns>
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

    /// <summary>获取指定进程的命令行参数</summary>
    /// <param name="processId"></param>
    /// <returns></returns>
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
        if (processHandle == IntPtr.Zero)
            return null;

        try
        {
            var pbi = new PROCESS_BASIC_INFORMATION();
            var status = NtQueryInformationProcess(processHandle, 0, ref pbi, (UInt32)Marshal.SizeOf(pbi), out _);
            if (status != 0) return null;

            var rs = ReadStruct<PEB>(processHandle, pbi.PebBaseAddress, out var peb);
            if (!rs) return null;

            rs = ReadStruct<RtlUserProcessParameters>(processHandle, peb.ProcessParameters, out var upp);
            if (!rs) return null;

            rs = ReadStringUni(processHandle, upp.CommandLine, out var commandLine);
            if (!rs) return null;

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
        val = default;
        var size = us.MaximumLength;
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
    /// <summary>安全退出进程，目标进程还有机会执行退出代码</summary>
    /// <remarks>
    /// Linux系统下，使用kill命令发送信号，等待一段时间后再Kill。
    /// Windows系统下，使用taskkill命令，等待一段时间后再Kill。
    /// </remarks>
    /// <param name="process">目标进程</param>
    /// <param name="msWait">等待退出的时间。默认5000毫秒</param>
    /// <param name="times">重试次数</param>
    /// <param name="interval">间隔时间，毫秒</param>
    /// <returns></returns>
    public static Process? SafetyKill(this Process process, Int32 msWait = 5_000, Int32 times = 50, Int32 interval = 200)
    {
        if (process == null || process.GetHasExited()) return process;

        XTrace.WriteLine("安全，温柔一刀！PID={0}/{1}", process.Id, process.ProcessName);

        try
        {
            if (Runtime.Linux)
            {
                Process.Start("kill", process.Id.ToString()).WaitForExit(msWait);

                for (var i = 0; i < times && !process.GetHasExited(); i++)
                {
                    Thread.Sleep(interval);
                }
            }
            else if (Runtime.Windows)
            {
                Process.Start("taskkill", $"-pid {process.Id}").WaitForExit(msWait);

                for (var i = 0; i < times && !process.GetHasExited(); i++)
                {
                    Thread.Sleep(interval);
                }
            }
        }
        catch { }

        //if (!process.GetHasExited()) process.Kill();

        return process;
    }

    /// <summary>强制结束进程树，包含子进程</summary>
    /// <param name="process">目标进程</param>
    /// <param name="msWait">等待退出的时间。默认5000毫秒</param>
    /// <returns></returns>
    public static Process? ForceKill(this Process process, Int32 msWait = 5_000)
    {
        if (process == null || process.GetHasExited()) return process;

        XTrace.WriteLine("强杀，大力出奇迹！PID={0}/{1}", process.Id, process.ProcessName);

        // 终止指定的进程及启动的子进程,如nginx等
        // 在Core 3.0, Core 3.1, 5, 6, 7, 8, 9 中支持此重载
        // https://learn.microsoft.com/zh-cn/dotnet/api/system.diagnostics.process.kill?view=net-8.0#system-diagnostics-process-kill(system-boolean)
#if NETCOREAPP
        process.Kill(true);
#else
        process.Kill();
#endif

        try
        {
            if (Runtime.Linux)
            {
                //-9 SIGKILL 强制终止信号
                Process.Start("kill", $"-9 {process.Id}").WaitForExit(msWait);
            }
            else if (Runtime.Windows)
            {
                // /f 指定强制终止进程，有子进程时只能强制
                // /t 终止指定的进程和由它启用的子进程 
                Process.Start("taskkill", $"/t /f /pid {process.Id}").WaitForExit(msWait);
            }
        }
        catch { }

        // 兜底再来一次
        if (!process.GetHasExited()) process.Kill();

        return process;
    }

    /// <summary>获取进程是否终止</summary>
    public static Boolean GetHasExited(this Process process)
    {
        try
        {
            return process.HasExited;
        }
        catch (Win32Exception)
        {
            return true;
        }
        //catch
        //{
        //    return false;
        //}
    }
    #endregion

    #region 执行命令行

    /// <summary>以隐藏窗口执行命令行</summary>
    /// <param name="cmd">文件名</param>
    /// <param name="arguments">命令参数</param>
    /// <param name="msWait">等待毫秒数</param>
    /// <param name="output">进程输出内容。默认为空时输出到日志</param>
    /// <param name="onExit">进程退出时执行</param>
    /// <param name="working">工作目录</param>
    /// <returns>进程退出代码</returns>
    public static Int32 Run(this String cmd, String? arguments = null, Int32 msWait = 0, Action<String?>? output = null, Action<Process>? onExit = null, String? working = null)
    {
        if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteLine("Run {0} {1} {2}", cmd, arguments, msWait);

        // 修正文件路径
        var fileName = cmd;
        //if (!Path.IsPathRooted(fileName) && !working.IsNullOrEmpty()) fileName = working.CombinePath(fileName);

        var p = new Process();
        var si = p.StartInfo;
        si.FileName = fileName;
        if (arguments != null) si.Arguments = arguments;
        si.WindowStyle = ProcessWindowStyle.Hidden;
        si.CreateNoWindow = true;
        if (!String.IsNullOrWhiteSpace(working)) si.WorkingDirectory = working;
        // 对于控制台项目，这里需要捕获输出
        if (msWait > 0)
        {
            si.UseShellExecute = false;
            si.RedirectStandardOutput = true;
            si.RedirectStandardError = true;
            si.StandardOutputEncoding = Encoding.UTF8;
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
        if (onExit != null) p.Exited += (s, e) => { if (s is Process proc) onExit(proc); };

        p.Start();
        if (msWait > 0)
        {
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
        }

        if (msWait == 0) return -1;

        // 如果未退出，则不能拿到退出代码
        if (msWait < 0)
            p.WaitForExit();
        else if (!p.WaitForExit(msWait))
            return -1;

        return p.ExitCode;
    }

    /// <summary>
    /// 在Shell上执行命令。目标进程不是子进程，不会随着当前进程退出而退出
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="arguments">参数</param>
    /// <param name="workingDirectory">工作目录。目标进程的当前目录</param>
    /// <returns></returns>
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

    /// <summary>执行命令并等待返回</summary>
    /// <param name="cmd">命令</param>
    /// <param name="arguments">命令参数</param>
    /// <param name="msWait">等待退出的时间。默认0毫秒不等待</param>
    /// <param name="returnError">没有标准输出时，是否返回错误内容。默认false</param>
    /// <returns></returns>
    public static String? Execute(this String cmd, String? arguments = null, Int32 msWait = 0, Boolean returnError = false)
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
                //RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
            };
            var process = Process.Start(psi);
            if (process == null) return null;

            if (msWait > 0 && !process.WaitForExit(msWait))
            {
                process.Kill();
                return null;
            }

            var rs = process.StandardOutput.ReadToEnd();
            if (rs.IsNullOrEmpty() && returnError) rs = process.StandardError.ReadToEnd();

            return rs;
        }
        catch { return null; }
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