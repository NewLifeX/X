using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
#if !__CORE__
using System.Runtime.ConstrainedExecution;
#endif
using System.Security;
#if !__MOBILE__ && !__CORE__
using System.Web;

#endif

namespace NewLife
{
    /// <summary>运行时</summary>
    public static class Runtime
    {
        #region 控制台
#if !__MOBILE__ && !__CORE__
        static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
#endif

        private static Boolean? _IsConsole;
        /// <summary>是否控制台。用于判断是否可以执行一些控制台操作。</summary>
        public static Boolean IsConsole
        {
            get
            {
                if (_IsConsole != null) return _IsConsole.Value;

#if __MOBILE__ || __CORE__
                _IsConsole = true;
#else
                if (Mono)
                {
                    _IsConsole = true;
                    return _IsConsole.Value;
                }

                var ip = Win32Native.GetStdHandle(-11);
                if (ip == IntPtr.Zero || ip == INVALID_HANDLE_VALUE)
                    _IsConsole = false;
                else
                {
                    ip = Win32Native.GetStdHandle(-10);
                    if (ip == IntPtr.Zero || ip == INVALID_HANDLE_VALUE)
                        _IsConsole = false;
                    else
                        _IsConsole = true;
                }
#endif

                return _IsConsole.Value;
            }
        }
        #endregion

        #region Web环境
#if __MOBILE__ || __CORE__
        /// <summary>是否Web环境</summary>
        public static Boolean IsWeb { get { return false; } }
#else
        /// <summary>是否Web环境</summary>
        public static Boolean IsWeb => !String.IsNullOrEmpty(HttpRuntime.AppDomainAppId);
#endif
        #endregion

        #region 系统特性
        /// <summary>是否Mono环境</summary>
        public static Boolean Mono { get; } = Type.GetType("Mono.Runtime") != null;

        /// <summary>是否Linux环境</summary>
#if __CORE__
        public static Boolean Linux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
#else
        public static Boolean Linux => false;
#endif

        /// <summary>是否OSX环境</summary>
#if __CORE__
        public static Boolean OSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
#else
        public static Boolean OSX => false;
#endif
        #endregion
    }

#if __MOBILE__
#elif __CORE__
#else
    class Win32Native
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetStdHandle(Int32 nStdHandle);
    }
#endif
}