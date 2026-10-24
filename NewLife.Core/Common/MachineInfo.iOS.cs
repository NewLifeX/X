#if NET5_0_OR_GREATER
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace NewLife;

/// <summary>机器信息 iOS 部分</summary>
public partial class MachineInfo
{
    /// <summary>加载 iOS 静态信息</summary>
    [SupportedOSPlatform("ios")]
    private void LoadIOSInfo()
    {
        OSName = "iOS";
        Vendor = "Apple";
        Processor = RuntimeInformation.ProcessArchitecture.ToString();

        var machine = SysctlString("hw.machine");
        if (!machine.IsNullOrEmpty()) Product = machine;

        var model = SysctlString("hw.model");
        if (!model.IsNullOrEmpty()) Board = model;

        var memory = SysctlUInt64("hw.memsize");
        if (memory > 0) Memory = memory;

        // Environment.OSVersion 在 iOS 上返回 Darwin 内核版本，这里改用系统产品版本（如 17.0）
        var version = SysctlString("kern.osproductversion");
        if (!version.IsNullOrEmpty()) OSVersion = version;
    }

    /// <summary>刷新 iOS 实时信息</summary>
    [SupportedOSPlatform("ios")]
    private void RefreshIOS()
    {
        if (Memory == 0)
        {
            var memory = SysctlUInt64("hw.memsize");
            if (memory > 0) Memory = memory;
        }

        var available = GetAvailableMemory();
        if (available > 0)
        {
            if (Memory > 0 && available > Memory) available = Memory;
            AvailableMemory = available;
            FreeMemory = available;
        }
    }

    [SupportedOSPlatform("ios")]
    private static UInt64 GetAvailableMemory()
    {
        try
        {
            return OsProcAvailableMemory();
        }
        catch
        {
            return 0;
        }
    }

    [SupportedOSPlatform("ios")]
    private static String? SysctlString(String name)
    {
        var len = UIntPtr.Zero;
        if (SysctlByName(name, null, ref len, IntPtr.Zero, UIntPtr.Zero) != 0) return null;

        var size = (Int32)len.ToUInt64();
        if (size <= 0) return null;

        var buffer = new Byte[size];
        if (SysctlByName(name, buffer, ref len, IntPtr.Zero, UIntPtr.Zero) != 0) return null;

        var length = (Int32)len.ToUInt64();
        if (length > buffer.Length) length = buffer.Length;
        while (length > 0 && buffer[length - 1] == 0) length--;

        return Encoding.UTF8.GetString(buffer, 0, length);
    }

    [SupportedOSPlatform("ios")]
    private static UInt64 SysctlUInt64(String name)
    {
        var buffer = new Byte[sizeof(UInt64)];
        var len = new UIntPtr((UInt32)buffer.Length);
        if (SysctlByName(name, buffer, ref len, IntPtr.Zero, UIntPtr.Zero) != 0) return 0;

        return BitConverter.ToUInt64(buffer, 0);
    }

    [DllImport("libSystem.dylib", EntryPoint = "sysctlbyname")]
    private static extern Int32 SysctlByName([MarshalAs(UnmanagedType.LPUTF8Str)] String name, Byte[]? oldp, ref UIntPtr oldlenp, IntPtr newp, UIntPtr newlen);

    [DllImport("libSystem.dylib", EntryPoint = "os_proc_available_memory")]
    private static extern UInt64 OsProcAvailableMemory();
}
#endif
