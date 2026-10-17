using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using NewLife;

namespace MobileConsole;

/// <summary>机器信息文本报告。桌面控制台与 Android 宿主共用同一份采集逻辑。</summary>
public static class ConsoleDump
{
    /// <summary>采集一次机器信息并生成文本报告。</summary>
    /// <param name="title">报告标题</param>
    /// <returns>可直接输出到控制台或屏幕的纯文本</returns>
    public static String Build(String title = "NewLife MobileConsole")
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== " + title + " ===");
        sb.AppendLine("采集时间: " + DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture));
        sb.AppendLine();

        var info = new MachineInfo();
        try
        {
            info.Init();

            // 首次采样无法计算 CPU 与网络速度，稍等再刷新一次
            Thread.Sleep(500);
            info.Refresh();
        }
        catch (Exception ex)
        {
            sb.AppendLine("[MachineInfo] 采集异常: " + ex.Message);
        }

        AppendSection(sb, "MachineInfo", [
            ("系统名称", info.OSName),
            ("系统版本", info.OSVersion),
            ("产品名称", info.Product),
            ("制造商", info.Vendor),
            ("处理器型号", info.Processor),
            ("硬件唯一标识", info.UUID),
            ("软件唯一标识", info.Guid),
            ("计算机序列号", info.Serial),
            ("主板", info.Board),
            ("磁盘序列号", info.DiskID),
            ("内存总量", FormatSize(info.Memory)),
            ("可用内存", FormatSize(info.AvailableMemory)),
            ("空闲内存", FormatSize(info.FreeMemory)),
            ("CPU占用率", FormatPercent(info.CpuRate)),
            ("网络上行速度", FormatSpeed(info.UplinkSpeed)),
            ("网络下行速度", FormatSpeed(info.DownlinkSpeed)),
            ("温度", FormatNumber(info.Temperature, " °C")),
            ("电池剩余", FormatPercent(info.Battery)),
        ]);

        AppendSection(sb, "运行时", [
            ("框架", RuntimeInformation.FrameworkDescription),
            ("运行时标识", RuntimeInformation.RuntimeIdentifier),
            ("OS 描述", RuntimeInformation.OSDescription),
            ("进程架构", RuntimeInformation.ProcessArchitecture + ""),
            ("Mono", Runtime.Mono + ""),
            ("Unity", Runtime.Unity + ""),
            ("容器", Runtime.Container + ""),
            ("控制台宿主", Runtime.IsConsole + ""),
            ("进程编号", Runtime.ProcessId + ""),
            ("运行时长", FormatDuration(Runtime.TickCount64)),
        ]);

        AppendSection(sb, "环境", [
            ("机器名称", Environment.MachineName),
            ("用户名称", SafeGet(() => Environment.UserName)),
            ("处理器数量", Environment.ProcessorCount + ""),
            ("64位进程", Environment.Is64BitProcess + ""),
            ("CLR 版本", Environment.Version + ""),
            ("托管内存", GC.GetTotalMemory(false).ToGMK()),
            ("工作集", Environment.WorkingSet.ToGMK()),
            ("时区", TimeZoneInfo.Local.Id),
        ]);

        AppendSection(sb, "路径", [
            ("程序目录", AppContext.BaseDirectory),
            ("当前目录", Environment.CurrentDirectory),
            ("应用数据", SafeGet(() => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))),
            ("临时目录", Path.GetTempPath()),
        ]);

        return sb.ToString();
    }

    private static void AppendSection(StringBuilder sb, String title, IEnumerable<(String Name, String? Value)> items)
    {
        sb.AppendLine("[" + title + "]");
        foreach (var (name, value) in items)
        {
            sb.AppendLine(name + ": " + value);
        }
        sb.AppendLine();
    }

    private static String SafeGet(Func<String> func)
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            return "读取失败: " + ex.Message;
        }
    }

    private static String FormatSize(UInt64 value) => value == 0 ? "0" : $"{value.ToGMK()} ({value:n0} B)";
    private static String FormatSpeed(UInt64 value) => value.ToGMK() + "/s";
    private static String FormatPercent(Double value) => (value * 100).ToString("n2", CultureInfo.InvariantCulture) + "%";
    private static String FormatNumber(Double value, String unit) => value == 0 ? "0" : value.ToString("n1", CultureInfo.InvariantCulture) + unit;

    private static String FormatDuration(Int64 milliseconds)
    {
        var span = TimeSpan.FromMilliseconds(milliseconds);
        return span.Days > 0
            ? $"{span.Days}天{span.Hours:00}:{span.Minutes:00}:{span.Seconds:00}"
            : $"{span.Hours:00}:{span.Minutes:00}:{span.Seconds:00}";
    }
}
