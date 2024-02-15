using System.Diagnostics;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Model;
using NewLife.Security;

namespace NewLife.Data;

/// <summary>雪花算法。分布式Id，业务内必须确保单例</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/snow_flake
/// 
/// 使用一个 64 bit 的 long 型的数字作为全局唯一 id。在分布式系统中的应用十分广泛，且ID 引入了时间戳，基本上保持自增。
/// 1bit保留 + 41bit时间戳 + 10bit机器 + 12bit序列号
/// 
/// 内置自动选择机器workerId，IP+进程+线程，无法绝对保证唯一，从而导致整体生成的雪花Id有一定几率重复。
/// 如果想要绝对唯一，建议在外部设置唯一的workerId，再结合单例使用，此时确保最终生成的Id绝对不重复！
/// 高要求场合，推荐使用Redis自增序数作为workerId，在大型分布式系统中亦能保证绝对唯一。
/// 已提供JoinCluster方法，用于把当前对象加入集群，确保workerId唯一。
/// </remarks>
public class Snowflake
{
    #region 属性
    /// <summary>开始时间戳。首次使用前设置，否则无效，默认1970-1-1</summary>
    /// <remarks>
    /// 该时间戳默认已带有时区偏移，不管是为本地时间还是UTC时间生成雪花Id，都是一样的时间大小。
    /// 默认值本质上就是UTC 1970-1-1，转本地时间是为了方便解析雪花Id时得到的时间就是本地时间，最大兼容已有业务。
    /// 在星尘和IoT的自动分表场景中，一般需要用本地时间来作为分表依据，所以默认值是本地时间。
    /// </remarks>
    public DateTime StartTimestamp { get; set; } = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToLocalTime();

    /// <summary>机器Id，取10位</summary>
    /// <remarks>
    /// 内置默认取IP+进程+线程，不能保证绝对唯一，要求高的场合建议外部保证workerId唯一。
    /// 一般借助Redis自增序数作为workerId，确保绝对唯一。
    /// 如果应用接入星尘，将自动从星尘配置中心获取workerId，确保全局唯一。
    /// </remarks>
    public Int32 WorkerId { get; set; }

    private Int32 _Sequence;
    /// <summary>序列号，取12位。进程内静态，避免多个实例生成重复Id</summary>
    public Int32 Sequence => _Sequence;

    /// <summary>全局机器Id。若设置，所有雪花实例都将使用该Id，可以由星尘配置中心提供本应用全局唯一机器码，且跨多环境唯一</summary>
    public static Int32 GlobalWorkerId { get; set; }

    /// <summary>workerId分配集群。配置后可确保所有实例化的雪花对象得到唯一workerId，建议使用Redis</summary>
    public static ICache? Cluster { get; set; }

    private Int64 _msStart;
    private Stopwatch _watch = null!;
    private Int64 _lastTime;
    #endregion

    #region 构造
    private static Int32 _gid;
    private static readonly Int32 _instance;
    static Snowflake()
    {
        try
        {
            // 从容器中获取缓存提供者，查找Redis作为集群WorkerId分配器
            var provider = ObjectContainer.Provider?.GetService<ICacheProvider>();
            if (provider != null && provider.Cache != provider.InnerCache && provider is not MemoryCache)
                Cluster = provider.Cache;

            var ip = NetHelper.MyIP();
            if (ip != null)
            {
                var buf = ip.GetAddressBytes();
                _instance = (buf[2] << 8) | buf[3];
            }
            else
            {
                _instance = Rand.Next(1, 1024);
            }
        }
        catch
        {
            // 异常时随机
            _instance = Rand.Next(1, 1024);
        }
    }
    #endregion

    #region 核心方法
    private Boolean _inited;
    private void Init()
    {
        if (_inited) return;
        lock (this)
        {
            if (_inited) return;

            // 记录雪花算法初始化埋点，及时发现算法使用错误
            using var span = DefaultTracer.Instance?.NewSpan("Snowflake-Init", new { id = Interlocked.Increment(ref _gid) });

            if (WorkerId <= 0 && GlobalWorkerId > 0) WorkerId = GlobalWorkerId & 0x3FF;
            if (WorkerId <= 0 && Cluster != null) JoinCluster(Cluster);

            // 初始化WorkerId，取5位实例加上5位进程，确保同一台机器的WorkerId不同
            if (WorkerId <= 0)
            {
                var nodeId = _instance;
                var pid = Process.GetCurrentProcess().Id;
                var tid = Thread.CurrentThread.ManagedThreadId;
                //WorkerId = ((nodeId & 0x1F) << 5) | (pid & 0x1F);
                //WorkerId = (nodeId ^ pid ^ tid) & 0x3FF;
                WorkerId = ((nodeId & 0x1F) << 5) | ((pid ^ tid) & 0x1F);
            }

            // 记录此时距离起点的毫秒数以及开机嘀嗒数
            if (_watch == null)
            {
                var now = ConvertKind(DateTime.Now);
                _msStart = (Int64)(now - StartTimestamp).TotalMilliseconds;
                _watch = Stopwatch.StartNew();
            }

            span?.AppendTag($"WorkerId={WorkerId} StartTimestamp={StartTimestamp.ToFullString()} _msStart={_msStart}");

            _inited = true;
        }
    }

    /// <summary>获取下一个Id</summary>
    /// <remarks>基于当前时间，转StartTimestamp所属时区后，生成Id</remarks>
    /// <returns></returns>
    public virtual Int64 NewId()
    {
        Init();

        // 此时嘀嗒数减去起点嘀嗒数，加上起点毫秒数
        var ms = _watch.ElapsedMilliseconds + _msStart;
        var wid = WorkerId & (-1 ^ (-1 << 10));
        var seq = Interlocked.Increment(ref _Sequence) & (-1 ^ (-1 << 12));

        //!!! 避免时间倒退
        var t = _lastTime - ms;
        if (t > 0)
        {
            // 多线程生成Id的时候，_lastTime在计算差值前被另一个线程更新了，导致时间有微小偏差（一般是1ms）
            //XTrace.WriteLine("Snowflake时间倒退，时间差 {0}ms", t);
            if (t > 10_000) throw new InvalidOperationException($"Time reversal too large ({t}ms)To ensure uniqueness, Snowflake refuses to generate a new Id");

            ms = _lastTime;
        }

        // 相同毫秒内，如果序列号用尽，则可能超过4096，导致生成重复Id
        // 睡眠1毫秒，抢占它的位置 @656092719（广西-风吹面）
        if (ms == _lastTime && seq == 0)
        {
            // spin等1000次耗时141us，10000次耗时397us，100000次耗时3231us。@i9-10900k
            //Thread.SpinWait(1000);
            while (ms <= _lastTime) ms = _watch.ElapsedMilliseconds + _msStart;
        }
        _lastTime = ms;

        /*
         * 每个毫秒内_Sequence没有归零，主要是为了安全，避免被人猜测得到前后Id。
         * 而毫秒内的顺序，重要性不大。
         */

        return (ms << (10 + 12)) | (Int64)(wid << 12) | (Int64)seq;
    }

    /// <summary>获取指定时间的Id，带上节点和序列号。可用于根据业务时间构造插入Id</summary>
    /// <remarks>
    /// 基于指定时间，转StartTimestamp所属时区后，生成Id。
    /// 
    /// 如果为指定毫秒时间生成多个Id（超过4096），则可能重复。
    /// </remarks>
    /// <param name="time">时间</param>
    /// <returns></returns>
    public virtual Int64 NewId(DateTime time)
    {
        Init();

        time = ConvertKind(time);

        var ms = (Int64)(time - StartTimestamp).TotalMilliseconds;
        var wid = WorkerId & (-1 ^ (-1 << 10));
        var seq = Interlocked.Increment(ref _Sequence) & (-1 ^ (-1 << 12));

        return (ms << (10 + 12)) | (Int64)(wid << 12) | (Int64)seq;
    }

    /// <summary>获取指定时间的Id，传入唯一业务id（取模为10位）。可用于物联网数据采集，每1024个传感器为一组，每组每毫秒多个Id</summary>
    /// <remarks>
    /// 基于指定时间，转StartTimestamp所属时区后，生成Id。
    /// 
    /// 在物联网数据采集中，数据分析需要，更多希望能够按照采集时间去存储。
    /// 为了避免主键重复，可以使用传感器id作为workerId。
    /// uid需要取模为10位，即按1024分组，每组每毫秒最多生成4096个Id。
    /// 
    /// 如果为指定分组在特定毫秒时间生成多个Id（超过4096），则可能重复。
    /// </remarks>
    /// <param name="time">时间</param>
    /// <param name="uid">唯一业务id。例如传感器id</param>
    /// <returns></returns>
    public virtual Int64 NewId(DateTime time, Int32 uid)
    {
        Init();

        time = ConvertKind(time);

        // 业务id作为workerId，保留12位序列号。即传感器按1024分组，每组每毫秒最多生成4096个Id
        var ms = (Int64)(time - StartTimestamp).TotalMilliseconds;
        var wid = uid & (-1 ^ (-1 << 10));
        var seq = Interlocked.Increment(ref _Sequence) & (-1 ^ (-1 << 12));

        return (ms << (10 + 12)) | (Int64)(wid << 12) | (Int64)seq;
    }

    /// <summary>获取指定时间的Id，传入唯一业务id（22位）。可用于物联网数据采集，每4194304个传感器一组，每组每毫秒1个Id</summary>
    /// <remarks>
    /// 基于指定时间，转StartTimestamp所属时区后，生成Id。
    /// 
    /// 在物联网数据采集中，数据分析需要，更多希望能够按照采集时间去存储。
    /// 为了避免主键重复，可以使用传感器id作为workerId。
    /// 再配合upsert写入数据，如果同一个毫秒内传感器有多行数据，则只会插入一行。
    /// 
    /// 如果为指定业务id在特定毫秒时间生成多个Id（超过1个），则可能重复。
    /// </remarks>
    /// <param name="time">时间</param>
    /// <param name="uid">唯一业务id。例如传感器id</param>
    /// <returns></returns>
    public virtual Int64 NewId22(DateTime time, Int32 uid)
    {
        Init();

        time = ConvertKind(time);

        // 业务id作为workerId，不保留序列号。即传感器按4194304（1<<22）分组，每组每毫秒最多生成1个Id
        var ms = (Int64)(time - StartTimestamp).TotalMilliseconds;
        var wid = uid & (-1 ^ (-1 << 22));

        return (ms << (10 + 12)) | (Int64)wid;
    }

    /// <summary>时间转为Id，不带节点和序列号。可用于构建时间片段查询</summary>
    /// <remarks>
    /// 基于指定时间，转StartTimestamp所属时区后，生成不带WorkerId和序列号的Id。
    /// 一般用于构建时间片段查询，例如查询某个时间段内的数据，把时间片段转为雪花Id片段。
    /// </remarks>
    /// <param name="time">时间</param>
    /// <returns></returns>
    public virtual Int64 GetId(DateTime time)
    {
        time = ConvertKind(time);
        var t = (Int64)(time - StartTimestamp).TotalMilliseconds;
        return t << (10 + 12);
    }

    /// <summary>解析雪花Id，得到时间、WorkerId和序列号</summary>
    /// <remarks>
    /// 其中的时间是StartTimestamp所属时区的时间。
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="time">时间</param>
    /// <param name="workerId">节点</param>
    /// <param name="sequence">序列号</param>
    /// <returns></returns>
    public virtual Boolean TryParse(Int64 id, out DateTime time, out Int32 workerId, out Int32 sequence)
    {
        time = StartTimestamp.AddMilliseconds(id >> (10 + 12));
        workerId = (Int32)((id >> 12) & 0x3FF);
        sequence = (Int32)(id & 0x0FFF);

        return true;
    }

    /// <summary>把输入时间转为开始时间戳的类型，便于相减</summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public DateTime ConvertKind(DateTime time)
    {
        return StartTimestamp.Kind switch
        {
            DateTimeKind.Utc => time.ToUniversalTime(),
            DateTimeKind.Local => time.ToLocalTime(),
            _ => time,
        };
    }
    #endregion

    #region 集群扩展
    /// <summary>加入集群。由集群统一分配WorkerId，确保唯一，从而保证生成的雪花Id绝对唯一</summary>
    /// <param name="cache"></param>
    /// <param name="key"></param>
    public virtual void JoinCluster(ICache cache, String key = "SnowflakeWorkerId")
    {
        var wid = (Int32)cache.Increment(key, 1);
        WorkerId = wid & 0x3FF;
    }
    #endregion
}