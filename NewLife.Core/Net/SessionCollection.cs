using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
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
        var key = session.Remote.EndPoint + "";

        if (!_dic.TryAdd(key, session)) return false;

        var p = ClearPeriod * 1000;
        _clearTimer ??= new TimerX(RemoveNotAlive, null, p, p) { Async = true, };

        session.OnDisposed += (s, e) =>
        {
            if (s is ISocketSession ss)
                _dic.TryRemove(ss.Remote.EndPoint + "", out _);
        };

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

    /// <summary>关闭所有会话</summary>
    /// <param name="reason">关闭原因</param>
    public void CloseAll(String reason)
    {
        if (!_dic.Any()) return;

        foreach (var item in _dic.ToValueArray())
        {
            if (item != null && !item.Disposed)
            {
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
        // 从会话集合里删除这些键值，并行字典操作安全
        foreach (var item in keys)
        {
            _dic.TryRemove(item, out _);
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
    /// <summary>清空会话集合</summary>
    public void Clear() => _dic.Clear();

    /// <summary>会话数量</summary>
    public Int32 Count => _dic.Count;

    /// <summary>是否只读</summary>
    public Boolean IsReadOnly => (_dic as IDictionary<Int32, ISocketSession>)?.IsReadOnly ?? false;

    /// <summary>获取枚举器</summary>
    /// <returns>会话枚举器</returns>
    public IEnumerator<ISocketSession> GetEnumerator() => _dic.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _dic.GetEnumerator();
    #endregion

    #region IDictionary<String,ISocketSession> 成员

    void IDictionary<String, ISocketSession>.Add(String key, ISocketSession value) => Add(value);

    Boolean IDictionary<String, ISocketSession>.ContainsKey(String key) => _dic.ContainsKey(key);

    ICollection<String> IDictionary<String, ISocketSession>.Keys => _dic.Keys;

    Boolean IDictionary<String, ISocketSession>.Remove(String key)
    {
        if (!_dic.TryRemove(key, out var session)) return false;

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

    ISocketSession IDictionary<String, ISocketSession>.this[String key] { get => _dic[key]; set => _dic[key] = value; }

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