using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Web;

namespace NewLife
{
    /// <summary>运行时</summary>
    public static class Runtime
    {
        #region 控制台
        static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        private static Boolean? _IsConsole;
        /// <summary>是否控制台。用于判断是否可以执行一些控制台操作。</summary>
        public static Boolean IsConsole
        {
            get
            {
                if (_IsConsole != null) return _IsConsole.Value;

                IntPtr ip = Win32Native.GetStdHandle(-11);
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

                return _IsConsole.Value;
            }
        }

        private static IntPtr _consoleOutputHandle;
        private static IntPtr ConsoleOutputHandle
        {
            [SecurityCritical]
            get
            {
                if (_consoleOutputHandle == IntPtr.Zero) _consoleOutputHandle = Win32Native.GetStdHandle(-11);
                return _consoleOutputHandle;
            }
        }

        /// <summary>获取PE文件类型。扩展方法</summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static PEFileKinds GetPEFileKinds(this MemberInfo e)
        {
            return GetPEFileKinds(Path.GetFullPath(e.Module.Assembly.Location));

        }

        /// <summary>Parses the PE header and determines whether the given assembly is a console application.</summary>
        /// <param name="assemblyPath">The path of the assembly to check.</param>
        /// <remarks>The magic numbers in this method are extracted from the PE/COFF file
        /// format specification available from http://www.microsoft.com/whdc/system/platform/firmware/pecoff.mspx
        /// </remarks>
        static PEFileKinds GetPEFileKinds(string assemblyPath)
        {
            using (var s = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read))
            {
                return GetPEFileKinds(s);
            }
        }

        private static PEFileKinds GetPEFileKinds(Stream s)
        {
            var rawPeSignatureOffset = new byte[4];
            s.Seek(0x3c, SeekOrigin.Begin);
            s.Read(rawPeSignatureOffset, 0, 4);
            int peSignatureOffset = rawPeSignatureOffset[0];
            peSignatureOffset |= rawPeSignatureOffset[1] << 8;
            peSignatureOffset |= rawPeSignatureOffset[2] << 16;
            peSignatureOffset |= rawPeSignatureOffset[3] << 24;
            var coffHeader = new byte[24];
            s.Seek(peSignatureOffset, SeekOrigin.Begin);
            s.Read(coffHeader, 0, 24);
            byte[] signature = { (byte)'P', (byte)'E', (byte)'\0', (byte)'\0' };
            for (int index = 0; index < 4; index++)
            {
                if (coffHeader[index] != signature[index]) throw new InvalidOperationException("Attempted to check a non PE file for the console subsystem!");
            }
            var subsystemBytes = new byte[2];
            s.Seek(68, SeekOrigin.Current);
            s.Read(subsystemBytes, 0, 2);
            int subSystem = subsystemBytes[0] | subsystemBytes[1] << 8;
            return
                // http://support.microsoft.com/kb/90493
                subSystem == 3 ? PEFileKinds.ConsoleApplication :
                subSystem == 2 ? PEFileKinds.WindowApplication :
                PEFileKinds.Dll; /*IMAGE_SUBSYSTEM_WINDOWS_CUI*/
        }
        #endregion

        #region Web环境
        /// <summary>是否Web环境</summary>
        public static Boolean IsWeb { get { return !String.IsNullOrEmpty(HttpRuntime.AppDomainAppId); } }
        #endregion

        #region 64位系统
        /// <summary>确定当前操作系统是否为 64 位操作系统。</summary>
        /// <returns>如果操作系统为 64 位操作系统，则为 true；否则为 false。</returns>
        public static Boolean Is64BitOperatingSystem
        {
            [SecuritySafeCritical]
            get
            {
                if (Is64BitProcess) return true;

                Boolean flag;
                return Win32Native.DoesWin32MethodExist("kernel32.dll", "IsWow64Process") && Win32Native.IsWow64Process(Win32Native.GetCurrentProcess(), out flag) && flag;
            }
        }

        /// <summary>确定当前进程是否为 64 位进程。</summary>
        /// <returns>如果进程为 64 位进程，则为 true；否则为 false。</returns>
        public static bool Is64BitProcess { get { return IntPtr.Size == 8; } }
        #endregion

        #region 内存设置
        /// <summary>设置进程的程序集大小，将部分物理内存占用转移到虚拟内存</summary>
        /// <param name="pid">要设置的进程ID</param>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <returns></returns>
        public static Boolean SetProcessWorkingSetSize(Int32 pid, Int32 min, Int32 max)
        {
            Process p = pid <= 0 ? Process.GetCurrentProcess() : Process.GetProcessById(pid);
            return Win32Native.SetProcessWorkingSetSize(p.Handle, min, max);
        }

        /// <summary>释放当前进程所占用的内存</summary>
        /// <returns></returns>
        public static Boolean ReleaseMemory()
        {
            GC.Collect();

            return SetProcessWorkingSetSize(0, -1, -1);
        }
        #endregion
    }

    class Win32Native
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int nStdHandle);

        [SecurityCritical]
        internal static bool DoesWin32MethodExist(string moduleName, string methodName)
        {
            IntPtr moduleHandle = GetModuleHandle(moduleName);
            if (moduleHandle == IntPtr.Zero) return false;
            return GetProcAddress(moduleHandle, methodName) != IntPtr.Zero;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail), DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string moduleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string methodName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetCurrentProcess();

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool IsWow64Process([In] IntPtr hSourceProcessHandle, [MarshalAs(UnmanagedType.Bool)] out bool isWow64);

        [DllImport("kernel32.dll")]
        internal static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);
    }
}