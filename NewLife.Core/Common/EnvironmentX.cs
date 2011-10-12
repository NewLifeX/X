using System;
using System.Collections.Generic;
using System.Text;
using System.Security;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Reflection.Emit;
using System.IO;
using System.Reflection;

namespace NewLife
{
    /// <summary>当前环境和平台的信息</summary>
    public static class EnvironmentX
    {
        #region 控制台
        /// <summary>是否控制台。用于判断是否可以执行一些控制台操作。</summary>
        public static Boolean IsConsole { get { return ConsoleOutputHandle != IntPtr.Zero; } }

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

        /// <summary>
        /// 获取PE文件类型。扩展方法
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static PEFileKinds GetPEFileKinds(this MemberInfo e)
        {
            return GetPEFileKinds(Path.GetFullPath(e.Module.Assembly.Location));

        }

        /// <summary>
        /// Parses the PE header and determines whether the given assembly is a console application.
        /// </summary>
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
    }
}