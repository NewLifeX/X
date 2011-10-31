using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
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

        #region JIT方法地址
        /// <summary>
        /// 获取方法在JIT编译后的地址(JIT Stubs)
        /// </summary>
        /// <remarks>
        /// MethodBase.DeclaringType.TypeHandle.Value: 指向该类型方法表(编译后)在 JIT Stubs 的起始位置。
        /// Method.MethodHandle.Value: 表示该方法的索引序号。
        /// CLR 2.0 SP2 (2.0.50727.3053) 及其后续版本中，该地址的内存布局发生了变化。直接用 "Method.MethodHandle.Value + 2" 即可得到编译后的地址。
        /// </remarks>
        /// <param name="method"></param>
        /// <returns></returns>
        unsafe public static IntPtr GetMethodAddress(MethodBase method)
        {
            // 处理动态方法
            if (method is DynamicMethod)
            {
                //byte* ptr = (byte*)GetDynamicMethodRuntimeHandle(method).ToPointer();

                FieldInfo fieldInfo = typeof(DynamicMethod).GetField("m_method", BindingFlags.NonPublic | BindingFlags.Instance);
                byte* ptr = (byte*)((RuntimeMethodHandle)fieldInfo.GetValue(method)).Value.ToPointer();

                if (IntPtr.Size == 8)
                {
                    ulong* address = (ulong*)ptr;
                    address += 6;
                    return new IntPtr(address);
                }
                else
                {
                    uint* address = (uint*)ptr;
                    address += 6;
                    return new IntPtr(address);
                }
            }

            // 确保方法已经被编译
            RuntimeHelpers.PrepareMethod(method.MethodHandle);

            if (Environment.Version.Major >= 2 && Environment.Version.MinorRevision >= 3053)
            {
                return new IntPtr((int*)method.MethodHandle.Value.ToPointer() + 2);
            }
            else
            {
                // 要跳过的
                var skip = 10;

                // 读取方法索引
                var location = (UInt64*)(method.MethodHandle.Value.ToPointer());
                var index = (int)(((*location) >> 32) & 0xFF);

                // 区分处理x86和x64
                if (IntPtr.Size == 8)
                {
                    // 获取方法表
                    var classStart = (ulong*)method.DeclaringType.TypeHandle.Value.ToPointer();
                    var address = classStart + index + skip;
                    return new IntPtr(address);
                }
                else
                {
                    // 获取方法表
                    var classStart = (uint*)method.DeclaringType.TypeHandle.Value.ToPointer();
                    var address = classStart + index + skip;
                    return new IntPtr(address);
                }
            }
        }

        /// <summary>
        /// 替换方法
        /// </summary>
        /// <remarks>
        /// Method Address 处所存储的 Native Code Address 是可以修改的，也就意味着我们完全可以用另外一个具有相同签名的方法来替代它，从而达到偷梁换柱(Injection)的目的。
        /// </remarks>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        public static void ReplaceMethod(MethodBase src, MethodBase dest)
        {
            IntPtr s = GetMethodAddress(src);
            IntPtr d = GetMethodAddress(dest);

            ReplaceMethod(s, d);
        }

        unsafe private static void ReplaceMethod(IntPtr src, IntPtr dest)
        {
            // 区分处理x86和x64
            if (IntPtr.Size == 8)
            {
                var d = (ulong*)src.ToPointer();
                *d = *((ulong*)dest.ToPointer());
            }
            else
            {
                var d = (uint*)src.ToPointer();
                *d = *((uint*)dest.ToPointer());
            }
        }
        #endregion

        #region 内存设置
        /// <summary>
        /// 设置进程的程序集大小，将部分物理内存占用转移到虚拟内存
        /// </summary>
        /// <param name="pid">要设置的进程ID</param>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <returns></returns>
        public static Boolean SetProcessWorkingSetSize(Int32 pid, Int32 min, Int32 max)
        {
            Process p = pid <= 0 ? Process.GetCurrentProcess() : Process.GetProcessById(pid);
            return Win32Native.SetProcessWorkingSetSize(p.Handle, min, max);
        }

        /// <summary>
        /// 释放当前进程所占用的内存
        /// </summary>
        /// <returns></returns>
        public static Boolean ReleaseMemory()
        {
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