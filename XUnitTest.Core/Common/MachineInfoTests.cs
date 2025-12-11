using NewLife;
using NewLife.Model;
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
        //Assert.NotEmpty(mi.CpuID);
        Assert.NotEmpty(mi.UUID);
        Assert.NotEmpty(mi.Guid);
        //Assert.NotEmpty(mi.Serial);
        //Assert.NotEmpty(mi.DiskID);

        Assert.True(mi.Memory > 1L * 1024 * 1024 * 1024);
        Assert.True(mi.AvailableMemory > 1L * 1024 * 1024);
        //Assert.True(mi.CpuRate > 0.001);
        Assert.Equal(0UL, mi.UplinkSpeed);
        Assert.Equal(0UL, mi.DownlinkSpeed);
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
            disks = disks.Where(e => !e.IsNullOrEmpty() && !e.Contains("QEMU_")).Select(e => e.TrimStart("virtio-")).ToList();
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