using System.Diagnostics;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Model;
using NewLife.Security;

namespace NewLife.Data;

/// <summary>雪花算法。分布式Id生成器，业务内必须确保单例</summary>
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
/// 
/// 务必请保证Snowflake对象的唯一性，Snowflake确保本对象生成的Id绝对唯一，但如果有多个Snowflake对象，可能会生成重复Id。
/// 特别在使用XCode等数据中间件时，要确保每张表只有一个Snowflake实例。
/// </remarks>
public class Snowflake
{
    #region 静态常量
    /// <summary>工作节点ID位数</summary>
    private const Int32 WorkerIdBits = 10;

    /// <summary>序列号位数</summary>  
    private const Int32 SequenceBits = 12;

    /// <summary>最大工作节点ID</summary>
    private const Int32 MaxWorkerId = (1 << WorkerIdBits) - 1;

    /// <summary>最大序列号</summary>
    private const Int32 MaxSequence = (1 << SequenceBits) - 1;

    /// <summary>时间戳左移位数</summary>
    private const Int32 TimestampShift = WorkerIdBits + SequenceBits;

    /// <summary>工作节点ID左移位数</summary>
    private const Int32 WorkerIdShift = SequenceBits;

    /// <summary>时间回拨最大容忍度（毫秒）</summary>
    private const Int64 MaxClockBack = 3600_000 + 10_000; // 夏令时最大1小时
    #endregion

    #region 属性
    /// <summary>开始时间戳。首次使用前设置，否则无效，默认1970-1-1</summary>
    /// <remarks>
    /// 该时间戳默认已带有时区偏移，不管是为本地时间还是UTC时间生成雪花Id，都是一样的时间大小。
    /// 默认值本质上就是UTC 1970-1-1，转本地时间是为了方便解析雪花Id时得到的时间就是本地时间，最大兼容已有业务。
    /// 在星尘和IoT的自动分表场景中，一般需要用本地时间来作为分表依据，所以默认值是本地时间。
    /// </remarks>
    public DateTime StartTimestamp { get; set; } = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToLocalTime();

    private Int32 _workerId;
    /// <summary>机器Id，取10位（0-1023）</summary>
    /// <remarks>
    /// 内置默认取IP+进程+线程，不能保证绝对唯一，要求高的场合建议外部保证workerId唯一。
    /// 一般借助Redis自增序数作为workerId，确保绝对唯一。
    /// 如果应用接入星尘，将自动从星尘配置中心获取workerId，确保全局唯一。
    /// </remarks>
    public Int32 WorkerId
    {
        get => _workerId;
        set
        {
            if (value is < 0 or > MaxWorkerId)
                throw new ArgumentOutOfRangeException(nameof(value), $"WorkerId必须在0-{MaxWorkerId}范围内");
            _workerId = value;
        }
    }

    private Int32 _sequence;
    /// <summary>序列号，取12位（0-4095）。进程内静态，避免多个实例生成重复Id</summary>
    public Int32 Sequence => _sequence;

    /// <summary>全局机器Id。若设置，所有雪花实例都将使用该Id，可以由星尘配置中心提供本应用全局唯一机器码，且跨多环境唯一</summary>
    public static Int32 GlobalWorkerId { get; set; }

    /// <summary>workerId分配集群。配置后可确保所有实例化的雪花对象得到唯一workerId，建议使用Redis</summary>
    public static ICache? Cluster { get; set; }

    private Int64 _lastTimestamp;
    #endregion

    #region 构造函数
    private static Int32 _globalInstanceId;
    private static readonly Int32 _defaultInstanceId;

    static Snowflake()
    {
        try
        {
            // 从容器中获取缓存提供者，查找Redis作为集群WorkerId分配器
            var provider = ObjectContainer.Provider?.GetService<ICacheProvider>();
            if (provider is { Cache: not MemoryCache } && provider.Cache != provider.InnerCache)
                Cluster = provider.Cache;

            // 基于IP地址的默认实例ID
            var ip = NetHelper.MyIP();
            if (ip != null)
            {
                var buf = ip.GetAddressBytes();
                _defaultInstanceId = (buf[2] << 8) | buf[3];
            }
            else
            {
                _defaultInstanceId = Rand.Next(1, 1024);
            }
        }
        catch
        {
            // 异常时使用随机值
            _defaultInstanceId = Rand.Next(1, 1024);
        }
    }
    #endregion

    #region 核心方法
    private Boolean _initialized;
    private readonly Object _lockObject = new();

    /// <summary>初始化WorkerId</summary>
    private void Initialize()
    {
        if (_initialized) return;

        lock (_lockObject)
        {
            if (_initialized) return;

            // 记录雪花算法初始化埋点，及时发现算法使用错误
            using var span = DefaultTracer.Instance?.NewSpan("Snowflake-Init", new { id = Interlocked.Increment(ref _globalInstanceId) });

            // 按优先级设置WorkerId
            if (WorkerId <= 0 && GlobalWorkerId > 0)
                WorkerId = GlobalWorkerId & MaxWorkerId;

            if (WorkerId <= 0 && Cluster != null)
                JoinCluster(Cluster);

            // 初始化WorkerId，取5位实例加上5位进程，确保同一台机器的WorkerId不同
            if (WorkerId <= 0)
            {
                var nodeId = _defaultInstanceId;
                var pid = Process.GetCurrentProcess().Id;
                var tid = Thread.CurrentThread.ManagedThreadId;
                //WorkerId = ((nodeId & 0x1F) << 5) | (pid & 0x1F);
                //WorkerId = (nodeId ^ pid ^ tid) & 0x3FF;
                WorkerId = ((nodeId & 0x1F) << 5) | ((pid ^ tid) & 0x1F);
            }

            span?.AppendTag($"WorkerId={WorkerId} StartTimestamp={StartTimestamp.ToFullString()}");
            _initialized = true;
        }
    }

    /// <summary>获取下一个Id</summary>
    /// <remarks>基于当前时间，转StartTimestamp所属时区后，生成Id</remarks>
    /// <returns>雪花Id</returns>
    public virtual Int64 NewId()
    {
        Initialize();

        // 此时嘀嗒数减去起点嘀嗒数，加上起点毫秒数
        var currentTimestamp = (Int64)(ConvertKind(DateTime.Now) - StartTimestamp).TotalMilliseconds;
        var workerId = WorkerId & MaxWorkerId;

        // 获取时间戳，处理时间回拨
        var lastTime = Volatile.Read(ref _lastTimestamp);
        var timestamp = currentTimestamp;
        if (currentTimestamp < lastTime)
        {
            // 检测时间回拨
            var clockBack = lastTime - currentTimestamp;
            if (clockBack > MaxClockBack)
                throw new InvalidOperationException($"时间回拨过大 ({clockBack}ms)。为保证唯一性，雪花算法拒绝生成新Id");

            // 使用上次时间戳，等待时间追上
            timestamp = lastTime;
        }

        // 生成序列号并构建ID
        var sequence = 0;
        lock (_lockObject)
        {
            while (true)
            {
                if (timestamp > _lastTimestamp)
                {
                    // 时间推进，重置序列号
                    _sequence = 0;
                    _lastTimestamp = timestamp;
                    sequence = 0;
                    break;
                }

                if (timestamp == _lastTimestamp)
                {
                    // 同一毫秒内，递增序列号
                    sequence = Interlocked.Increment(ref _sequence);
                    if (sequence <= MaxSequence) break;

                    // 序列号溢出，等待下一毫秒
                    timestamp = _lastTimestamp + 1;
                    _sequence = 0;
                }
                else
                {
                    // 时间未变化，使用当前时间戳
                    timestamp = _lastTimestamp;
                }
            }
        }

        return BuildSnowflakeId(timestamp, workerId, sequence);
    }

    /// <summary>获取指定时间的Id，带上节点和序列号。可用于根据业务时间构造插入Id</summary>
    /// <remarks>
    /// 基于指定时间，转StartTimestamp所属时区后，生成Id。
    /// 如果为指定毫秒时间生成多个Id（超过4096），则可能重复。
    /// </remarks>
    /// <param name="time">时间</param>
    /// <returns>雪花Id</returns>
    public virtual Int64 NewId(DateTime time)
    {
        Initialize();

        time = ConvertKind(time);
        var timestamp = (Int64)(time - StartTimestamp).TotalMilliseconds;
        var workerId = WorkerId & MaxWorkerId;
        var sequence = Interlocked.Increment(ref _sequence) & MaxSequence;

        return BuildSnowflakeId(timestamp, workerId, sequence);
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
    /// <returns>雪花Id</returns>
    public virtual Int64 NewId(DateTime time, Int32 uid)
    {
        Initialize();

        time = ConvertKind(time);

        // 业务id作为workerId，保留12位序列号。即传感器按1024分组，每组每毫秒最多生成4096个Id
        var timestamp = (Int64)(time - StartTimestamp).TotalMilliseconds;
        var workerId = uid & MaxWorkerId;
        var sequence = Interlocked.Increment(ref _sequence) & MaxSequence;

        return BuildSnowflakeId(timestamp, workerId, sequence);
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
    /// <returns>雪花Id</returns>
    public virtual Int64 NewId22(DateTime time, Int32 uid)
    {
        Initialize();

        time = ConvertKind(time);

        // 业务id作为workerId，不保留序列号。即传感器按4194304（1<<22）分组，每组每毫秒最多生成1个Id
        var timestamp = (Int64)(time - StartTimestamp).TotalMilliseconds;
        var workerId = uid & ((1 << 22) - 1); // 22位业务ID

        return (timestamp << TimestampShift) | (Int64)workerId;
    }

    /// <summary>时间转为Id，不带节点和序列号。可用于构建时间片段查询</summary>
    /// <remarks>
    /// 基于指定时间，转StartTimestamp所属时区后，生成不带WorkerId和序列号的Id。
    /// 一般用于构建时间片段查询，例如查询某个时间段内的数据，把时间片段转为雪花Id片段。
    /// </remarks>
    /// <param name="time">时间</param>
    /// <returns>时间部分的Id</returns>
    public virtual Int64 GetId(DateTime time)
    {
        time = ConvertKind(time);
        var timestamp = (Int64)(time - StartTimestamp).TotalMilliseconds;
        return timestamp << TimestampShift;
    }

    /// <summary>解析雪花Id，得到时间、WorkerId和序列号</summary>
    /// <remarks>
    /// 其中的时间是StartTimestamp所属时区的时间。
    /// </remarks>
    /// <param name="id">雪花Id</param>
    /// <param name="time">解析出的时间</param>
    /// <param name="workerId">解析出的工作节点Id</param>
    /// <param name="sequence">解析出的序列号</param>
    /// <returns>是否解析成功</returns>
    public virtual Boolean TryParse(Int64 id, out DateTime time, out Int32 workerId, out Int32 sequence)
    {
        var timestamp = id >> TimestampShift;
        time = StartTimestamp.AddMilliseconds(timestamp);
        workerId = (Int32)((id >> WorkerIdShift) & MaxWorkerId);
        sequence = (Int32)(id & MaxSequence);
        return true;
    }

    /// <summary>把输入时间转为开始时间戳的类型，便于相减</summary>
    /// <param name="time">要转换的时间</param>
    /// <returns>转换后的时间</returns>
    public DateTime ConvertKind(DateTime time)
    {
        // 如果待转换时间未指定时区，则直接返回
        if (time.Kind == DateTimeKind.Unspecified) return time;

        return StartTimestamp.Kind switch
        {
            DateTimeKind.Utc => time.ToUniversalTime(),
            DateTimeKind.Local => time.ToLocalTime(),
            _ => time,
        };
    }
    #endregion

    #region 私有辅助方法
    /// <summary>构建雪花Id</summary>
    private static Int64 BuildSnowflakeId(Int64 timestamp, Int32 workerId, Int32 sequence)
    {
        return (timestamp << TimestampShift) |
               ((Int64)(workerId & MaxWorkerId) << WorkerIdShift) |
               (Int64)(sequence & MaxSequence);
    }
    #endregion

    #region 集群扩展
    /// <summary>加入集群。由集群统一分配WorkerId，确保唯一，从而保证生成的雪花Id绝对唯一</summary>
    /// <param name="cache">缓存实例，通常是Redis</param>
    /// <param name="key">分配WorkerId的缓存键</param>
    public virtual void JoinCluster(ICache cache, String key = "SnowflakeWorkerId")
    {
        if (cache == null) throw new ArgumentNullException(nameof(cache));

        var workerId = (Int32)cache.Increment(key, 1);
        WorkerId = workerId & MaxWorkerId;
    }
    #endregion
}