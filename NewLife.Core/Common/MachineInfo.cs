using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using System.Reflection;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife;

/// <summary>机器信息接口</summary>
/// <remarks>用于扩展MachineInfo功能，具体应用自定义各字段获取方式</remarks>
public interface IMachineInfo
{
    /// <summary>初始化静态数据</summary>
    /// <param name="info">机器信息实例</param>
    void Init(MachineInfo info);

    /// <summary>刷新动态数据</summary>
    /// <param name="info">机器信息实例</param>
    void Refresh(MachineInfo info);
}

/// <summary>机器信息</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/machine_info
/// 
/// 刷新信息成本较高，建议采用单例模式
/// </remarks>
public partial class MachineInfo : IExtend
{
    #region 属性
    /// <summary>系统名称</summary>
    [DisplayName("系统名称")]
    public String? OSName { get; set; }

    /// <summary>系统版本</summary>
    [DisplayName("系统版本")]
    public String? OSVersion { get; set; }

    /// <summary>产品名称</summary>
    [DisplayName("产品名称")]
    public String? Product { get; set; }

    /// <summary>制造商</summary>
    [DisplayName("制造商")]
    public String? Vendor { get; set; }

    /// <summary>处理器型号</summary>
    [DisplayName("处理器型号")]
    public String? Processor { get; set; }

    ///// <summary>处理器序列号。PC处理器序列号绝大部分重复，实际存储处理器的其它信息</summary>
    //public String CpuID { get; set; }

    /// <summary>硬件唯一标识。取主板编码，部分品牌存在重复</summary>
    [DisplayName("硬件唯一标识")]
    public String? UUID { get; set; }

    /// <summary>软件唯一标识。系统标识，操作系统重装后更新，Linux系统的machine_id，Android的android_id，Ghost系统存在重复</summary>
    [DisplayName("软件唯一标识")]
    public String? Guid { get; set; }

    /// <summary>计算机序列号。适用于品牌机，跟笔记本标签显示一致</summary>
    [DisplayName("计算机序列号")]
    public String? Serial { get; set; }

    /// <summary>主板。序列号或家族信息</summary>
    [DisplayName("主板")]
    public String? Board { get; set; }

    /// <summary>磁盘序列号</summary>
    [DisplayName("磁盘序列号")]
    public String? DiskID { get; set; }

    /// <summary>内存总量。单位Byte</summary>
    [DisplayName("内存总量")]
    public UInt64 Memory { get; set; }

    /// <summary>可用内存。单位Byte</summary>
    /// <remarks>
    /// Linux：优先使用 /proc/meminfo 中的 MemAvailable，表示在不触发大量换页或 OOM 的前提下，
    /// 内核评估仍可安全分配的内存，适合作为应用自我保护（限流/拒绝新任务）以及监控告警阈值的主要依据。
    /// Windows：对应 GlobalMemoryStatusEx.ullAvailPhys，表示当前可用的物理内存。
    /// </remarks>
    [DisplayName("可用内存")]
    public UInt64 AvailableMemory { get; set; }

    /// <summary>空闲内存。单位Byte</summary>
    /// <remarks>
    /// Linux：采用 free 命令的宽松口径计算：MemFree + Buffers + Cached + SReclaimable - Shmem，
    /// 表示当前看起来空闲或可快速回收的内存，适合用于监控展示和人工分析整体内存使用情况。
    /// Windows：与 AvailableMemory 保持一致，均使用物理可用内存；进行安全可用性判断时应优先参考 AvailableMemory。
    /// </remarks>
    [DisplayName("空闲内存")]
    public UInt64 FreeMemory { get; set; }

    /// <summary>CPU占用率</summary>
    [DisplayName("CPU占用率")]
    public Double CpuRate { get; set; }

    /// <summary>网络上行速度。字节每秒，初始化后首次读取为0</summary>
    [DisplayName("网络上行速度")]
    public UInt64 UplinkSpeed { get; set; }

    /// <summary>网络下行速度。字节每秒，初始化后首次读取为0</summary>
    [DisplayName("网络下行速度")]
    public UInt64 DownlinkSpeed { get; set; }

    /// <summary>温度。单位度</summary>
    [DisplayName("温度")]
    public Double Temperature { get; set; }

    /// <summary>电池剩余。小于1的小数，常用百分比表示</summary>
    [DisplayName("电池剩余")]
    public Double Battery { get; set; }

    private readonly Dictionary<String, Object?> _items = [];
    IDictionary<String, Object?> IExtend.Items => _items;

    /// <summary>获取 或 设置 扩展属性数据</summary>
    /// <param name="key">属性键名</param>
    /// <returns>属性值</returns>
    public Object? this[String key] { get => _items.TryGetValue(key, out var obj) ? obj : null; set => _items[key] = value; }
    #endregion

    #region 全局静态
    /// <summary>当前机器信息。默认null，在RegisterAsync后才能使用</summary>
    public static MachineInfo? Current { get; set; }

    /// <summary>机器信息提供者。外部实现可修改部分行为</summary>
    public static IMachineInfo? Provider { get; set; }

    //static MachineInfo() => RegisterAsync().Wait(100);

    private static Task<MachineInfo>? _task;
    /// <summary>异步注册一个初始化后的机器信息实例</summary>
    /// <returns>初始化后的机器信息实例</returns>
    public static Task<MachineInfo> RegisterAsync()
    {
        if (_task != null) return _task;

        return _task = Task.Factory.StartNew(() =>
        {
            var set = Setting.Current;
            var dataPath = set.DataPath;
            if (dataPath.IsNullOrEmpty()) dataPath = "Data";

            // 文件缓存，加快机器信息获取。在Linux下，可能StarAgent以root权限写入缓存文件，其它应用以普通用户访问
            var file = Path.GetTempPath().CombinePath("machine_info.json");
            var file2 = dataPath.CombinePath("machine_info.json").GetBasePath();
            var json = "";
            if (Current == null)
            {
                var f = file;
                if (!File.Exists(f)) f = file2;
                if (File.Exists(f))
                {
                    try
                    {
                        //XTrace.WriteLine("Load MachineInfo {0}", f);
                        json = File.ReadAllText(f);
                        Current = json.ToJsonEntity<MachineInfo>();
                    }
                    catch (Exception ex)
                    {
                        if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteException(ex);
                    }
                }
            }

            var mi = Current ?? new MachineInfo();

            mi.Init();
            Current = mi;

            // 注册到对象容器
            ObjectContainer.Current.AddSingleton(mi);

            try
            {
                var json2 = mi.ToJson(true);
                if (json != json2)
                {
                    File.WriteAllText(file2.EnsureDirectory(true), json2);
                    File.WriteAllText(file.EnsureDirectory(true), json2);
                }
            }
            catch (Exception ex)
            {
                if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteException(ex);
            }

            return mi;
        }, TaskCreationOptions.LongRunning);
    }

    /// <summary>获取当前信息，如果未设置则等待异步注册结果</summary>
    /// <returns>当前机器信息实例</returns>
    public static MachineInfo GetCurrent() => Current ?? RegisterAsync().ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>从对象容器中获取一个已注册机器信息实例</summary>
    /// <returns>机器信息实例</returns>
    public static MachineInfo? Resolve() => ObjectContainer.Current.GetService<MachineInfo>();
    #endregion

    #region 方法
    /// <summary>初始化静态数据。可能是实例化后执行，也可能是Json反序列化后执行</summary>
    public void Init()
    {
        var osv = Environment.OSVersion;
        if (OSVersion.IsNullOrEmpty()) OSVersion = osv.Version + "";
        if (OSName.IsNullOrEmpty()) OSName = (osv + "").TrimPrefix("Microsoft").TrimSuffix(OSVersion).Trim();
        if (Guid.IsNullOrEmpty()) Guid = "";

        try
        {
#if NET5_0_OR_GREATER
            if (OperatingSystem.IsWindows())
                LoadWindowsInfo();
            else if (OperatingSystem.IsAndroid())
                LoadAndroidInfo();
            else if (OperatingSystem.IsLinux())
                LoadLinuxInfo();
            else if (OperatingSystem.IsMacOS())
                LoadMacInfo();
#else
            if (Runtime.Windows)
                LoadWindowsInfo();
            else if (Runtime.Linux)
                LoadLinuxInfo();
#endif

            Provider?.Init(this);
        }
        catch (Exception ex)
        {
            if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteException(ex);
        }

        // 裁剪不可见字符，顺带去掉两头空白
        OSName = Clean(OSName);
        OSVersion = Clean(OSVersion);
        Product = Clean(Product);
        Processor = Clean(Processor);
        UUID = Clean(UUID);
        Guid = Clean(Guid);
        Serial = Clean(Serial);
        Board = Clean(Board);
        DiskID = Clean(DiskID);

        // 无法读取系统标识时，随机生成一个guid，借助文件缓存确保其不变
        if (Guid.IsNullOrEmpty()) Guid = "0-" + System.Guid.NewGuid().ToString();
        if (UUID.IsNullOrEmpty()) UUID = "0-" + System.Guid.NewGuid().ToString();

        try
        {
            Refresh();
        }
        catch (Exception ex)
        {
            if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteException(ex);
        }
    }

    /// <summary>裁剪不可见字符并去除两端空白</summary>
    private static String? Clean(String? value) => value.TrimInvisible()?.Trim();

    private readonly ICollection<String> _excludes = [];

    /// <summary>获取实时数据，如CPU、内存、温度</summary>
    public void Refresh()
    {
        if (Runtime.Windows)
            RefreshWindows();
#if NET5_0_OR_GREATER
        else if (OperatingSystem.IsAndroid())
            RefreshAndroid();
#endif
        // 特别识别Linux发行版
        else if (Runtime.Linux)
            RefreshLinux();

        RefreshSpeed();

        Provider?.Refresh(this);
    }

    private Int64 _lastTime;
    private Int64 _lastSent;
    private Int64 _lastReceived;
    /// <summary>刷新网络速度</summary>
    public void RefreshSpeed()
    {
        var sent = 0L;
        var received = 0L;
        try
        {
            // 包含本地环回和隧道网卡
            // WSL获取网络列表时可能报错
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                try
                {
                    var st = ni.GetIPStatistics();
                    sent += st.BytesSent;
                    received += st.BytesReceived;
                }
                catch { }
            }
        }
        catch { }

        var now = Runtime.TickCount64;
        if (_lastTime > 0)
        {
            var interval = now - _lastTime;
            if (interval > 0)
            {
                var s1 = (sent - _lastSent) * 1000 / interval;
                var s2 = (received - _lastReceived) * 1000 / interval;
                if (s1 >= 0) UplinkSpeed = (UInt64)s1;
                if (s2 >= 0) DownlinkSpeed = (UInt64)s2;
            }
        }

        _lastSent = sent;
        _lastReceived = received;
        _lastTime = now;
    }
    #endregion

    #region 辅助
    private static Boolean TryRead(String fileName, [NotNullWhen(true)] out String? value)
    {
        value = null;

        if (!File.Exists(fileName)) return false;

        try
        {
            value = File.ReadAllText(fileName)?.Trim();
            if (value.IsNullOrEmpty()) return false;
        }
        catch { return false; }

        return true;
    }

    /// <summary>读取文件信息，分割为字典</summary>
    /// <param name="file">文件路径</param>
    /// <param name="separate">分隔符</param>
    /// <returns>解析后的字典</returns>
    public static IDictionary<String, String?>? ReadInfo(String file, Char separate = ':')
    {
        if (file.IsNullOrEmpty() || !File.Exists(file)) return null;

        var dic = new NullableDictionary<String, String?>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(file);
        while (!reader.EndOfStream)
        {
            // 按行读取
            var line = reader.ReadLine();
            if (line != null)
            {
                // 分割
                var p = line.IndexOf(separate);
                if (p > 0)
                {
                    var key = line[..p].Trim();
                    var value = line[(p + 1)..].Trim();
                    dic[key] = value.TrimInvisible();
                }
            }
        }

        return dic;
    }

    /// <summary>获取设备信息。用于Xamarin</summary>
    /// <returns>设备信息字典</returns>
    /// <returns></returns>
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "设备信息经 WMI/系统对象反射读取（运行时类型）；读取失败降级为空，不参与业务主链")]
#if NET5_0_OR_GREATER
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, "Android.OS.Build", "Mono.Android")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods, "Android.Provider.Settings/Secure", "Mono.Android")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, "Android.App.Application", "Mono.Android")]
#endif
    public static IDictionary<String, String?> ReadDeviceInfo()
    {
        var dic = new Dictionary<String, String?>();
#if NET5_0_OR_GREATER
        // .NET for Android 使用 Mono 运行时，但不一定暴露 Mono.Runtime 类型
        if (!Runtime.Mono && !OperatingSystem.IsAndroid()) return dic;
#else
        if (!Runtime.Mono) return dic;
#endif

        {
            var type = "Android.OS.Build".GetTypeEx();
            if (type != null)
            {
                foreach (var item in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
                {
                    try
                    {
                        dic[item.Name] = item.GetValue(null) + "";
                    }
                    catch { }
                }
            }
        }
        {
            var type = "Xamarin.Essentials.DeviceInfo".GetTypeEx();
            if (type != null)
            {
                foreach (var item in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
                {
                    try
                    {
                        dic[item.Name] = item.GetValue(null) + "";
                    }
                    catch { }
                }
            }
        }
        {
            var type = "Android.Provider.Settings".GetTypeEx()?.GetNestedType("Secure");
            if (type != null)
            {
                var resolver = "Android.App.Application".GetTypeEx()?.GetValue("Context")?.GetValue("ContentResolver");
                if (resolver != null)
                {
                    var name = "android_id";
                    dic[name] = type.Invoke("GetString", resolver, name) as String;
                }
            }
        }

        return dic;
    }

    /// <summary>获取设备电量。用于 Xamarin</summary>
    /// <returns>设备电量信息字典</returns>
    /// <returns></returns>
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "电池/设备信息经 WMI 对象反射读取（运行时类型）；读取失败降级为空，不参与业务主链")]
    public static IDictionary<String, Object?> ReadDeviceBattery()
    {
        var dic = new Dictionary<String, Object?>();
        if (!Runtime.Mono) return dic;

        var type = "Xamarin.Essentials.Battery".GetTypeEx();
        if (type == null) return dic;

        foreach (var item in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            try
            {
                dic[item.Name] = item.GetValue(null);
            }
            catch { }
        }

        return dic;
    }
    #endregion

    #region 磁盘
    /// <summary>获取指定目录所在盘可用空间，默认当前目录</summary>
    /// <param name="path">目录路径</param>
    /// <returns>返回可用空间，字节，获取失败返回-1</returns>
    public static Int64 GetFreeSpace(String? path = null)
    {
        if (path.IsNullOrEmpty()) path = ".";
        var root = Path.GetPathRoot(path.GetFullPath());
        if (root.IsNullOrEmpty()) return 0;

        var driveInfo = new DriveInfo(root);
        if (driveInfo == null || !driveInfo.IsReady) return -1;

        try
        {
            return driveInfo.AvailableFreeSpace;
        }
        catch
        {
            return -1;
        }
    }

    /// <summary>获取指定目录下文件名，支持去掉后缀的去重，主要用于Linux</summary>
    /// <param name="path">目录路径</param>
    /// <param name="trimSuffix">是否去掉后缀进行去重</param>
    /// <returns>文件名集合</returns>
    public static ICollection<String> GetFiles(String path, Boolean trimSuffix = false)
    {
        var list = new List<String>();
        if (path.IsNullOrEmpty()) return list;

        var di = path.AsDirectory();
        if (!di.Exists) return list;

        var list2 = di.GetFiles().Select(e => e.Name).ToList();
        foreach (var item in list2)
        {
            var line = item?.Trim();
            if (!line.IsNullOrEmpty())
            {
                // 前面出现 virtio-uf6fv7fzp6fm1b91fli8 ，后面出现 virtio-uf6fv7fzp6fm1b91fli8-part1
                if (trimSuffix)
                {
                    if (!list2.Any(e => e != line && line.StartsWith(e))) list.Add(line);
                }
                else
                {
                    list.Add(line);
                }
            }
        }

        return list;
    }
    #endregion

    #region Windows辅助
    private class SystemTime
    {
        public Int64 IdleTime;
        public Int64 TotalTime;
    }

    private SystemTime? _systemTime;
    #endregion
}