using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using NewLife.Threading;

namespace NewLife.Net;

/// <summary>会话集合</summary>
/// <remarks>
/// <para>带有自动清理不活动会话的功能。</para>
/// <para>使用远程地址端口作为标识，自动管理会话生命周期。</para>
/// </remarks>
internal class SessionCollection : DisposeBase, IDictionary<String, ISocketSession>
{
    #region 属性
    private readonly ConcurrentDictionary<String, ISocketSession> _dic = new();

    /// <summary>远程端点缓存。热路径（UDP 每包查找会话）以端点对象为键，免去每包拼接字符串键（IPEndPoint.ToString 的多次字符串分配）</summary>
    private readonly ConcurrentDictionary<IPEndPoint, ISocketSession> _endPoints = new();

    /// <summary>服务端</summary>
    public ISocketServer Server { get; private set; }

    /// <summary>清理周期（秒）</summary>
    /// <remarks>默认10秒检查一次不活动会话</remarks>
    public Int32 ClearPeriod { get; set; } = 10;

    /// <summary>清理会话计时器</summary>
    private TimerX? _clearTimer;
    #endregion

    #region 构造
    /// <summary>实例化会话集合</summary>
    /// <param name="server">所属服务端</param>
    public SessionCollection(ISocketServer server) => Server = server;

    /// <summary>释放资源</summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _clearTimer.TryDispose();
        _clearTimer = null;

        var reason = GetType().Name + (disposing ? "Dispose" : "GC");
        try
        {
            CloseAll(reason);
        }
        catch { }
    }
    #endregion

    #region 主要方法
    /// <summary>添加新会话</summary>
    /// <param name="session">会话实例</param>
    /// <returns>返回添加新会话是否成功</returns>
    public Boolean Add(ISocketSession session)
    {
        // 已销毁的会话不再入集合：其 OnDisposed 已经触发过，移除回调不会再触发，条目只能等超时清理
        if (session.Disposed) return false;

        var key = session.Remote.EndPoint + "";

        // 先订阅销毁回调再入集合。反序存在“已入集合但尚未订阅”的窗口：
        // 会话在该窗口内销毁则移除回调永不触发，端点标识永久残留，之后同端点的新连接被静默拒绝
        EventHandler onDisposed = (s, e) =>
        {
            if (s is ISocketSession ss)
            {
                _dic.TryRemove(ss.Remote.EndPoint + "", out _);
                RemoveCache(ss);
            }
        };
        session.OnDisposed += onDisposed;

        if (!_dic.TryAdd(key, session))
        {
            // 端点重复等入集合失败：退订，避免回调挂在未被集合接管的会话上
            session.OnDisposed -= onDisposed;
            return false;
        }

        _endPoints[session.Remote.EndPoint] = session;

        var p = ClearPeriod * 1000;
        _clearTimer ??= new TimerX(RemoveNotAlive, null, p, p) { Async = true, };

        return true;
    }

    /// <summary>获取会话</summary>
    /// <param name="key">远程地址端口标识</param>
    /// <returns>会话实例</returns>
    public ISocketSession? Get(String key)
    {
        if (!_dic.TryGetValue(key, out var session)) return null;

        return session;
    }

    /// <summary>获取会话（按远程端点）</summary>
    /// <remarks>仅作加速：未命中时回退字符串键路径；创建与移除路径同步维护缓存，保证与实际集合一致。</remarks>
    /// <param name="endPoint">远程端点</param>
    /// <returns>会话实例</returns>
    internal ISocketSession? Get(IPEndPoint endPoint)
    {
        if (_endPoints.TryGetValue(endPoint, out var session) && !session.Disposed) return session;

        // 首包或缓存失效时回退字符串键路径，命中后写入缓存
        session = Get(endPoint + "");
        if (session != null) _endPoints[endPoint] = session;

        return session;
    }

    /// <summary>从端点缓存移除指向指定会话的条目（值校验，避免旧会话销毁时误删同端点的新会话）</summary>
    private void RemoveCache(ISocketSession session)
    {
        var ep = session.Remote.EndPoint;
        if (_endPoints.TryGetValue(ep, out var item) && ReferenceEquals(item, session)) _endPoints.TryRemove(ep, out _);
    }

    /// <summary>停机时是否排空发送队列。默认 false</summary>
    /// <remarks>
    /// <para>会话关闭会限时等待发送队列排空（最长会话 Timeout）。批量停机逐个等待会让总耗时随会话数线性放大
    /// （1000 会话 × 3 秒 ≈ 50 分钟），而停机场景对端往往已不可达，排队数据本就送不出去。</para>
    /// <para>需要“停机前尽力发完”时置为 true。</para>
    /// </remarks>
    public Boolean DrainOnShutdown { get; set; }

    /// <summary>关闭所有会话</summary>
    /// <param name="reason">关闭原因</param>
    public void CloseAll(String reason)
    {
        if (!_dic.Any()) return;

        foreach (var item in _dic.ToValueArray())
        {
            if (item != null && !item.Disposed)
            {
                // 批量停机直接中止发送队列，不做逐会话限时排空
                if (!DrainOnShutdown && item is SessionBase sb) sb.FastCloseOnShutdown = true;

                if (item is INetSession ss) ss.Close(reason);

                item.TryDispose();
            }
        }
    }

    /// <summary>移除不活动的会话</summary>
    /// <param name="state">定时器状态</param>
    private void RemoveNotAlive(Object? state)
    {
        if (!_dic.Any()) return;

        var timeout = 30;
        if (Server != null) timeout = Server.SessionTimeout;
        var keys = new List<String>();
        var values = new List<ISocketSession>();

        foreach (var elm in _dic)
        {
            var item = elm.Value;
            // 判断是否已超过最大不活跃时间
            if (item == null || item.Disposed || timeout > 0 && IsNotAlive(item, timeout))
            {
                keys.Add(elm.Key);
                values.Add(elm.Value);
            }
        }
        // 从会话集合里删除这些键值，并行字典操作安全；同步清理端点缓存（值校验，防止误删同端点的新会话）
        for (var i = 0; i < keys.Count; i++)
        {
            _dic.TryRemove(keys[i], out _);
            RemoveCache(values[i]);
        }

        // 已经离开了锁，慢慢释放各个会话
        foreach (var item in values)
        {
            item.WriteLog("超过{0}秒不活跃销毁 {1}", timeout, item);

            if (item is ISocketClient ss) ss.Close(nameof(RemoveNotAlive));
            item.TryDispose();
        }
    }

    private static Boolean IsNotAlive(ISocketSession session, Int32 timeout) => session.LastTime > DateTime.MinValue && session.LastTime.AddSeconds(timeout) < DateTime.Now;
    #endregion

    #region 成员
    /// <summary>清空会话索引（不关闭会话）</summary>
    /// <remarks>仅清空字典索引、不动会话本身；需要关闭会话请用 <see cref="CloseAll"/>。
    /// 两者常在停机流程里先后调用：先 CloseAll 收尾，再 Clear 清索引。</remarks>
    public void Clear()
    {
        _dic.Clear();
        _endPoints.Clear();
    }

    /// <summary>会话数量</summary>
    public Int32 Count => _dic.Count;

    /// <summary>是否只读。本集合支持增删，恒为 false</summary>
    public Boolean IsReadOnly => false;

    /// <summary>获取枚举器</summary>
    /// <returns>会话枚举器</returns>
    public IEnumerator<ISocketSession> GetEnumerator() => _dic.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _dic.GetEnumerator();
    #endregion

    #region IDictionary<String,ISocketSession> 成员

    void IDictionary<String, ISocketSession>.Add(String key, ISocketSession value)
    {
        // 显式 Add 语义要求键与会话端点一致且不得重复；旧实现忽略 key，传错键会静默按会话端点入集合
        var actual = value.Remote.EndPoint + "";
        if (key != null && key != actual) throw new ArgumentException($"键 [{key}] 与会话端点 [{actual}] 不一致", nameof(key));
        if (!Add(value)) throw new ArgumentException($"已存在键 [{key}] 的会话", nameof(key));
    }

    Boolean IDictionary<String, ISocketSession>.ContainsKey(String key) => _dic.ContainsKey(key);

    ICollection<String> IDictionary<String, ISocketSession>.Keys => _dic.Keys;

    Boolean IDictionary<String, ISocketSession>.Remove(String key)
    {
        if (!_dic.TryRemove(key, out var session)) return false;

        RemoveCache(session);

        if (session is INetSession ss) ss.Close("Remove");
        session.Dispose();

        return true;
    }

#if NETFRAMEWORK || NETSTANDARD
    Boolean IDictionary<String, ISocketSession>.TryGetValue(String key, out ISocketSession value) => _dic.TryGetValue(key, out value);
#else
    Boolean IDictionary<String, ISocketSession>.TryGetValue(String key, [MaybeNullWhen(false)] out ISocketSession value) => _dic.TryGetValue(key, out value);
#endif

    ICollection<ISocketSession> IDictionary<String, ISocketSession>.Values => _dic.Values;

    ISocketSession IDictionary<String, ISocketSession>.this[String key]
    {
        get => _dic[key];
        set
        {
            _dic[key] = value;
            _endPoints[value.Remote.EndPoint] = value;
        }
    }

    #endregion

    #region ICollection<KeyValuePair<String,ISocketSession>> 成员

    void ICollection<KeyValuePair<String, ISocketSession>>.Add(KeyValuePair<String, ISocketSession> item) => throw new XException("不支持！请使用Add(ISocketSession session)方法！");

    Boolean ICollection<KeyValuePair<String, ISocketSession>>.Contains(KeyValuePair<String, ISocketSession> item) => _dic.ContainsKey(item.Key);

    void ICollection<KeyValuePair<String, ISocketSession>>.CopyTo(KeyValuePair<String, ISocketSession>[] array, Int32 arrayIndex) =>
        ((ICollection<KeyValuePair<String, ISocketSession>>)_dic).CopyTo(array, arrayIndex);

    Boolean ICollection<KeyValuePair<String, ISocketSession>>.Remove(KeyValuePair<String, ISocketSession> item) => throw new XException("不支持！请直接销毁会话对象！");

    #endregion

    #region IEnumerable<KeyValuePair<String,ISocketSession>> 成员
    IEnumerator<KeyValuePair<String, ISocketSession>> IEnumerable<KeyValuePair<String, ISocketSession>>.GetEnumerator() => _dic.GetEnumerator();
    #endregion
}