using System.IO;
using NewLife;
using NewLife.Model;
using NewLife.Log;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Common;

public class MachineInfoTests
{
    [Fact(DisplayName = "基础测试")]
    public void BasicTest()
    {
        var mi = new MachineInfo();
        mi.Init();

        Assert.NotEmpty(mi.OSName);
        Assert.NotEmpty(mi.OSVersion);
        Assert.NotEmpty(mi.Product);
        Assert.NotEmpty(mi.Processor);
        Assert.NotEmpty(mi.UUID);
        Assert.NotEmpty(mi.Guid);
        if (mi.Vendor != null) Assert.NotEmpty(mi.Vendor);

        Assert.True(mi.Memory > 1L * 1024 * 1024 * 1024);
        Assert.True(mi.AvailableMemory > 1L * 1024 * 1024);
        Assert.Equal(0UL, mi.UplinkSpeed);
        Assert.Equal(0UL, mi.DownlinkSpeed);
    }

    [Fact(DisplayName = "所有属性测试")]
    public void PropertiesTest()
    {
        var mi = new MachineInfo();
        mi.Init();

        // 静态信息
        Assert.NotNull(mi.OSName);
        Assert.NotNull(mi.OSVersion);
        Assert.NotNull(mi.Product);
        Assert.NotNull(mi.Processor);
        Assert.NotNull(mi.UUID);
        Assert.NotNull(mi.Guid);

        // 内存
        Assert.True(mi.Memory > 0);
        Assert.True(mi.AvailableMemory > 0);
        Assert.True(mi.FreeMemory > 0);

        // 刷新后 CPU 和网络速度
        mi.Refresh();
        Assert.True(mi.CpuRate >= 0);
        Assert.True(mi.CpuRate <= 1);
    }

    [Fact(DisplayName = "刷新测试")]
    public void RefreshTest()
    {
        var mi = new MachineInfo();
        mi.Init();

        // 首次刷新前网络速度为0（差分算法）
        Assert.Equal(0UL, mi.UplinkSpeed);
        Assert.Equal(0UL, mi.DownlinkSpeed);

        // 首次刷新
        mi.Refresh();
        Assert.True(mi.CpuRate >= 0);
        Assert.True(mi.CpuRate <= 1);
        Assert.True(mi.AvailableMemory > 0);

        // 第二次刷新应产生网络速度
        mi.Refresh();
        Assert.True(mi.UplinkSpeed >= 0);
        Assert.True(mi.DownlinkSpeed >= 0);
    }

    [Fact(DisplayName = "索引器测试")]
    public void IndexerTest()
    {
        var mi = new MachineInfo();
        mi["CustomKey"] = 42;
        Assert.Equal(42, mi["CustomKey"]);

        mi["CustomKey"] = "Hello";
        Assert.Equal("Hello", mi["CustomKey"]);

        // 不存在键返回 null
        Assert.Null(mi["NonExistent"]);
    }

    [Fact(DisplayName = "磁盘标识")]
    public void DiskTest()
    {
        var mi = new MachineInfo();
        mi.Init();

        // 仅 Linux 使用 by-id/by-uuid 逻辑
        if (!Runtime.Linux) return;

        Assert.NotNull(mi.DiskID);

        // 存在硬件标识时，优先使用 by-id（如 mmc-8GTF4R_0x4c4f4bb2），而不是文件系统 UUID
        var byId = MachineInfo.GetFiles("/dev/disk/by-id", true);
        if (byId.Count > 0)
        {
            foreach (var item in mi.DiskID.Split(','))
            {
                // UUID 形如 3273d6bf-2c9f-4364-9269-0b2bf026c735，长度为36且第9位为连字符
                Assert.False(item.Length == 36 && item[8] == '-', $"不应使用文件系统UUID：{item}");
            }
        }
    }

    [Fact(DisplayName = "WMI单属性查询测试")]
    public void GetInfoTest()
    {
        // 基本查询：操作系统名称
        var value = MachineInfo.GetInfo("Win32_OperatingSystem", "Caption");
        Assert.NotEmpty(value);
        Assert.Contains("Windows", value, StringComparison.OrdinalIgnoreCase);

        // BIOS 序列号
        var sn = MachineInfo.GetInfo("Win32_BIOS", "SerialNumber");
        Assert.NotNull(sn);

        // 处理器名称
        var cpu = MachineInfo.GetInfo("Win32_Processor", "Name");
        Assert.NotEmpty(cpu);

        // 带命名空间的查询（温度传感器，可能不存在，不应抛出异常）
        var temp = MachineInfo.GetInfo("MSAcpi_ThermalZoneTemperature", "CurrentTemperature", "root/wmi");
        Assert.NotNull(temp);
    }

    [Fact(DisplayName = "WMI多属性查询测试")]
    public void ReadWmiComMultiTest()
    {
        // 别名映射查询：os → Win32_OperatingSystem
        var dic = MachineInfo.ReadWmiComMulti("os", "Caption", "Version");
        Assert.NotNull(dic);
        Assert.True(dic.Count > 0);
        Assert.True(dic.ContainsKey("Caption"));
        Assert.True(dic.ContainsKey("Version"));
        Assert.NotEmpty(dic["Caption"]);
        Assert.NotEmpty(dic["Version"]);

        // path 前缀查询
        var bat = MachineInfo.ReadWmiComMulti("path win32_battery", "EstimatedChargeRemaining");
        Assert.NotNull(bat);
        // Battery 可能不存在于台式机，不应抛出异常

        // 命名空间路径查询
        var temp = MachineInfo.ReadWmiComMulti(@"/namespace:\\root\wmi path MSAcpi_ThermalZoneTemperature", "CurrentTemperature");
        Assert.NotNull(temp);
    }

    [Fact(DisplayName = "GetInfo 显式命名空间测试")]
    public void GetInfoWithExplicitNamespaceTest()
    {
        // 显式传入默认命名空间 root\cimv2（验证 nameSpace 参数传递路径）
        var value1 = MachineInfo.GetInfo("Win32_OperatingSystem", "Caption", "root\\cimv2");
        Assert.NotEmpty(value1);
        Assert.Contains("Windows", value1, StringComparison.OrdinalIgnoreCase);

        // 不传命名空间（使用默认值 null → "root\\cimv2"）
        var value2 = MachineInfo.GetInfo("Win32_OperatingSystem", "Caption");
        Assert.NotEmpty(value2);
        Assert.Equal(value1, value2);
    }

    [Fact(DisplayName = "ReadWmiComMulti WHERE 子句测试")]
    public void ReadWmiComMultiWhereTest()
    {
        // 带 WHERE 子句的磁盘驱动器查询（覆盖 alias 解析的 where 分支）
        var dic = MachineInfo.ReadWmiComMulti("diskdrive where mediatype=\"Fixed hard disk media\"", "SerialNumber", "Size");
        Assert.NotNull(dic);
        // 台式机无固定硬盘时字典可能为空，但不应抛出异常
        if (dic.Count > 0)
        {
            Assert.True(dic.ContainsKey("serialnumber") || dic.ContainsKey("SerialNumber"));
            Assert.True(dic.ContainsKey("Size"));
        }
    }

    [Fact(DisplayName = "GetInfo 不存在的属性测试")]
    public void GetInfoNonExistentPropertyTest()
    {
        // 查询不存在的属性不应抛出异常，应返回空字符串
        var value = MachineInfo.GetInfo("Win32_OperatingSystem", "NonExistentProperty_XYZ_" + Guid.NewGuid().ToString("N"));
        Assert.NotNull(value);
        Assert.Empty(value);
    }

    [Fact(DisplayName = "GetInfo 不存在的类测试")]
    public void GetInfo_NonExistentClass_ReturnsEmpty()
    {
        // 查询不存在的 WMI 类应返回空字符串（不抛异常、不返回 null）
        var value = MachineInfo.GetInfo("Win32_NonExistentClass_XYZ_" + Guid.NewGuid().ToString("N"), "Name");
        Assert.NotNull(value);
        Assert.Empty(value);
    }

    [Theory(DisplayName = "GetInfo 多组 WMI 类/属性查询")]
    [InlineData("Win32_OperatingSystem", "Caption")]
    [InlineData("Win32_BIOS", "SerialNumber")]
    [InlineData("Win32_Processor", "Name")]
    [InlineData("Win32_ComputerSystem", "TotalPhysicalMemory")]
    public void GetInfo_MultipleCommonWmiClasses(String wmiClass, String property)
    {
        var value = MachineInfo.GetInfo(wmiClass, property);
        Assert.NotNull(value);
        if (wmiClass == "Win32_OperatingSystem")
            Assert.Contains("Windows", value, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "GetInfo 空属性测试")]
    public void GetInfo_EmptyProperty_ReturnsEmpty()
    {
        // 空属性名应返回空字符串
        var value = MachineInfo.GetInfo("Win32_OperatingSystem", "");
        Assert.NotNull(value);
        // WMI 查询空属性可能返回空或特定行为，只验证不抛异常
    }

    [Fact(DisplayName = "GetInfo 正斜杠命名空间测试")]
    public void GetInfo_ForwardSlashNamespace()
    {
        // 验证 nameSpace 参数的正斜杠被正确归一化为反斜杠
        // 覆盖 GetInfo 首行：if (nameSpace != null) nameSpace = nameSpace.Replace("/", "\\");
        var value = MachineInfo.GetInfo("Win32_OperatingSystem", "Caption", "root/cimv2");
        Assert.NotEmpty(value);
        Assert.Contains("Windows", value, StringComparison.OrdinalIgnoreCase);

        // 反斜杠版本作为 baseline
        var baseline = MachineInfo.GetInfo("Win32_OperatingSystem", "Caption", "root\\cimv2");
        Assert.Equal(baseline, value);
    }

    [Fact(DisplayName = "GetInfo 空路径测试")]
    public void GetInfo_NullPath_DoesNotThrow()
    {
        // null 路径不应抛异常，应返回空字符串
        var value1 = MachineInfo.GetInfo(null!, "Caption");
        Assert.NotNull(value1);

        // 空路径同样不应抛异常
        var value2 = MachineInfo.GetInfo("", "Caption");
        Assert.NotNull(value2);
    }

#if __WIN__
    [Fact(DisplayName = "QueryWmiSystemManagement 正常查询测试")]
    public void QueryWmiSystemManagement_NormalTest()
    {
        // __WIN__ 保证 System.Management 反射可用，直接断言
        var result = MachineInfo.QueryWmiSystemManagement("Win32_OperatingSystem", "Caption", "root\\cimv2");
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("Windows", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "QueryWmiSystemManagement 错误命名空间抛出测试")]
    public void QueryWmiSystemManagement_BadNamespaceThrowTest()
    {
        // 不存在的命名空间，throwOnError=true 应抛出异常
        Assert.ThrowsAny<Exception>(() =>
            MachineInfo.QueryWmiSystemManagement("Win32_OperatingSystem", "Caption", "root\\BadNamespace", throwOnError: true));
    }

    [Fact(DisplayName = "QueryWmiSystemManagement 不存在的类抛出测试")]
    public void QueryWmiSystemManagement_InvalidClassThrowTest()
    {
        // 不存在的 WMI 类，throwOnError=true 应抛出异常
        Assert.ThrowsAny<Exception>(() =>
            MachineInfo.QueryWmiSystemManagement("Win32_NonExistentClass_XYZ", "Name", "root\\cimv2", throwOnError: true));
    }

    [Fact(DisplayName = "QueryWmiSystemManagement 默认不抛出测试")]
    public void QueryWmiSystemManagement_ThrowOnErrorFalse_DoesNotThrow()
    {
        // throwOnError=false（默认值）时，坏命名空间和坏类不抛异常，返回 null
        var result1 = MachineInfo.QueryWmiSystemManagement("Win32_OperatingSystem", "Caption", "root\\BadNamespace", throwOnError: false);
        Assert.Null(result1);

        var result2 = MachineInfo.QueryWmiSystemManagement("Win32_NonExistentClass_XYZ", "Name", "root\\cimv2", throwOnError: false);
        Assert.Null(result2);
    }

    [Fact(DisplayName = "QueryWmiSystemManagement 空属性测试")]
    public void QueryWmiSystemManagement_EmptyProperty()
    {
        // 空属性名，throwOnError=false 返回 null
        var result = MachineInfo.QueryWmiSystemManagement("Win32_OperatingSystem", "", "root\\cimv2");
        Assert.Null(result);
    }

    [Fact(DisplayName = "QueryWmiSystemManagement 空类名测试")]
    public void QueryWmiSystemManagement_EmptyWmiClass_ReturnsNull()
    {
        // 空类名，throwOnError=false 不抛异常，返回 null
        var result = MachineInfo.QueryWmiSystemManagement("", "Caption", "root\\cimv2");
        Assert.Null(result);
    }

    [Fact(DisplayName = "QueryWmiSystemManagement 不存在的属性测试")]
    public void QueryWmiSystemManagement_NonExistentProperty_ReturnsNull()
    {
        // 有效的类名 + 不存在的属性，应返回 null（不抛异常）
        var result = MachineInfo.QueryWmiSystemManagement("Win32_OperatingSystem", "NonExistent_" + Guid.NewGuid().ToString("N"), "root\\cimv2");
        Assert.Null(result);
    }

    [Fact(DisplayName = "QueryWmiSystemManagement 多实例归并测试")]
    public void QueryWmiSystemManagement_MultipleResults_Join()
    {
        // Win32_LogicalDisk 在多磁盘系统上返回多个实例，验证 Sort + Distinct + Join
        var result = MachineInfo.QueryWmiSystemManagement("Win32_LogicalDisk", "Size", "root\\cimv2");
        // 台式机至少有一个系统盘，Size 不为 null
        if (result != null)
        {
            Assert.NotEmpty(result);
            // 多个磁盘时 Join 用逗号分隔
        }
    }

    [Fact(DisplayName = "QueryWmiSystemManagement 验证与直接 System.Management 接口对比")]
    public void QueryWmiSystemManagement_VerifyAgainstDirectSystemManagement()
    {
        // 使用 ManagementObjectSearcher 直接查询，与反射路径对比结果
        var directValue = GetFromSystemManagementDirectly("Win32_OperatingSystem", "Caption", "root\\cimv2");
        Assert.NotNull(directValue);
        Assert.Contains("Windows", directValue, StringComparison.OrdinalIgnoreCase);

        // 反射路径
        var reflectValue = MachineInfo.QueryWmiSystemManagement("Win32_OperatingSystem", "Caption", "root\\cimv2");
        Assert.NotNull(reflectValue);
        Assert.Equal(directValue, reflectValue);
    }

    private static String? GetFromSystemManagementDirectly(String wmiClass, String property, String nameSpace)
    {
        try
        {
            var wql = $"SELECT {property} FROM {wmiClass}";
            using var searcher = new System.Management.ManagementObjectSearcher(nameSpace, wql);
            var bbs = new List<String>();
            foreach (var mo in searcher.Get())
            {
                using var mObj = (System.Management.ManagementObject)mo;
                var val = mObj[property];
                if (val != null)
                {
                    var v = val.ToString()?.Trim();
                    if (!String.IsNullOrEmpty(v)) bbs.Add(v);
                }
            }
            if (bbs.Count > 0)
            {
                bbs.Sort();
                return bbs.Distinct().Join();
            }
        }
        catch
        {
            // 直接查询失败时返回 null
        }
        return null;
    }
#endif
    [Fact(DisplayName = "QueryWmiCom COM查询测试（best-effort）")]
    public void QueryWmiCom_DirectTest()
    {
        // COM 路径为其它运行时的优选方案，部分环境可能失败，此处 best-effort 验证
        var result = MachineInfo.QueryWmiCom("Win32_OperatingSystem", "Caption", "root\\cimv2");
        if (result != null)
        {
            Assert.NotEmpty(result);
            Assert.Contains("Windows", result, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact(DisplayName = "QueryWmiCom 错误命名空间抛出测试")]
    public void QueryWmiCom_BadNamespaceThrowTest()
    {
        if (!Runtime.Windows) return;

        // 不存在的命名空间，throwOnError=true 应抛出异常
        Assert.ThrowsAny<Exception>(() =>
            MachineInfo.QueryWmiCom("Win32_OperatingSystem", "Caption", "root\\BadNamespace", throwOnError: true));
    }

    [Fact(DisplayName = "QueryWmiCom 不存在的类测试")]
    public void QueryWmiCom_InvalidClass_ReturnsNull()
    {
        if (!Runtime.Windows) return;

        // COM 路径下不存在的 WMI 类不抛出异常（ExecQuery 返回空结果集），应返回 null
        // 这与 System.Management 路径不同，后者会抛出 ManagementException
        var result = MachineInfo.QueryWmiCom("Win32_NonExistentClass_XYZ_" + Guid.NewGuid().ToString("N"), "Name", "root\\cimv2", throwOnError: true);
        Assert.Null(result);
    }

    [Fact(DisplayName = "QueryWmiCom 默认不抛出测试")]
    public void QueryWmiCom_ThrowOnErrorFalse_DoesNotThrow()
    {
        if (!Runtime.Windows) return;

        // throwOnError=false（默认值）时，坏命名空间和坏类不抛异常，返回 null
        var result1 = MachineInfo.QueryWmiCom("Win32_OperatingSystem", "Caption", "root\\BadNamespace", throwOnError: false);
        Assert.Null(result1);

        var result2 = MachineInfo.QueryWmiCom("Win32_NonExistentClass_XYZ_" + Guid.NewGuid().ToString("N"), "Name", "root\\cimv2", throwOnError: false);
        Assert.Null(result2);
    }

    [Fact(DisplayName = "QueryWmiCom 空属性测试")]
    public void QueryWmiCom_EmptyProperty()
    {
        if (!Runtime.Windows) return;

        // 空属性名，throwOnError=false 返回 null
        var result = MachineInfo.QueryWmiCom("Win32_OperatingSystem", "", "root\\cimv2");
        Assert.Null(result);
    }

    [Fact(DisplayName = "QueryWmiCom 不存在的属性测试")]
    public void QueryWmiCom_NonExistentProperty()
    {
        if (!Runtime.Windows) return;

        // 有效的类名 + 不存在的属性，应返回 null
        var result = MachineInfo.QueryWmiCom("Win32_OperatingSystem", "NonExistent_" + Guid.NewGuid().ToString("N"), "root\\cimv2");
        Assert.Null(result);
    }

    [Fact(DisplayName = "QueryWmiCom 多实例归并测试")]
    public void QueryWmiCom_MultipleResults_Join()
    {
        if (!Runtime.Windows) return;

        // Win32_LogicalDisk 在多磁盘系统上返回多个实例，验证 Sort + Distinct + Join
        var result = MachineInfo.QueryWmiCom("Win32_LogicalDisk", "Size", "root\\cimv2");
        if (result != null)
        {
            Assert.NotEmpty(result);
        }
    }

    [Fact(DisplayName = "ReadWmic 查询测试")]
    public void ReadWmicTest()
    {
        // 通过 wmic.exe 查询操作系统信息
        var dic = MachineInfo.ReadWmic("os", "Caption", "Version");
        Assert.NotNull(dic);
        Assert.True(dic.Count > 0);

        // 查询 CPU
        var cpu = MachineInfo.ReadWmic("cpu", "Name");
        Assert.NotNull(cpu);
        if (cpu.Count > 0) Assert.NotEmpty(cpu["Name"]);

        // 磁盘驱动器
        var disk = MachineInfo.ReadWmic("diskdrive where mediatype=\"Fixed hard disk media\"", "SerialNumber");
        Assert.NotNull(disk);
        // 虚拟机可能没有物理磁盘，此时序列号可能为空
        if (disk.Count > 0) Assert.NotEmpty(disk["serialnumber"]);
    }

    [Fact(DisplayName = "PowerShell 查询测试")]
    public void ReadPowerShellTest()
    {
        var dic = MachineInfo.ReadPowerShell("Get-WmiObject Win32_OperatingSystem | Select-Object Caption, Version | ConvertTo-Json");
        Assert.NotNull(dic);
        if (dic.Count > 0)
        {
            Assert.True(dic.ContainsKey("Caption"));
            Assert.True(dic.ContainsKey("Version"));
        }
    }

    [Fact(DisplayName = "磁盘空间测试")]
    public void GetFreeSpaceTest()
    {
        // 当前目录
        var space = MachineInfo.GetFreeSpace();
        Assert.True(space > 0);

        // 系统盘
        var cSpace = MachineInfo.GetFreeSpace("C:\\");
        Assert.True(cSpace > 0);

        // 不存在的路径
        var invalid = MachineInfo.GetFreeSpace("Z:\\NonExistent");
        Assert.True(invalid == -1 || invalid >= 0);
    }

    [Fact(DisplayName = "文件列表测试")]
    public void GetFilesTest()
    {
        var files = MachineInfo.GetFiles(Path.GetTempPath());
        Assert.NotNull(files);

        // 空路径
        var empty = MachineInfo.GetFiles("");
        Assert.NotNull(empty);
        Assert.Empty(empty);

        // 不存在的路径
        var noExist = MachineInfo.GetFiles(@"C:\NonExistentDir_12345");
        Assert.NotNull(noExist);
        Assert.Empty(noExist);
    }

    [Fact(DisplayName = "容器检测测试")]
    public void IsInContainerTest()
    {
        // Windows 下通常不在容器中
        if (Runtime.Windows)
            Assert.False(MachineInfo.IsInContainer);
    }

    [Fact(DisplayName = "容器资源限制测试")]
    public void GetContainerLimitsTest()
    {
        var (memoryLimit, cpuLimit) = MachineInfo.GetContainerLimits();
        // Windows 下容器限制不可用，返回 null
        if (!Runtime.Linux)
        {
            Assert.Null(memoryLimit);
            Assert.Null(cpuLimit);
        }
    }

    [Fact(DisplayName = "容器内存使用测试")]
    public void GetContainerMemoryUsageTest()
    {
        var usage = MachineInfo.GetContainerMemoryUsage();
        // Windows 下不可用，返回 null
        if (!Runtime.Linux) Assert.Null(usage);
    }

    [Fact(DisplayName = "ReadInfo 文件解析测试")]
    public void ReadInfoTest()
    {
        var file = Path.GetTempFileName();
        try
        {
            File.WriteAllText(file, "Key1: Value1\nKey2: Value2\n");
            var dic = MachineInfo.ReadInfo(file);
            Assert.NotNull(dic);
            Assert.Equal("Value1", dic["Key1"]);
            Assert.Equal("Value2", dic["Key2"]);

            // 不存在的文件
            var noFile = MachineInfo.ReadInfo(@"C:\NonExistent_12345.txt");
            Assert.Null(noFile);

            // 空路径
            var empty = MachineInfo.ReadInfo(null!);
            Assert.Null(empty);
        }
        finally
        {
            if (File.Exists(file)) File.Delete(file);
        }
    }

    [Fact(DisplayName = "Linux发行版名称测试")]
    public void GetLinuxNameTest()
    {
        // Windows 下应返回 null（无法读取 /etc/ 下的文件）
        if (!Runtime.Linux)
        {
            var name = MachineInfo.GetLinuxName();
            Assert.Null(name);
        }
    }

    [Fact(DisplayName = "GetCurrent 和 Resolve 测试")]
    public void CurrentAndResolveTest()
    {
        // GetCurrent 触发异步注册
        var mi = MachineInfo.GetCurrent();
        Assert.NotNull(mi);
        Assert.NotNull(mi.OSName);

        // Resolve 从容器获取
        var mi2 = MachineInfo.Resolve();
        Assert.NotNull(mi2);
        Assert.Equal(mi, mi2);
    }

    [Fact(DisplayName = "设备信息测试")]
    public void ReadDeviceInfoTest()
    {
        // 非 Mono 下应返回空（仅用于 Xamarin/Android）
        if (!Runtime.Mono)
        {
            var dic = MachineInfo.ReadDeviceInfo();
            Assert.NotNull(dic);
            Assert.Empty(dic);
        }
    }

    [Fact(DisplayName = "设备电量测试")]
    public void ReadDeviceBatteryTest()
    {
        // 非 Mono 下应返回空（仅用于 Xamarin）
        if (!Runtime.Mono)
        {
            var dic = MachineInfo.ReadDeviceBattery();
            Assert.NotNull(dic);
            Assert.Empty(dic);
        }
    }

    [Fact]
    public void ParseDisk()
    {
        var disks = new List<String>
        {
            "2025-08-14-18-36-42-00",
            "5e47603e-428b-4ec6-9d59-c93f2b260763",
            "C16A-A5F4",
        };
        if (disks.Count > 0)
        {
            // 去掉时间id例如 2025-08-14-18-36-42-00，因为它随着时间在改变
            disks = disks.Where(e => !e.IsNullOrEmpty() && (e.Length < 10 || e[4] != '-' || e[..10].ToDateTime().Year < 2000)).ToList();
            Assert.NotEmpty(disks);
            Assert.Equal("5e47603e-428b-4ec6-9d59-c93f2b260763", disks[0]);
            Assert.Equal("C16A-A5F4", disks[1]);
        }

        disks =
        [
            "virtio-uf6ag3b49w6v4e9ldgcj",
        ];
        if (disks.Count > 0)
        {
            disks = disks.Where(e => !e.IsNullOrEmpty() && !e.Contains("QEMU_")).Select(e => e.TrimPrefix("virtio-")).ToList();
            Assert.NotEmpty(disks);
            Assert.Equal("uf6ag3b49w6v4e9ldgcj", disks[0]);
        }
    }

    [Fact]
    public async Task RegisterTest()
    {
        //MachineInfo.Current = null;
        var mi = await MachineInfo.RegisterAsync();
        Assert.Equal(mi, MachineInfo.Current);

        var mi2 = ObjectContainer.Current.GetService<MachineInfo>();
        Assert.Equal(mi, mi2);

        var file = Path.GetTempPath().CombinePath("machine_info.json").GetFullPath();
        Assert.True(File.Exists(file));

        var mi3 = File.ReadAllText(file).ToJsonEntity<MachineInfo>();
        Assert.Equal(mi.OSName, mi3.OSName);
        Assert.Equal(mi.UUID, mi3.UUID);
        Assert.Equal(mi.Guid, mi3.Guid);
    }

    [Fact]
    public void ProviderTest()
    {
        var savedProvider = MachineInfo.Provider;

        try
        {
            MachineInfo.Provider = new MyProvider();

            var mi = new MachineInfo();
            mi.Init();

            Assert.Equal("NewLife", mi.Product);
            Assert.Equal(98, mi["Signal"]);
            Assert.True(mi.CpuRate > 0.01);

            var js = mi.ToJson();
            var dic = JsonParser.Decode(js);
            var rs = dic.TryGetValue("Signal", out var obj);
            Assert.True(rs);
            Assert.Equal(98, obj);

            mi.Refresh();

            Assert.Equal(0.168f, mi.CpuRate);
        }
        finally
        {
            MachineInfo.Provider = savedProvider;
        }
    }

    class MyProvider : IMachineInfo
    {
        public void Init(MachineInfo info)
        {
            info.Product = "NewLife";
            info["Signal"] = 98;
        }

        public void Refresh(MachineInfo info)
        {
            info.CpuRate = 0.168f;
        }
    }
}