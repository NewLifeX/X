using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;

namespace Test
{
    //[SuppressUnmanagedCodeSecurity]
    internal class OpsInit
    {
        //public OpsInit();
        [DllImport("OraOps11w.dll")]
        public static extern int CheckVersionCompatibility(string version);
        [DllImport("kernel32.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int GetFileAttributes(string fileName);
        [DllImport("kernel32.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr LoadLibrary(string fileName);
        [DllImport("kernel32.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int SetDllDirectory(string pathName);
    }
}
