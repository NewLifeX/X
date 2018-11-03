using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
using NewLife.Threading;

namespace NewLife.Net
{
    /// <summary>会话基类</summary>
    public abstract class SessionBase : DisposeBase, ISocketClient, ITransport
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>本地绑定信息</summary>
        public NetUri Local { get; set; } = new NetUri();

        /// <summary>端口</summary>
        public Int32 Port { get { return Local.Port; } set { Local.Port = value; } }

        /// <summary>远程结点地址</summary>
        public NetUri Remote { get; set; } = new NetUri();

        /// <summary>超时。默认3000ms</summary>
        public Int32 Timeout { get; set; } = 3000;

        /// <summary>是否活动</summary>
        public Boolean Active { get; set; }

        /// <summary>底层Socket</summary>
        public Socket Client { get; protected set; }

        /// <summary>是否抛出异常，默认false不抛出。Send/Receive时可能发生异常，该设置决定是直接抛出异常还是通过<see cref="Error"/>事件</summary>
        public Boolean ThrowException { get; set; }

        /// <summary>发送数据包统计信息</summary>
        public ICounter StatSend { get; set; }

        /// <summary>接收数据包统计信息</summary>
        public ICounter StatReceive { get; set; }

        /// <summary>通信开始时间</summary>
        public DateTime StartTime { get; private set; } = TimerX.Now;

        /// <summary>最后一次通信时间，主要表示活跃时间，包括收发</summary>
        public DateTime LastTime { get; internal protected set; }

        /// <summary>是否使用动态端口。如果Port为0则为动态端口</summary>
        public Boolean DynamicPort { get; private set; }

        /// <summary>最大并行接收数。Tcp默认1，Udp默认CPU*1.6，0关闭异步接收使用同步接收</summary>
        public Int32 MaxAsync { get; set; } = 1;

        /// <summary>异步处理接收到的数据，Tcp默认false，Udp默认true。</summary>
        /// <remarks>异步处理有可能造成数据包乱序，特别是Tcp。true利于提升网络吞吐量。false避免拷贝，提升处理速度</remarks>
        public Boolean ProcessAsync { get; set; }

        /// <summary>缓冲区大小。默认8k</summary>
        public Int32 BufferSize { get; set; }
        #endregion

        #region 构造
        /// <summary>构造函数，初始化默认名称</summary>
        public SessionBase()
        {
            Name = GetType().Name;
            LogPrefix = Name.TrimEnd("Server", "Session", "Client") + ".";

            BufferSize = Setting.Current.BufferSize;
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            var reason = GetType().Name + (disposing ? "Dispose" : "GC");
            //_SendQueue?.Release(reason);

            try
            {
                Close(reason);
            }
            catch (Exception ex) { OnError("Dispose", ex); }
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => Local + "";
        #endregion

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

                LogPrefix = "{0}.".F((Name + "").TrimEnd("Server", "Session", "Client"));

                BufferSize = Setting.Current.BufferSize;

                // 估算完成时间，执行过长时提示
                using (var tc = new TimeCost(GetType().Name + ".Open", 1500))
                {
                    tc.Log = Log;

                    _RecvCount = 0;
                    var rs = OnOpen();
                    if (!rs) return false;

                    if (Timeout > 0) Client.ReceiveTimeout = Timeout;

                    if (!Local.IsUdp)
                    {
                        // 管道
                        var pp = Pipeline;
                        pp?.Open(CreateContext(this));
                    }
                }
                Active = true;

                // 统计
                if (StatSend == null) StatSend = new PerfCounter();
                if (StatReceive == null) StatReceive = new PerfCounter();

                ReceiveAsync();

                // 触发打开完成的事件
                Opened?.Invoke(this, EventArgs.Empty);
            }

            return true;
        }

        /// <summary>打开</summary>
        /// <returns></returns>
        protected abstract Boolean OnOpen();

        /// <summary>检查是否动态端口。如果是动态端口，则把随机得到的端口拷贝到Port</summary>
        internal protected void CheckDynamic()
        {
            if (Port == 0)
            {
                DynamicPort = true;
                if (Port == 0) Port = (Client.LocalEndPoint as IPEndPoint).Port;
            }
        }

        /// <summary>关闭</summary>
        /// <param name="reason">关闭原因。便于日志分析</param>
        /// <returns>是否成功</returns>
        public virtual Boolean Close(String reason)
        {
            if (!Active) return true;
            lock (this)
            {
                if (!Active) return true;

                // 管道
                var pp = Pipeline;
                pp?.Close(CreateContext(this), reason);

                var rs = true;
                if (OnClose(reason ?? (GetType().Name + "Close"))) rs = false;

                _RecvCount = 0;

                // 触发关闭完成的事件
                Closed?.Invoke(this, EventArgs.Empty);

                // 如果是动态端口，需要清零端口
                if (DynamicPort) Port = 0;

                Active = rs;

                return !rs;
            }
        }

        /// <summary>关闭</summary>
        /// <param name="reason">关闭原因。便于日志分析</param>
        /// <returns></returns>
        protected abstract Boolean OnClose(String reason);

        Boolean ITransport.Close() => Close("传输口关闭");

        /// <summary>打开后触发。</summary>
        public event EventHandler Opened;

        /// <summary>关闭后触发。可实现掉线重连</summary>
        public event EventHandler Closed;
        #endregion

        #region 发送
        /// <summary>直接发送数据包 Byte[]/Packet</summary>
        /// <remarks>
        /// 目标地址由<seealso cref="Remote"/>决定
        /// </remarks>
        /// <param name="pk">数据包</param>
        /// <returns>是否成功</returns>
        public Boolean Send(Packet pk)
        {
            if (Disposed) throw new ObjectDisposedException(GetType().Name);
            if (!Open()) return false;

            return OnSend(pk);
        }

        /// <summary>发送数据</summary>
        /// <remarks>
        /// 目标地址由<seealso cref="Remote"/>决定
        /// </remarks>
        /// <param name="pk">数据包</param>
        /// <returns>是否成功</returns>
        protected abstract Boolean OnSend(Packet pk);
        #endregion

        #region 接收
        /// <summary>接收数据</summary>
        /// <returns></returns>
        public virtual Packet Receive()
        {
            if (Disposed) throw new ObjectDisposedException(GetType().Name);

            if (!Open()) return null;

            var buf = new Byte[BufferSize];
            var size = Client.Receive(buf);

            return new Packet(buf, 0, size);
        }

        /// <summary>当前异步接收个数</summary>
        private Int32 _RecvCount;

        /// <summary>开始异步接收</summary>
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
                if (Interlocked.Increment(ref _RecvCount) > max)
                {
                    Interlocked.Decrement(ref _RecvCount);
                    return false;
                }
                count = _RecvCount;

                // 加大接收缓冲区，规避SocketError.MessageSize问题
                var buf = new Byte[BufferSize];
                var se = new SocketAsyncEventArgs();
                se.SetBuffer(buf, 0, buf.Length);
                se.Completed += (s, e) => ProcessEvent(e);
                se.UserToken = count;

                if (Log != null && Log.Level <= LogLevel.Debug) WriteLog("创建RecvSA {0}", count);

                ReceiveAsync(se, false);
            }

            return true;
        }

        /// <summary>释放一个事件参数</summary>
        /// <param name="se"></param>
        /// <param name="reason"></param>
        void ReleaseRecv(SocketAsyncEventArgs se, String reason)
        {
            var idx = (Int32)se.UserToken;

            if (Log != null && Log.Level <= LogLevel.Debug) WriteLog("释放RecvSA {0} {1}", idx, reason);

            if (_RecvCount > 0) Interlocked.Decrement(ref _RecvCount);
            se.TryDispose();
        }

        /// <summary>用一个事件参数来开始异步接收</summary>
        /// <param name="se">事件参数</param>
        /// <param name="io">是否在IO线程调用</param>
        /// <returns></returns>
        Boolean ReceiveAsync(SocketAsyncEventArgs se, Boolean io)
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
                    if (!io && ThrowException) throw;
                }
                return false;
            }

            // 如果当前就是异步线程，直接处理，否则需要开任务处理，不要占用主线程
            if (!rs)
            {
                if (io)
                    ProcessEvent(se);
                else
                    ThreadPoolX.QueueUserWorkItem(ProcessEvent, se);
            }

            return true;
        }

        internal abstract Boolean OnReceiveAsync(SocketAsyncEventArgs se);

        /// <summary>同步或异步收到数据</summary>
        /// <param name="se"></param>
        void ProcessEvent(SocketAsyncEventArgs se)
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

                var pk = new Packet(se.Buffer, se.Offset, se.BytesTransferred);
                if (ProcessAsync)
                {
                    // 拷贝走数据，参数要重复利用
                    pk = pk.Clone();
                    // 根据不信任用户原则，这里另外开线程执行用户逻辑
                    // 有些用户在处理数据时，又发送数据并等待响应
                    ThreadPoolX.QueueUserWorkItem(() => ProcessReceive(pk, ep));
                }
                else
                {
                    // 同步执行，直接使用数据，不需要拷贝
                    // 直接在IO线程调用业务逻辑
                    ProcessReceive(pk, ep);
                }
            }

            // 开始新的监听
            if (Active && !Disposed)
                ReceiveAsync(se, true);
            else
                ReleaseRecv(se, "!Active || Disposed");
        }

        /// <summary>接收预处理，粘包拆包</summary>
        /// <param name="pk"></param>
        /// <param name="remote"></param>
        private void ProcessReceive(Packet pk, IPEndPoint remote)
        {
            try
            {
                LastTime = TimerX.Now;

                // 预处理，得到将要处理该数据包的会话
                var ss = OnPreReceive(pk, remote);
                if (ss == null) return;

                if (LogReceive && Log != null && Log.Enable) WriteLog("Recv [{0}]: {1}", pk.Total, pk.ToHex(32, null));

                if (Local.IsTcp) remote = Remote.EndPoint;

                var e = new ReceivedEventArgs(pk) { Remote = remote };

                // 不管Tcp/Udp，都在这使用管道
                var pp = Pipeline;
                if (pp == null)
                    OnReceive(e);
                else
                {
                    var ctx = CreateContext(ss);
                    ctx.Data = e;

                    // 进入管道处理，如果有一个或多个结果通过Finish来处理
                    var msg = pp.Read(ctx, pk);
                    // 最后结果落实消息
                    if (msg != null)
                    {
                        //ctx.FireRead(msg);
                        e.Message = msg;
                        OnReceive(e);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!ex.IsDisposed()) OnError("OnReceive", ex);
            }
        }

        /// <summary>预处理</summary>
        /// <param name="pk">数据包</param>
        /// <param name="remote">远程地址</param>
        /// <returns>将要处理该数据包的会话</returns>
        internal protected abstract ISocketSession OnPreReceive(Packet pk, IPEndPoint remote);

        /// <summary>处理收到的数据。默认匹配同步接收委托</summary>
        /// <param name="e">接收事件参数</param>
        /// <returns>是否已处理，已处理的数据不再向下传递</returns>
        protected abstract Boolean OnReceive(ReceivedEventArgs e);

        /// <summary>数据到达事件</summary>
        public event EventHandler<ReceivedEventArgs> Received;

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
            if (se.SocketError == SocketError.ConnectionReset) Close("ReceiveAsync " + se.SocketError);

            return true;
        }
        #endregion

        #region 消息处理
        /// <summary>消息管道。收发消息都经过管道处理器</summary>
        public IPipeline Pipeline { get; set; }

        /// <summary>创建上下文</summary>
        /// <param name="session">远程会话</param>
        /// <returns></returns>
        internal protected virtual NetHandlerContext CreateContext(ISocketRemote session)
        {
            var context = new NetHandlerContext
            {
                Pipeline = Pipeline,
                Session = session,
                Owner = session,
            };

            return context;
        }

        /// <summary>通过管道发送消息</summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public virtual Boolean SendMessage(Object message)
        {
            //Pipeline.FireWrite(this, message);

            var ctx = CreateContext(this);
            message = Pipeline.Write(ctx, message);

            return ctx.FireWrite(message);
        }

        /// <summary>通过管道发送消息并等待响应</summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public virtual Task<Object> SendMessageAsync(Object message)
        {
            //Pipeline.FireWriteAndWait(this, message);

            var ctx = CreateContext(this);
            var source = new TaskCompletionSource<Object>();
            ctx["TaskSource"] = source;

            message = Pipeline.Write(ctx, message);

#if NET4
            if (!ctx.FireWrite(message)) return TaskEx.FromResult((Object)null);
#else
            if (!ctx.FireWrite(message)) return Task.FromResult((Object)null);
#endif

            return source.Task;
        }

        /// <summary>处理数据帧</summary>
        /// <param name="data">数据帧</param>
        void ISocketRemote.Process(IData data) => OnReceive(data as ReceivedEventArgs);
#endregion

#region 异常处理
        /// <summary>错误发生/断开连接时</summary>
        public event EventHandler<ExceptionEventArgs> Error;

        /// <summary>触发异常</summary>
        /// <param name="action">动作</param>
        /// <param name="ex">异常</param>
        internal protected virtual void OnError(String action, Exception ex)
        {
            var pp = Pipeline;
            if (pp != null) pp.Error(CreateContext(this), ex);

            if (Log != null) Log.Error("{0}{1}Error {2} {3}", LogPrefix, action, this, ex?.Message);
            Error?.Invoke(this, new ExceptionEventArgs { Action = action, Exception = ex });
        }
#endregion

#region 扩展接口
        /// <summary>数据项</summary>
        public IDictionary<String, Object> Items { get; } = new NullableDictionary<String, Object>();

        /// <summary>设置 或 获取 数据项</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Object this[String key] { get => Items[key]; set => Items[key] = value; }
#endregion

#region 日志
        /// <summary>日志前缀</summary>
        public virtual String LogPrefix { get; set; }

        /// <summary>日志对象。禁止设为空对象</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>是否输出发送日志。默认false</summary>
        public Boolean LogSend { get; set; }

        /// <summary>是否输出接收日志。默认false</summary>
        public Boolean LogReceive { get; set; }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            if (Log != null && Log.Enable) Log.Info(LogPrefix + format, args);
        }
#endregion
    }
}