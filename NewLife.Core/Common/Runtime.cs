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

        #region 内存设置
#if __MOBILE__
#elif __CORE__
#else
        /// <summary>设置进程的程序集大小，将部分物理内存占用转移到虚拟内存</summary>
        /// <param name="pid">要设置的进程ID</param>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <returns></returns>
        public static Boolean SetProcessWorkingSetSize(Int32 pid, Int32 min, Int32 max)
        {
            var p = pid <= 0 ? Process.GetCurrentProcess() : Process.GetProcessById(pid);
            return Win32Native.SetProcessWorkingSetSize(p.Handle, min, max);
        }

        /// <summary>释放当前进程所占用的内存</summary>
        /// <returns></returns>
        public static Boolean ReleaseMemory()
        {
            GC.Collect();

            return SetProcessWorkingSetSize(0, -1, -1);
        }

        private static Int32? _PhysicalMemory;
        /// <summary>物理内存大小。单位MB</summary>
        public static Int32 PhysicalMemory
        {
            get
            {
                if (_PhysicalMemory == null) Refresh();
                return _PhysicalMemory.Value;
            }
        }

        private static Int32? _AvailableMemory;
        /// <summary>可用物理内存大小。单位MB</summary>
        public static Int32 AvailableMemory
        {
            get
            {
                if (_AvailableMemory == null) Refresh();
                return _AvailableMemory.Value;
            }
        }

        //private static Int32? _VirtualMemory;
        ///// <summary>虚拟内存大小。单位MB</summary>
        //public static Int32 VirtualMemory
        //{
        //    get
        //    {
        //        if (_VirtualMemory == null) Refresh();
        //        return _VirtualMemory.Value;
        //    }
        //}

        private static void Refresh()
        {
            if (Mono)
            {
                _PhysicalMemory = 0;
                _AvailableMemory = 0;
                return;
            }
            //var ci = new ComputerInfo();
            //_PhysicalMemory = (Int32)(ci.TotalPhysicalMemory / 1024 / 1024);
            //_VirtualMemory = (Int32)(ci.TotalVirtualMemory / 1024 / 1024);

            var st = default(Win32Native.MEMORYSTATUSEX);
            st.Init();
            Win32Native.GlobalMemoryStatusEx(ref st);

            _PhysicalMemory = (Int32)(st.ullTotalPhys / 1024 / 1024);
            _AvailableMemory = (Int32)(st.ullAvailPhys / 1024 / 1024);
            //_VirtualMemory = (Int32)(st.ullTotalVirtual / 1024 / 1024);

        }
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

        [DllImport("kernel32.dll")]
        internal static extern Boolean SetProcessWorkingSetSize(IntPtr proc, Int32 min, Int32 max);

        public struct MEMORYSTATUSEX
        {
            internal UInt32 dwLength;
            internal UInt32 dwMemoryLoad;
            internal UInt64 ullTotalPhys;
            internal UInt64 ullAvailPhys;
            internal UInt64 ullTotalPageFile;
            internal UInt64 ullAvailPageFile;
            internal UInt64 ullTotalVirtual;
            internal UInt64 ullAvailVirtual;
            internal UInt64 ullAvailExtendedVirtual;
            internal void Init()
            {
                dwLength = checked((UInt32)Marshal.SizeOf(typeof(MEMORYSTATUSEX)));
            }
        }

        [SecurityCritical]
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
    }
#endif
}