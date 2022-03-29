using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NewLife
{
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
            set { _IsConsole = value; }
        }
        #endregion

        #region 系统特性
        /// <summary>是否Mono环境</summary>
        public static Boolean Mono { get; } = Type.GetType("Mono.Runtime") != null;

        /// <summary>是否Web环境</summary>
        public static Boolean IsWeb => false;

        /// <summary>是否Windows环境</summary>
        public static Boolean Windows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>是否Linux环境</summary>
        public static Boolean Linux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        /// <summary>是否OSX环境</summary>
        public static Boolean OSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
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
                if (Stopwatch.IsHighResolution) return Stopwatch.GetTimestamp() / Stopwatch.Frequency * 1000;

                return Environment.TickCount;
            }
        }
#endif
        #endregion
    }
}