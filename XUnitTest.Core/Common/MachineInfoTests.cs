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