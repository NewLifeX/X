﻿using System.Collections;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using NewLife.Log;

namespace NewLife;

/// <summary>运行时</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/runtime
/// </remarks>
public static class Runtime
{
    #region 控制台
    private static Boolean? _IsConsole;
    /// <summary>是否控制台。用于判断是否可以执行一些控制台操作。</summary>
    public static Boolean IsConsole
    {
        get
        {
            if (_IsConsole != null) return _IsConsole.Value;

            // netcore 默认都是控制台，除非主动设置
            _IsConsole = true;

            try
            {
                var flag = Console.ForegroundColor;
                if (Process.GetCurrentProcess().MainWindowHandle != IntPtr.Zero)
                    _IsConsole = false;
                else
                    _IsConsole = true;
            }
            catch
            {
                _IsConsole = false;
            }

            return _IsConsole.Value;
        }
        set => _IsConsole = value;
    }

    /// <summary>是否在容器中运行</summary>
    public static Boolean Container => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
    #endregion

    #region 系统特性
    /// <summary>是否Mono环境</summary>
    public static Boolean Mono { get; } = Type.GetType("Mono.Runtime") != null;

#if !NETFRAMEWORK
    private static Boolean? _IsWeb;
    /// <summary>是否Web环境</summary>
    public static Boolean IsWeb
    {
        get
        {
            if (_IsWeb == null)
            {
                try
                {
                    var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(e => e.GetName().Name == "Microsoft.AspNetCore");
                    _IsWeb = asm != null;
                }
                catch
                {
                    _IsWeb = false;
                }
            }

            return _IsWeb.Value;
        }
    }

    /// <summary>是否Windows环境</summary>
    public static Boolean Windows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>是否Linux环境</summary>
    public static Boolean Linux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>是否OSX环境</summary>
    public static Boolean OSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
#else
    /// <summary>是否Web环境</summary>
    public static Boolean IsWeb => !String.IsNullOrEmpty(System.Web.HttpRuntime.AppDomainAppId);

    /// <summary>是否Windows环境</summary>
    public static Boolean Windows { get; } = Environment.OSVersion.Platform <= PlatformID.WinCE;

    /// <summary>是否Linux环境</summary>
    public static Boolean Linux { get; } = Environment.OSVersion.Platform == PlatformID.Unix;

    /// <summary>是否OSX环境</summary>
    public static Boolean OSX { get; } = Environment.OSVersion.Platform == PlatformID.MacOSX;
#endif
    #endregion

    #region 扩展
#if NETCOREAPP3_1_OR_GREATER
    /// <summary>系统启动以来的毫秒数</summary>
    public static Int64 TickCount64 => Environment.TickCount64;
#else
    /// <summary>系统启动以来的毫秒数</summary>
    public static Int64 TickCount64
    {
        get
        {
            if (Stopwatch.IsHighResolution) return Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;

            return Environment.TickCount;
        }
    }
#endif

    private static Int32 _ProcessId;
#if NET5_0_OR_GREATER
    /// <summary>当前进程Id</summary>
    public static Int32 ProcessId => _ProcessId > 0 ? _ProcessId : _ProcessId = Environment.ProcessId;
#else
    /// <summary>当前进程Id</summary>
    public static Int32 ProcessId => _ProcessId > 0 ? _ProcessId : _ProcessId = Process.GetCurrentProcess().Id;
#endif

    /// <summary>
    /// 获取环境变量。不区分大小写
    /// </summary>
    /// <param name="variable"></param>
    /// <returns></returns>
    public static String? GetEnvironmentVariable(String variable)
    {
        var val = Environment.GetEnvironmentVariable(variable);
        if (!val.IsNullOrEmpty()) return val;

        foreach (var item in Environment.GetEnvironmentVariables())
        {
            if (item is DictionaryEntry de)
            {
                var key = de.Key as String;
                if (key.EqualIgnoreCase(variable)) return de.Value as String;
            }
        }

        return null;
    }

    /// <summary>
    /// 获取环境变量集合。不区分大小写
    /// </summary>
    /// <returns></returns>
    public static IDictionary<String, String?> GetEnvironmentVariables()
    {
        var dic = new Dictionary<String, String?>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in Environment.GetEnvironmentVariables())
        {
            if (item is not DictionaryEntry de) continue;

            var key = de.Key as String;
            if (!key.IsNullOrEmpty()) dic[key] = de.Value as String;
        }

        return dic;
    }
    #endregion

    #region 设置
    private static Boolean? _createConfigOnMissing;
    /// <summary>默认配置。配置文件不存在时，是否生成默认配置文件</summary>
    public static Boolean CreateConfigOnMissing
    {
        get
        {
            if (_createConfigOnMissing == null)
            {
                var val = Environment.GetEnvironmentVariable("CreateConfigOnMissing");
                _createConfigOnMissing = !val.IsNullOrEmpty() ? val.ToBoolean(true) : true;
            }

            return _createConfigOnMissing.Value;
        }
        set { _createConfigOnMissing = value; }
    }
    #endregion

    #region 内存
    /// <summary>释放内存。GC回收后再释放虚拟内存</summary>
    /// <param name="processId">进程Id。默认0表示当前进程</param>
    /// <param name="gc">是否GC回收</param>
    /// <param name="workingSet">是否释放工作集</param>
    public static Boolean FreeMemory(Int32 processId = 0, Boolean gc = true, Boolean workingSet = true)
    {
        if (processId <= 0) processId = ProcessId;

        var p = Process.GetProcessById(processId);
        if (p == null) return false;

        if (processId != ProcessId) gc = false;

        var log = XTrace.Log;
        if (log != null && log.Enable && log.Level <= LogLevel.Debug)
        {
            p ??= Process.GetCurrentProcess();
            var gcm = GC.GetTotalMemory(false) / 1024;
            var ws = p.WorkingSet64 / 1024;
            var prv = p.PrivateMemorySize64 / 1024;
            if (gc)
                log.Debug("[{3}/{4}]开始释放内存：GC={0:n0}K，WorkingSet={1:n0}K，PrivateMemory={2:n0}K", gcm, ws, prv, p.ProcessName, p.Id);
            else
                log.Debug("[{3}/{4}]开始释放内存：WorkingSet={1:n0}K，PrivateMemory={2:n0}K", gcm, ws, prv, p.ProcessName, p.Id);
        }

        if (gc)
        {
            var max = GC.MaxGeneration;
            var mode = GCCollectionMode.Forced;
#if NET8_0_OR_GREATER
            mode = GCCollectionMode.Aggressive;
#endif
#if NET451_OR_GREATER || NETSTANDARD || NETCOREAPP
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
#endif
            GC.Collect(max, mode);
            GC.WaitForPendingFinalizers();
            GC.Collect(max, mode);
        }

        if (workingSet)
        {
            if (Runtime.Windows)
            {
                try
                {
                    p ??= Process.GetProcessById(processId);
                    EmptyWorkingSet(p.Handle);
                }
                catch (Exception ex)
                {
                    log?.Error("EmptyWorkingSet失败：{0}", ex.Message);
                    return false;
                }
            }
        }

        if (log != null && log.Enable && log.Level <= LogLevel.Debug)
        {
            p ??= Process.GetProcessById(processId);
            p.Refresh();
            var gcm = GC.GetTotalMemory(false) / 1024;
            var ws = p.WorkingSet64 / 1024;
            var prv = p.PrivateMemorySize64 / 1024;
            if (gc)
                log.Debug("[{3}/{4}]释放内存完成：GC={0:n0}K，WorkingSet={1:n0}K，PrivateMemory={2:n0}K", gcm, ws, prv, p.ProcessName, p.Id);
            else
                log.Debug("[{3}/{4}]释放内存完成：WorkingSet={1:n0}K，PrivateMemory={2:n0}K", gcm, ws, prv, p.ProcessName, p.Id);
        }

        return true;
    }

    [DllImport("psapi.dll", SetLastError = true)]
    internal static extern Boolean EmptyWorkingSet(IntPtr hProcess);
    #endregion
}