using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NewLife;

/// <summary>运行时</summary>
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

    /// <summary>
    /// 获取环境变量。不区分大小写
    /// </summary>
    /// <param name="variable"></param>
    /// <returns></returns>
    public static String GetEnvironmentVariable(String variable)
    {
        var val = Environment.GetEnvironmentVariable(variable);
        if (!val.IsNullOrEmpty()) return val;

        foreach (DictionaryEntry item in Environment.GetEnvironmentVariables())
        {
            var key = item.Key as String;
            if (key.EqualIgnoreCase(variable)) return item.Value as String;
        }

        return null;
    }
    #endregion
}