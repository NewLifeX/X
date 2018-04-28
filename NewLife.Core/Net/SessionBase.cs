using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Log;
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
        public IStatistics StatSend { get; set; } = new Statistics();

        /// <summary>接收数据包统计信息</summary>
        public IStatistics StatReceive { get; set; } = new Statistics();

        /// <summary>通信开始时间</summary>
        public DateTime StartTime { get; private set; } = DateTime.Now;

        /// <summary>最后一次通信时间，主要表示活跃时间，包括收发</summary>
        public DateTime LastTime { get; internal protected set; }

        /// <summary>是否使用动态端口。如果Port为0则为动态端口</summary>
        public Boolean DynamicPort { get; private set; }

        /// <summary>最大并行接收数。Tcp默认1，Udp默认CPU*1.6</summary>
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
            LogPrefix = "{0}.".F((Name + "").TrimEnd("Server", "Session", "Client"));

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

            LogPrefix = "{0}.".F((Name + "").TrimEnd("Server", "Session", "Client"));

            BufferSize = Setting.Current.BufferSize;

            // 估算完成时间，执行过长时提示
            using (var tc = new TimeCost("{0}.Open".F(GetType().Name), 1500))
            {
                tc.Log = Log;

                _RecvCount = 0;
                Active = OnOpen();
                if (!Active) return false;

                if (Timeout > 0) Client.ReceiveTimeout = Timeout;

                // 管道
                var pp = Pipeline;
                pp?.Open(pp.CreateContext(this));
            }

            ReceiveAsync();

            // 触发打开完成的事件
            Opened?.Invoke(this, EventArgs.Empty);

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

            // 估算完成时间，执行过长时提示
            using (var tc = new TimeCost("{0}.Close".F(GetType().Name), 500))
            {
                tc.Log = Log;

                if (OnClose(reason ?? (GetType().Name + "Close"))) Active = false;

                _RecvCount = 0;

                // 管道
                var pp = Pipeline;
                pp?.Close(pp.CreateContext(this), reason);

                // 触发关闭完成的事件
                Closed?.Invoke(this, EventArgs.Empty);
            }

            // 如果是动态端口，需要清零端口
            if (DynamicPort) Port = 0;

            return !Active;
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
        /// <summary>发送数据</summary>
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
                se.Completed += (s, e) => ProcessReceive(e);
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
                    ProcessReceive(se);
                else
                    ThreadPoolX.QueueUserWorkItem(() => ProcessReceive(se));
            }

            return true;
        }

        internal abstract Boolean OnReceiveAsync(SocketAsyncEventArgs se);

        /// <summary>同步或异步收到数据</summary>
        /// <param name="se"></param>
        void ProcessReceive(SocketAsyncEventArgs se)
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
                LastTime = DateTime.Now;
                if (!OnPreReceive(pk, remote)) return;

                if (Log.Enable && LogReceive) WriteLog("Recv [{0}]: {1}", pk.Total, pk.ToHex(32, null));

                if (Local.IsTcp) remote = Remote.EndPoint;
                var e = new ReceivedEventArgs(pk) { Remote = remote };

                // 不管Tcp/Udp，都在这使用管道
                var pp = Pipeline;
                if (pp == null)
                    OnReceive(e);
                else
                {
                    var ctx = pp.CreateContext(this);
                    ctx.Data = e;
                    //ctx.Finish = (c, v) =>
                    //{
                    //    var e2 = new ReceivedEventArgs(c.Data) { Message = v };
                    //    OnReceive(e2);
                    //};

                    // 进入管道处理，如果有一个或多个结果通过Finish来处理
                    pp.Read(ctx, pk);
                }
            }
            catch (Exception ex)
            {
                if (!ex.IsDisposed()) OnError("OnReceive", ex);
            }
        }

        /// <param name="pk">数据包</param>
        /// <param name="remote">远程</param>
        internal protected abstract Boolean OnPreReceive(Packet pk, IPEndPoint remote);

        /// <summary>处理收到的数据。默认匹配同步接收委托</summary>
        /// <param name="e">接收事件参数</param>
        /// <returns>是否已处理，已处理的数据不再向下传递</returns>
        protected abstract Boolean OnReceive(ReceivedEventArgs e);

        /// <summary>数据到达事件</summary>
        public event EventHandler<ReceivedEventArgs> Received;

        /// <summary>触发数据到达事件</summary>
        /// <param name="sender"></param>
        /// <param name="e">接收事件参数</param>
        protected virtual void RaiseReceive(Object sender, ReceivedEventArgs e)
        {
            LastTime = DateTime.Now;

            Received?.Invoke(sender, e);
        }

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

        #region 数据包处理
        /// <summary>管道</summary>
        public IPipeline Pipeline { get; set; }

        /// <summary>发送消息</summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public virtual Boolean SendMessage(Object message) => Pipeline.FireWrite(this, message);

        /// <summary>发送消息并等待响应</summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public virtual async Task<Object> SendAsync(Object message) => await Pipeline.FireWriteAndWait(this, message);

        /// <summary>处理数据帧</summary>
        /// <param name="data">数据帧</param>
        public virtual void Receive(IData data) => OnReceive(data as ReceivedEventArgs);
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
            if (pp != null) pp.Error(pp.CreateContext(this), ex);

            if (Log != null) Log.Error("{0}{1}Error {2} {3}", LogPrefix, action, this, ex?.Message);
            Error?.Invoke(this, new ExceptionEventArgs { Action = action, Exception = ex });
        }
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