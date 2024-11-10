﻿using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
#if !NET45
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Net;

/// <summary>会话基类</summary>
public abstract class SessionBase : DisposeBase, ISocketClient, ITransport, ILogFeature
{
    #region 属性

    /// <summary>标识</summary>
    public Int32 ID { get; internal set; }

    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>本地绑定信息</summary>
    public NetUri Local { get; set; } = new NetUri();

    /// <summary>端口</summary>
    public Int32 Port { get { return Local.Port; } set { Local.Port = value; } }

    /// <summary>远程结点地址</summary>
    public NetUri Remote { get; set; } = new NetUri();

    /// <summary>超时。默认3000ms</summary>
    public Int32 Timeout { get; set; } = 3_000;

    /// <summary>是否活动</summary>
    public Boolean Active { get; set; }

    /// <summary>底层Socket</summary>
    public Socket? Client { get; protected set; }

    /// <summary>最后一次通信时间，主要表示活跃时间，包括收发</summary>
    public DateTime LastTime { get; internal protected set; } = DateTime.Now;

    /// <summary>最大并行接收数。Tcp默认1，Udp默认CPU*1.6，0关闭异步接收使用同步接收</summary>
    public Int32 MaxAsync { get; set; } = 1;

    /// <summary>缓冲区大小。默认8k</summary>
    public Int32 BufferSize { get; set; }

    /// <summary>连接关闭原因</summary>
    public String? CloseReason { get; set; }

    /// <summary>APM性能追踪器</summary>
    public ITracer? Tracer { get; set; }

    #endregion 属性

    #region 构造

    /// <summary>构造函数，初始化默认名称</summary>
    public SessionBase()
    {
        Name = GetType().Name;

        BufferSize = SocketSetting.Current.BufferSize;
        LogDataLength = SocketSetting.Current.LogDataLength;
    }

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        var reason = GetType().Name + (disposing ? "Dispose" : "GC");

        try
        {
            Close(reason);
        }
        catch (Exception ex)
        {
            OnError("Dispose", ex);
        }
    }

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => Local + "";

    #endregion 构造

    #region 打开关闭

    /// <summary>打开</summary>
    /// <returns>是否成功</returns>
    public virtual Boolean Open()
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);

        if (Active) return true;
        lock (this)
        {
            if (Active) return true;

            using var span = Tracer?.NewSpan($"net:{Name}:Open", Remote?.ToString());
            try
            {
                _RecvCount = 0;

                var rs = OnOpen();
                if (!rs) return false;

                var timeout = Timeout;
                if (timeout > 0 && Client != null)
                {
                    Client.SendTimeout = timeout;
                    Client.ReceiveTimeout = timeout;
                }

                Active = true;

                if (Pipeline is Pipeline pipe && pipe.Handlers.Count > 0)
                {
                    WriteLog("初始化管道：");
                    foreach (var handler in pipe.Handlers)
                    {
                        WriteLog("    {0}", handler);
                    }
                }

                // 初始化管道
                Pipeline?.Open(CreateContext(this));

                ReceiveAsync();

                // 触发打开完成的事件
                Opened?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                span?.SetError(ex, null);
                throw;
            }
        }

        return true;
    }

    /// <summary>打开</summary>
    /// <returns></returns>
    [MemberNotNullWhen(true, nameof(Client))]
    protected abstract Boolean OnOpen();

    /// <summary>关闭</summary>
    /// <param name="reason">关闭原因。便于日志分析</param>
    /// <returns>是否成功</returns>
    public virtual Boolean Close(String reason)
    {
        if (!Active) return true;
        lock (this)
        {
            if (!Active) return true;

            using var span = Tracer?.NewSpan($"net:{Name}:Close", Remote?.ToString());
            try
            {
                CloseReason = reason;

                // 管道
                Pipeline?.Close(CreateContext(this), reason);

                var rs = true;
                if (OnClose(reason ?? (GetType().Name + "Close"))) rs = false;

                _RecvCount = 0;

                // 触发关闭完成的事件
                Closed?.Invoke(this, EventArgs.Empty);

                Active = rs;

                return !rs;
            }
            catch (Exception ex)
            {
                span?.SetError(ex, null);
                throw;
            }
        }
    }

    /// <summary>关闭</summary>
    /// <param name="reason">关闭原因。便于日志分析</param>
    /// <returns></returns>
    protected abstract Boolean OnClose(String reason);

    Boolean ITransport.Close() => Close("TransportClose");

    /// <summary>检查连接是否已关闭，并返回关闭原因，主要检测FIN/RST</summary>
    /// <returns></returns>
    protected String? CheckClosed()
    {
        var sock = Client;
        if (sock == null || !sock.Connected) return "Disconnected";

        if (sock.Poll(10, SelectMode.SelectRead))
        {
            try
            {
                var buffer = new Byte[1];
                if (sock.Receive(buffer, SocketFlags.Peek) == 0)
                {
                    // 收到FIN标记
                    return "Finish";
                }
            }
            catch (SocketException ex)
            when (ex.SocketErrorCode == SocketError.ConnectionReset)
            {
                return "ConnectionReset";
            }
        }

        return null;
    }

    /// <summary>打开后触发。</summary>
    public event EventHandler? Opened;

    /// <summary>关闭后触发。可实现掉线重连</summary>
    public event EventHandler? Closed;

    #endregion 打开关闭

    #region 发送

    /// <summary>直接发送数据包 Byte[]/Packet</summary>
    /// <remarks>
    /// 目标地址由<seealso cref="Remote"/>决定
    /// </remarks>
    /// <param name="data">数据包</param>
    /// <returns>是否成功</returns>
    public Int32 Send(IPacket data)
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);
        if (!Open()) return -1;

        return OnSend(data);
    }

    /// <summary>发送数据</summary>
    /// <remarks>
    /// 目标地址由<seealso cref="Remote"/>决定
    /// </remarks>
    /// <param name="data">数据包</param>
    /// <returns>是否成功</returns>
    protected abstract Int32 OnSend(IPacket data);

    #endregion 发送

    #region 接收

    /// <summary>接收数据</summary>
    /// <returns></returns>
    public virtual IOwnerPacket? Receive()
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);

        if (!Open() || Client == null) return null;

        using var span = Tracer?.NewSpan($"net:{Name}:Receive", BufferSize + "");
        try
        {
            var pk = new OwnerPacket(BufferSize);
            var size = Client.Receive(pk.Buffer, SocketFlags.None);
            if (span != null) span.Value = size;

            return pk.Resize(size);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>异步接收数据</summary>
    /// <returns></returns>
    public virtual async Task<IOwnerPacket?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);

        if (!Open() || Client == null) return null;

        using var span = Tracer?.NewSpan($"net:{Name}:ReceiveAsync", BufferSize + "");
        try
        {
            var pk = new OwnerPacket(BufferSize);
#if NETFRAMEWORK || NETSTANDARD2_0
            var ar = Client.BeginReceive(pk.Buffer, 0, pk.Length, SocketFlags.None, null, Client);
            var size = ar.IsCompleted ?
                Client.EndReceive(ar) :
                await Task.Factory.FromAsync(ar, Client.EndReceive);
#else
            var size = await Client.ReceiveAsync(pk.GetMemory(), SocketFlags.None, cancellationToken);
#endif
            if (span != null) span.Value = size;

            return pk.Resize(size);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>当前异步接收个数</summary>
    private Int32 _RecvCount;

    /// <summary>开始异步接收。在事件中返回数据</summary>
    /// <returns>是否成功</returns>
    public virtual Boolean ReceiveAsync()
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);

        if (!Open()) return false;

        var count = _RecvCount;
        var max = MaxAsync;
        if (count >= max) return false;

        // 按照最大并发创建异步委托
        for (var i = count; i < max; i++)
        {
            count = Interlocked.Increment(ref _RecvCount);
            if (count > max)
            {
                Interlocked.Decrement(ref _RecvCount);
                return false;
            }

            // 加大接收缓冲区，规避SocketError.MessageSize问题
            var buf = new Byte[BufferSize];
            var se = new SocketAsyncEventArgs();
            se.SetBuffer(buf, 0, buf.Length);
            se.Completed += (s, e) => ProcessEvent(e, -1, _IntoThreadCount);
            se.UserToken = count;

            if (Log != null && Log.Level <= LogLevel.Debug) WriteLog("创建RecvSA {0}", count);

            StartReceive(se, 0);
        }

        return true;
    }

    /// <summary>释放一个事件参数</summary>
    /// <param name="se"></param>
    /// <param name="reason"></param>
    private void ReleaseRecv(SocketAsyncEventArgs se, String reason)
    {
        var idx = se.UserToken.ToInt();

        if (Log != null && Log.Level <= LogLevel.Debug) WriteLog("释放RecvSA {0} {1}", idx, reason);

        if (_RecvCount > 0) Interlocked.Decrement(ref _RecvCount);
        try
        {
            se.SetBuffer(null, 0, 0);
        }
        catch { }
        se.TryDispose();
    }

    /// <summary>当前进入线程递归数量，超过10就另外起线程</summary>
    private readonly static Int32 _IntoThreadCount = 10;

    /// <summary>用一个事件参数来开始异步接收</summary>
    /// <param name="se">事件参数</param>
    /// <param name="ioThread">是否在线程池调用,小于等于0不是，大于0是</param>
    /// <returns></returns>
    private Boolean StartReceive(SocketAsyncEventArgs se, Int32 ioThread)
    {
        if (Disposed)
        {
            ReleaseRecv(se, "Disposed " + se.SocketError);

            throw new ObjectDisposedException(GetType().Name);
        }

        var rs = false;
        try
        {
            // 开始新的监听
            rs = OnReceiveAsync(se);
        }
        catch (Exception ex)
        {
            ReleaseRecv(se, "ReceiveAsyncError " + ex.Message);

            if (!ex.IsDisposed())
            {
                OnError("ReceiveAsync", ex);

                // 异常一般是网络错误，UDP不需要关闭
                //if (!io && ThrowException) throw;
            }
            return false;
        }

        // 同步返回0数据包，断开连接
        if (!rs && se.BytesTransferred == 0 && se.SocketError == SocketError.Success)
        {
            var reason = CheckClosed() ?? "EmptyData";
            Close(reason);
            Dispose();

            return false;
        }

        // 如果当前就是异步线程，直接处理，否则需要开任务处理，不要占用主线程
        if (!rs)
        {
            if (ioThread-- > 0)
            {
                ProcessEvent(se, -1, ioThread);
            }
            else
            {
                ThreadPool.UnsafeQueueUserWorkItem(s =>
                {
                    try
                    {
                        if (s is SocketAsyncEventArgs ee) ProcessEvent(ee, -1, _IntoThreadCount);
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex);
                    }
                }, se);
            }
        }

        return true;
    }

    internal abstract Boolean OnReceiveAsync(SocketAsyncEventArgs se);

    /// <summary>同步或异步收到数据</summary>
    /// <remarks>
    /// ioThread:
    /// 如果在StartReceive的时候线程池调用ProcessEvent，则处于worker线程；
    /// 如果在IOCP的时候调用ProcessEvent，则处于completionPort线程。
    /// </remarks>
    /// <param name="se"></param>
    /// <param name="bytes"></param>
    /// <param name="ioThread">是否在IO线程池里面</param>
    protected internal void ProcessEvent(SocketAsyncEventArgs se, Int32 bytes, Int32 ioThread)
    {
        try
        {
            if (!Active)
            {
                ReleaseRecv(se, "!Active " + se.SocketError);
                return;
            }

            // 判断成功失败
            if (se.SocketError != SocketError.Success)
            {
                // 未被关闭Socket时，可以继续使用
                if (OnReceiveError(se))
                {
                    var ex = se.GetException();
                    if (ex != null) OnError("ReceiveAsync", ex);

                    ReleaseRecv(se, "SocketError " + se.SocketError);

                    return;
                }
            }
            else
            {
                var ep = se.RemoteEndPoint as IPEndPoint ?? Remote.EndPoint;
                if (bytes < 0) bytes = se.BytesTransferred;
                if (se.Buffer != null)
                {
                    var pk = new ArrayPacket(se.Buffer, se.Offset, bytes);

                    // 同步执行，直接使用数据，不需要拷贝
                    // 直接在IO线程调用业务逻辑
                    ProcessReceive(pk, se, ep);
                }
            }

            // 开始新的监听
            if (Active && !Disposed)
                StartReceive(se, ioThread);
            else
                ReleaseRecv(se, "!Active || Disposed");
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);

            try
            {
                // 如果数据处理异常，并且Error处理也抛出异常，则这里可能出错，导致整个接收链毁掉。
                // 但是这个可能性极低
                ReleaseRecv(se, "ProcessEventError " + ex.Message);
                Close("ProcessEventError");
            }
            catch { }

            Dispose();
        }
    }

    /// <summary>接收预处理，粘包拆包</summary>
    /// <param name="pk">数据包</param>
    /// <param name="se">socket异步事件</param>
    /// <param name="remote">远程地址</param>
    private void ProcessReceive(IPacket pk, SocketAsyncEventArgs se, IPEndPoint remote)
    {
        // 打断上下文调用链，这里必须是起点
        DefaultSpan.Current = null;

        var total = pk.Length;
        var local = se.ReceiveMessageFromPacketInfo.Address;
        using var span = Tracer?.NewSpan($"net:{Name}:ProcessReceive", new { total, local, remote }, total);
        try
        {
            LastTime = DateTime.Now;

            // 预处理，得到将要处理该数据包的会话
            var ss = OnPreReceive(pk, local, remote);
            if (ss == null) return;

            if (LogReceive && Log != null && Log.Enable) WriteLog("Recv [{0}]: {1}", total, pk.ToHex(LogDataLength));

            if (Local.IsTcp) remote = Remote.EndPoint;

            var e = new ReceivedEventArgs { Packet = pk, Local = local, Remote = remote };

            // 不管Tcp/Udp，都在这使用管道
            var pp = Pipeline;
            if (pp == null)
                OnReceive(e);
            else
            {
                var ctx = CreateContext(ss);
                ctx.Data = e;
                ctx.EventArgs = se;

                // 进入管道处理，如果有一个或多个结果通过Finish来处理
                pp.Read(ctx, pk);
            }
        }
        catch (Exception ex)
        {
            span?.SetError(ex, pk.ToHex());
            if (!ex.IsDisposed()) OnError("OnReceive", ex);
        }
    }

    /// <summary>预处理</summary>
    /// <param name="pk">数据包</param>
    /// <param name="local">接收数据的本地地址</param>
    /// <param name="remote">远程地址</param>
    /// <returns>将要处理该数据包的会话</returns>
    protected internal abstract ISocketSession? OnPreReceive(IPacket pk, IPAddress local, IPEndPoint remote);

    /// <summary>处理收到的数据。默认匹配同步接收委托</summary>
    /// <param name="e">接收事件参数</param>
    /// <returns>是否已处理，已处理的数据不再向下传递</returns>
    protected abstract Boolean OnReceive(ReceivedEventArgs e);

    /// <summary>数据到达事件</summary>
    public event EventHandler<ReceivedEventArgs>? Received;

    /// <summary>触发数据到达事件</summary>
    /// <param name="sender"></param>
    /// <param name="e">接收事件参数</param>
    protected virtual void RaiseReceive(Object sender, ReceivedEventArgs e) => Received?.Invoke(sender, e);

    /// <summary>收到异常时如何处理。默认关闭会话</summary>
    /// <param name="se"></param>
    /// <returns>是否当作异常处理并结束会话</returns>
    internal virtual Boolean OnReceiveError(SocketAsyncEventArgs se)
    {
        //if (se.SocketError == SocketError.ConnectionReset) Dispose();
        if (se.SocketError == SocketError.ConnectionReset) Close("ConnectionReset");

        return true;
    }

    #endregion 接收

    #region 消息处理

    /// <summary>消息管道。收发消息都经过管道处理器，进行协议编码解码</summary>
    /// <remarks>
    /// 1，接收数据解码时，从前向后通过管道处理器；
    /// 2，发送数据编码时，从后向前通过管道处理器；
    /// </remarks>
    public IPipeline? Pipeline { get; set; }

    /// <summary>创建上下文</summary>
    /// <param name="session">远程会话</param>
    /// <returns></returns>
    protected internal virtual NetHandlerContext CreateContext(ISocketRemote session)
    {
        var context = new NetHandlerContext
        {
            Pipeline = Pipeline,
            Session = session,
            Owner = session,
        };

        return context;
    }

    /// <summary>通过管道发送消息，不等待响应</summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public virtual Int32 SendMessage(Object message)
    {
        if (Pipeline == null) throw new ArgumentNullException(nameof(Pipeline), "No pipes are set");

        using var span = Tracer?.NewSpan($"net:{Name}:SendMessage", message);
        try
        {
            var ctx = CreateContext(this);
            return (Int32)(Pipeline.Write(ctx, message) ?? 0);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, message);
            throw;
        }
    }

    /// <summary>通过管道发送消息并等待响应</summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public virtual async Task<Object> SendMessageAsync(Object message)
    {
        if (Pipeline == null) throw new ArgumentNullException(nameof(Pipeline), "No pipes are set");

        using var span = Tracer?.NewSpan($"net:{Name}:SendMessageAsync", message);
        try
        {
            var ctx = CreateContext(this);
#if NET45
            var source = new TaskCompletionSource<Object>();
#else
            var source = new TaskCompletionSource<Object>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
            ctx["TaskSource"] = source;
            ctx["Span"] = span;

            var rs = (Int32)(Pipeline.Write(ctx, message) ?? 0);
            if (rs < 0) return TaskEx.CompletedTask;

            return await source.Task;
        }
        catch (Exception ex)
        {
            if (ex is TaskCanceledException)
                span?.AppendTag(ex.Message);
            else
                span?.SetError(ex, message);
            throw;
        }
    }

    /// <summary>通过管道发送消息并等待响应</summary>
    /// <param name="message">消息</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public virtual async Task<Object> SendMessageAsync(Object message, CancellationToken cancellationToken)
    {
        if (Pipeline == null) throw new ArgumentNullException(nameof(Pipeline), "No pipes are set");

        using var span = Tracer?.NewSpan($"net:{Name}:SendMessageAsync", message);
        try
        {
            var ctx = CreateContext(this);
#if NET45
            var source = new TaskCompletionSource<Object>();
#else
            var source = new TaskCompletionSource<Object>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
            ctx["TaskSource"] = source;
            ctx["Span"] = span;

            var rs = (Int32)(Pipeline.Write(ctx, message) ?? 0);
            if (rs < 0) return TaskEx.CompletedTask;

            // 注册取消时的处理，如果没有收到响应，取消发送等待
            // Register返回值需要Dispose，否则会导致内存泄漏
            // https://stackoverflow.com/questions/14627226/why-is-my-async-await-with-cancellationtokensource-leaking-memory
            using (cancellationToken.Register(TrySetCanceled, source))
            {
                return await source.Task;
            }
        }
        catch (Exception ex)
        {
            if (ex is TaskCanceledException)
                span?.AppendTag(ex.Message);
            else
                span?.SetError(ex, null);
            throw;
        }
    }

    private void TrySetCanceled(Object? state)
    {
        if (state is TaskCompletionSource<Object> source && !source.Task.IsCompleted)
            source.TrySetCanceled();
    }

    /// <summary>处理数据帧</summary>
    /// <param name="data">数据帧</param>
    void ISocketRemote.Process(IData data)
    {
        if (data is ReceivedEventArgs e) OnReceive(e);
    }

    #endregion 消息处理

    #region 异常处理

    /// <summary>错误发生/断开连接时</summary>
    public event EventHandler<ExceptionEventArgs>? Error;

    /// <summary>触发异常</summary>
    /// <param name="action">动作</param>
    /// <param name="ex">异常</param>
    protected internal virtual void OnError(String action, Exception ex)
    {
        Pipeline?.Error(CreateContext(this), ex);

        Log?.Error("{0}{1}Error {2} {3}", LogPrefix, action, this, ex.Message);
        Error?.Invoke(this, new ExceptionEventArgs(action, ex));
    }

    #endregion 异常处理

    #region 扩展接口
    private ConcurrentDictionary<String, Object?>? _items;
    /// <summary>数据项</summary>
    public IDictionary<String, Object?> Items => _items ??= new();

    /// <summary>设置 或 获取 数据项</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Object? this[String key] { get => _items != null && _items.TryGetValue(key, out var obj) ? obj : null; set => Items[key] = value; }
    #endregion

    #region 日志

    /// <summary>日志前缀</summary>
    public virtual String? LogPrefix { get; set; }

    /// <summary>日志对象。禁止设为空对象</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>是否输出发送日志。默认false</summary>
    public Boolean LogSend { get; set; }

    /// <summary>是否输出接收日志。默认false</summary>
    public Boolean LogReceive { get; set; }

    /// <summary>收发日志数据体长度。默认64</summary>
    public Int32 LogDataLength { get; set; } = 64;

    /// <summary>输出日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object?[] args)
    {
        LogPrefix ??= Name.TrimEnd("Server", "Session", "Client");
        if (Log != null && Log.Enable) Log.Info($"[{LogPrefix}]{format}", args);
    }

    #endregion 日志
}