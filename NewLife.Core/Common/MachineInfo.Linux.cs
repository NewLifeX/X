using System.Runtime.Versioning;

namespace NewLife;

/// <summary>机器信息 Linux 部分</summary>
public partial class MachineInfo
{
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("linux")]
#endif
    private void LoadLinuxInfo()
    {
        var str = GetLinuxName();
        if (!str.IsNullOrEmpty()) OSName = str;

        var device = ReadDeviceInfo();

        if (device.TryGetValue("Platform", out str))
            OSName = str;
        if (device.TryGetValue("Version", out str))
            OSVersion = str;

        // 树莓派的Hardware无法区分P0/P4
        var dic = ReadInfo("/proc/cpuinfo");
        if (dic != null)
        {
            if (dic.TryGetValue("Hardware", out str) ||
                dic.TryGetValue("cpu model", out str) ||
                dic.TryGetValue("model name", out str))
                Processor = str?.TrimPrefix("vendor ");

            if (device.TryGetValue("Product", out str))
                Product = str;
            else if (dic.TryGetValue("Model", out str))
                Product = str;

            if (dic.TryGetValue("vendor_id", out str))
                Vendor = str;

            //if (device.TryGetValue("Fingerprint", out str) && !str.IsNullOrEmpty())
            //    CpuID = str;
            if (dic.TryGetValue("Serial", out str) && !str.IsNullOrEmpty() && !str.Trim('0').IsNullOrEmpty())
                UUID = str;
        }

        var mid = "/etc/machine-id";
        if (!File.Exists(mid)) mid = "/var/lib/dbus/machine-id";
        if (TryRead(mid, out var value))
            Guid = value;
        else if (device.TryGetValue("android_id", out str) && !str.IsNullOrEmpty() && str != "unknown")
            Guid = str;
        //else if (android.TryGetValue("Id", out str))
        //    Guid = str;

        // DMI信息位于 /sys/class/dmi/id/ 目录，可以直接读取，不需要执行dmidecode命令
        var uuid = "";
        var file = "/sys/class/dmi/id/product_uuid";
        if (!File.Exists(file)) file = "/etc/uuid";
        if (!File.Exists(file)) file = "/proc/serial_num";  // miui12支持/proc/serial_num
        if (TryRead(file, out value))
            uuid = value;
        else if (device.TryGetValue("Serial", out str) && str != "unknown")
            uuid = str;
        if (!uuid.IsNullOrEmpty()) UUID = uuid;

        // 从release文件读取产品
        var prd = GetProductByRelease();
        if (!prd.IsNullOrEmpty()) Product = prd;

        if (prd.IsNullOrEmpty() && TryRead("/sys/class/dmi/id/product_name", out var product_name))
        {
            Product = product_name;

            // 增加制造商。如 Tencent Cloud，它的产品名只有 CVM。阿里云产品名 Alibaba Cloud ECS
            if (TryRead("/sys/class/dmi/id/sys_vendor", out var vendor) && !vendor.IsNullOrEmpty())
            {
                Vendor = vendor;

                if (!product_name.IsNullOrEmpty() && !product_name.Contains(vendor))
                {
                    // 红帽KVM太流行，细化处理
                    if (product_name == "KVM" && vendor == "Red Hat" &&
                        TryRead("/sys/class/dmi/id/product_version", out var ver) && !ver.IsNullOrEmpty())
                    {
                        var p = ver.IndexOf('(');
                        if (p > 0) ver = ver[..p].Trim();
                        Product = ver;
                    }
                }
            }
        }

        file = "/sys/class/dmi/id/product_serial";
        if (TryRead(file, out value)) Serial = value;

        // 在DMI信息内，没有太好的BoardID取值
        file = "/sys/class/dmi/id/product_sku";
        if (TryRead(file, out value) && !value.IsNullOrEmpty())
            Board = value;
        else
        {
            file = "/sys/class/dmi/id/product_family";
            if (TryRead(file, out value)) Board = value;
        }

        // 优先读取磁盘硬件标识（如 mmc-8GTF4R_0x4c4f4bb2），它来自磁盘固件，
        // 不随系统重装或重新烧写而变化；文件系统UUID重新格式化后改变，仅作回退
        var disks = GetFiles("/dev/disk/by-id", true);
        if (disks.Count > 0)
        {
            // id中需要剔除QEMU，去掉virtio-前缀，例如 virtio-uf6ag3b49w6v4e9ldgcj
            disks = disks.Where(e => !e.IsNullOrEmpty() && !e.Contains("QEMU_")).Select(e => e.TrimPrefix("virtio-")).ToList();
        }

        if (disks.Count == 0)
        {
            // 在虚拟机中，uuid可能出现一个时间id和一个guid。
            // 去掉时间id例如 2025-08-14-18-36-42-00，因为它随着时间在改变
            disks = GetFiles("/dev/disk/by-uuid", false);
            if (disks.Count > 0)
                disks = disks.Where(e => !e.IsNullOrEmpty() && (e.Length < 10 || e[4] != '-' || e[..10].ToDateTime().Year < 2000)).ToList();
        }

        if (disks.Count == 0) disks = GetFiles("/dev/disk/by-partuuid", true);

        // 排序输出，保证多次启动时顺序一致，便于比对设备身份
        if (disks.Count > 0) DiskID = disks.Where(e => !e.IsNullOrEmpty()).OrderBy(e => e, StringComparer.Ordinal).Join(",");

        // 从*-release文件读取产品信息，具有更高优先级
        file = "/etc/os-release";
        if (TryRead(file, out value))
        {
            var dic2 = value.SplitAsDictionary("=", Environment.NewLine, true);

            if (dic2.TryGetValue("Vendor", out str)) Vendor = str;
            if (dic2.TryGetValue("Product", out str)) Product = str;
            if (dic2.TryGetValue("Serial", out str)) Serial = str;
            if (dic2.TryGetValue("Board", out str)) Board = str;
        }
    }

    private void RefreshLinux()
    {
        var dic = ReadInfo("/proc/meminfo");
        if (dic != null)
        {
            if (dic.TryGetValue("MemTotal", out var str) && !str.IsNullOrEmpty())
                Memory = (UInt64)str.TrimSuffix(" kB").ToInt() * 1024;

            // MemAvailable是系统内核预测的可用内存，过低则认为不能安全分配给新进程，可能过于悲观；
            // MemFree是完全空闲的内存，未被使用的物理内存页，但内核不敢用；
            static UInt64 GetMem(IDictionary<String, String?> mem, String key)
            {
                return mem.TryGetValue(key, out var v) && !v.IsNullOrEmpty() ? (UInt64)v.TrimSuffix(" kB").ToInt() * 1024 : 0;
            }

            var ma = GetMem(dic, "MemAvailable");
            var mf = GetMem(dic, "MemFree");
            var buffers = GetMem(dic, "Buffers");
            var cached = GetMem(dic, "Cached");
            var srecl = GetMem(dic, "SReclaimable");
            var shmem = GetMem(dic, "Shmem");

            AvailableMemory = ma;

            // FreeMemory采用 free 命令的宽松口径：free + buffers + cache + SReclaimable - Shmem
            var cache = cached + srecl;
            if (cache > shmem) cache -= shmem;

            FreeMemory = mf + buffers + cache;
        }

        // A2/A4温度获取，Buildroot，CPU温度和主板温度
        if (TryRead("/sys/class/thermal/thermal_zone0/temp", out var value) ||
            TryRead("/sys/class/thermal/thermal_zone1/temp", out value))
        {
            Temperature = value.ToDouble();
            // 有时候温度会超过1000，可能是毫度。机器温度不会低于0度
            if (Temperature > 1000) Temperature /= 1000;
        }
        // respberrypi + fedora
        else if (TryRead("/sys/class/thermal/thermal_zone0/temp", out value) ||
             TryRead("/sys/class/hwmon/hwmon0/temp1_input", out value) ||
             TryRead("/sys/class/hwmon/hwmon0/temp2_input", out value) ||
             TryRead("/sys/class/hwmon/hwmon0/device/hwmon/hwmon0/temp2_input", out value) ||
             TryRead("/sys/devices/virtual/thermal/thermal_zone0/temp", out value))
        {
            Temperature = value.ToDouble() / 1000;
        }
        // A2温度获取，Ubuntu 16.04 LTS， Linux 3.4.39
        else if (TryRead("/sys/class/hwmon/hwmon0/device/temp_value", out value))
        {
            if (!value.IsNullOrEmpty()) Temperature = value.Substring(null, ":").ToDouble();
        }

        // 电池剩余
        if (TryRead("/sys/class/power_supply/BAT0/energy_now", out var energy_now) &&
            TryRead("/sys/class/power_supply/BAT0/energy_full", out var energy_full))
        {
            Battery = energy_now.ToDouble() / energy_full.ToDouble();
        }
        else if (TryRead("/sys/class/power_supply/battery/capacity", out var capacity))
        {
            Battery = capacity.ToDouble() / 100.0;
        }
        else if (Runtime.Mono)
        {
            var battery = ReadDeviceBattery();
            if (battery.TryGetValue("ChargeLevel", out var obj)) Battery = obj.ToDouble();
        }

        var file = "/proc/stat";
        if (!_excludes.Contains(nameof(CpuRate)) && File.Exists(file))
        {
            // CPU指标：user，nice, system, idle, iowait, irq, softirq
            // cpu  57057 0 14420 1554816 0 443 0 0 0 0
            try
            {
                using var reader = new StreamReader(file);
                var line = reader.ReadLine();
                if (!line.IsNullOrEmpty() && line.StartsWith("cpu"))
                {
                    var vs = line.TrimPrefix("cpu").Trim().Split(' ');
                    var current = new SystemTime
                    {
                        IdleTime = vs[3].ToLong(),
                        TotalTime = vs.Take(7).Select(e => e.ToLong()).Sum().ToLong(),
                    };

                    var idle = current.IdleTime - (_systemTime?.IdleTime ?? 0);
                    var total = current.TotalTime - (_systemTime?.TotalTime ?? 0);
                    _systemTime = current;

                    CpuRate = total == 0 ? 0 : Math.Round((Double)(total - idle) / total, 4);
                }
            }
            catch
            {
                _excludes.Add(nameof(CpuRate));
            }
        }
    }

    #region Linux辅助
    /// <summary>获取Linux发行版名称</summary>
    /// <returns>Linux发行版名称</returns>
    public static String? GetLinuxName()
    {
        var fr = "/etc/redhat-release";
        if (TryRead(fr, out var value)) return value;

        var dr = "/etc/debian-release";
        if (TryRead(dr, out value)) return value;

        var sr = "/etc/os-release";
        if (TryRead(sr, out value))
        {
            var dic = value.SplitAsDictionary("=", "\n", true);
            if (dic.TryGetValue("PRETTY_NAME", out var pretty) && !pretty.IsNullOrEmpty()) return pretty.Trim();
            if (dic.TryGetValue("NAME", out var name) && !name.IsNullOrEmpty()) return name.Trim();
        }

        var uname = "uname".Execute("-sr", 0, false)?.Trim();
        if (!uname.IsNullOrEmpty())
        {
            // 支持Android系统名
            var ss = uname.Split('-');
            foreach (var item in ss)
            {
                if (!item.IsNullOrEmpty() && item.StartsWithIgnoreCase("Android")) return item;
            }

            return uname;
        }

        return null;
    }

    private static String? GetProductByRelease()
    {
        var di = "/etc/".AsDirectory();
        if (!di.Exists) return null;

        foreach (var fi in di.GetFiles("*-release"))
        {
            if (!fi.Name.EqualIgnoreCase("redhat-release", "debian-release", "os-release", "system-release"))
            {
                var dic = File.ReadAllText(fi.FullName).SplitAsDictionary("=", "\n", true);
                if (dic.TryGetValue("BOARD", out var str)) return str;
                if (dic.TryGetValue("BOARD_NAME", out str)) return str;
            }
        }

        return null;
    }
    #endregion
}
