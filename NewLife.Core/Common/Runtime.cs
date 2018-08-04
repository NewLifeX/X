using System;
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

                try
                {
                    var flag = Console.CursorVisible;
                    _IsConsole = true;
                }
                catch
                {
                    _IsConsole = false;
                }

                return _IsConsole.Value;
            }
        }
        #endregion

        #region Web环境
#if __MOBILE__ || __CORE__
        /// <summary>是否Web环境</summary>
        public static Boolean IsWeb => false;
#else
        /// <summary>是否Web环境</summary>
        public static Boolean IsWeb => !String.IsNullOrEmpty(System.Web.HttpRuntime.AppDomainAppId);
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
}