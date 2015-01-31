using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
#if !Android
using System.Web;
#endif

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

        ///// <summary>获取PE文件类型。扩展方法</summary>
        ///// <param name="e"></param>
        ///// <returns></returns>
        //public static PEFileKinds GetPEFileKinds(this MemberInfo e)
        //{
        //    return GetPEFileKinds(Path.GetFullPath(e.Module.Assembly.Location));

        //}

        ///// <summary>分析PE头并检测程序集类型</summary>
        ///// <param name="assemblyPath">The path of the assembly to check.</param>
        ///// <remarks>The magic numbers in this method are extracted from the PE/COFF file
        ///// format specification available from http://www.microsoft.com/whdc/system/platform/firmware/pecoff.mspx
        ///// </remarks>
        //static PEFileKinds GetPEFileKinds(string assemblyPath)
        //{
        //    using (var s = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read))
        //    {
        //        return GetPEFileKinds(s);
        //    }
        //}

        //private static PEFileKinds GetPEFileKinds(Stream stream)
        //{
        //    var rawPeSignatureOffset = new byte[4];
        //    stream.Seek(0x3c, SeekOrigin.Begin);
        //    stream.Read(rawPeSignatureOffset, 0, 4);
        //    int peSignatureOffset = rawPeSignatureOffset[0];
        //    peSignatureOffset |= rawPeSignatureOffset[1] << 8;
        //    peSignatureOffset |= rawPeSignatureOffset[2] << 16;
        //    peSignatureOffset |= rawPeSignatureOffset[3] << 24;
        //    var coffHeader = new byte[24];
        //    stream.Seek(peSignatureOffset, SeekOrigin.Begin);
        //    stream.Read(coffHeader, 0, 24);
        //    byte[] signature = { (byte)'P', (byte)'E', (byte)'\0', (byte)'\0' };
        //    for (int index = 0; index < 4; index++)
        //    {
        //        if (coffHeader[index] != signature[index]) throw new InvalidOperationException("Attempted to check a non PE file for the console subsystem!");
        //    }
        //    var subsystemBytes = new byte[2];
        //    stream.Seek(68, SeekOrigin.Current);
        //    stream.Read(subsystemBytes, 0, 2);
        //    int subSystem = subsystemBytes[0] | subsystemBytes[1] << 8;
        //    return
        //        // http://support.microsoft.com/kb/90493
        //        subSystem == 3 ? PEFileKinds.ConsoleApplication :
        //        subSystem == 2 ? PEFileKinds.WindowApplication :
        //        PEFileKinds.Dll; /*IMAGE_SUBSYSTEM_WINDOWS_CUI*/
        //}
        #endregion

        #region Web环境
#if !Android
        /// <summary>是否Web环境</summary>
        public static Boolean IsWeb { get { return !String.IsNullOrEmpty(HttpRuntime.AppDomainAppId); } }
#endif
        #endregion

        #region Mono
        private static Boolean? _Mono;
        /// <summary>是否Mono环境</summary>
        public static Boolean Mono
        {
            get
            {
                if (_Mono == null) _Mono = Type.GetType("Mono.Runtime") != null;

                return _Mono.Value;
            }
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
        public static Boolean Is64BitProcess { get { return IntPtr.Size == 8; } }
        #endregion

        #region 操作系统
        private static String _OSName;
        /// <summary>操作系统</summary>
        public static String OSName
        {
            get
            {
                if (_OSName != null) return _OSName;

                var os = Environment.OSVersion;
                var vs = os.Version;
                var is64 = Is64BitOperatingSystem;
                var sys = "";

                #region Win32
                if (os.Platform == PlatformID.Win32Windows)
                {
                    // 非NT系统
                    switch (vs.Minor)
                    {
                        case 0:
                            sys = "95";
                            break;
                        case 10:
                            if (vs.Revision.ToString() == "2222A")
                                sys = "98SE";
                            else
                                sys = "98";
                            break;
                        case 90:
                            sys = "Me";
                            break;
                        default:
                            sys = vs.ToString();
                            break;
                    }
                }
                #endregion
                else if (os.Platform == PlatformID.Win32NT)
                    sys = GetNTName(vs);
                else
                    return _OSName = os.ToString();

                if (sys != "")
                {
                    sys = "Windows " + sys;

                    // 补丁
                    if (os.ServicePack != "") sys += " " + os.ServicePack;

                    if (is64) sys += " x64";
                }

                return _OSName = sys;
            }
        }

        static String GetNTName(Version vs)
        {
            var ver = new Win32Native.OSVersionInfoEx();
            if (!Win32Native.GetVersionEx(ver)) ver = null;
            var isStation = ver == null || ver.ProductType == OSProductType.WorkStation;

            var is64 = Is64BitOperatingSystem;

            const Int32 SM_SERVERR2 = 89;
            var IsServerR2 = Win32Native.GetSystemMetrics(SM_SERVERR2) == 1;

            var sys = "";
            switch (vs.Major)
            {
                case 3:
                    sys = "NT 3.51";
                    break;
                case 4:
                    sys = "NT 4.0";
                    break;
                case 5:
                    if (vs.Minor == 0)
                    {
                        sys = "2000";
                        if (ver != null && ver.ProductType != OSProductType.WorkStation)
                        {
                            if (ver.SuiteMask == OSSuites.Enterprise)
                                sys += " Datacenter Server";
                            else if (ver.SuiteMask == OSSuites.Datacenter)
                                sys += " Advanced Server";
                            else
                                sys += " Server";
                        }
                    }
                    else if (vs.Minor == 1)
                    {
                        sys = "XP";
                        if (ver != null)
                        {
                            if (ver.SuiteMask == OSSuites.EmbeddedNT)
                                sys += " Embedded";
                            else if (ver.SuiteMask == OSSuites.Personal)
                                sys += " Home";
                            else
                                sys += " Professional";
                        }
                    }
                    else if (vs.Minor == 2)
                    {
                        // 64位XP也是5.2
                        if (is64 && ver != null && ver.ProductType == OSProductType.WorkStation)
                            sys = "XP Professional";
                        else if (ver != null && ver.SuiteMask == OSSuites.WHServer)
                            sys = "Home Server";
                        else
                        {
                            sys = "Server 2003";
                            if (IsServerR2) sys += " R2";
                            if (ver != null)
                            {
                                switch (ver.SuiteMask)
                                {
                                    case OSSuites.Enterprise:
                                        sys += " Enterprise";
                                        break;
                                    case OSSuites.Datacenter:
                                        sys += " Datacenter";
                                        break;
                                    case OSSuites.Blade:
                                        sys += " Web";
                                        break;
                                    default:
                                        sys += " Standard";
                                        break;
                                }
                            }
                        }
                    }
                    else
                        sys = String.Format("{0}.{1}", vs.Major, vs.Minor);
                    break;
                case 6:
                    if (vs.Minor == 0)
                        sys = isStation ? "Vista" : "Server 2008";
                    else if (vs.Minor == 1)
                        sys = isStation ? "7" : "Server 2008 R2";
                    else if (vs.Minor == 2)
                        sys = isStation ? "8" : "Server 2012";
                    else
                        sys = String.Format("{0}.{1}", vs.Major, vs.Minor);
                    break;
                default:
                    sys = "NT " + vs.ToString();
                    break;
            }

            return sys;
        }
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

    /// <summary>标识系统上的程序组</summary>
    [Flags]
    enum OSSuites : ushort
    {
        //None = 0,
        SmallBusiness = 0x00000001,
        Enterprise = 0x00000002,
        BackOffice = 0x00000004,
        Communications = 0x00000008,
        Terminal = 0x00000010,
        SmallBusinessRestricted = 0x00000020,
        EmbeddedNT = 0x00000040,
        Datacenter = 0x00000080,
        SingleUserTS = 0x00000100,
        Personal = 0x00000200,
        Blade = 0x00000400,
        EmbeddedRestricted = 0x00000800,
        Appliance = 0x00001000,
        WHServer = 0x00008000
    }

    /// <summary>标识系统类型</summary>
    enum OSProductType : byte
    {
        /// <summary>工作站</summary>
        [Description("工作站")]
        WorkStation = 1,

        /// <summary>域控制器</summary>
        [Description("域控制器")]
        DomainController = 2,

        /// <summary>服务器</summary>
        [Description("服务器")]
        Server = 3
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

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool GetVersionEx([In, Out] OSVersionInfoEx ver);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal class OSVersionInfoEx
        {
            public int OSVersionInfoSize;
            public int MajorVersion;        // 系统主版本号
            public int MinorVersion;        // 系统次版本号
            public int BuildNumber;         // 系统构建号
            public int PlatformId;          // 系统支持的平台
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string CSDVersion;       // 系统补丁包的名称
            public ushort ServicePackMajor; // 系统补丁包的主版本
            public ushort ServicePackMinor; // 系统补丁包的次版本
            public OSSuites SuiteMask;         // 标识系统上的程序组
            public OSProductType ProductType;        // 标识系统类型
            public byte Reserved;           // 保留
            public OSVersionInfoEx()
            {
                OSVersionInfoSize = Marshal.SizeOf(this);
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetSystemMetrics(int nIndex);
    }
}