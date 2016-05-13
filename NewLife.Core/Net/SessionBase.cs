using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Log;

namespace NewLife.Net
{
    /// <summary>会话基类</summary>
    public abstract class SessionBase : DisposeBase, ISocketClient, ITransport
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>本地绑定信息</summary>
        public NetUri Local { get; set; }

        /// <summary>端口</summary>
        public Int32 Port { get { return Local.Port; } set { Local.Port = value; } }

        /// <summary>远程结点地址</summary>
        public NetUri Remote { get; set; }

        /// <summary>超时。默认3000ms</summary>
        public Int32 Timeout { get; set; }

        /// <summary>是否活动</summary>
        public Boolean Active { get; set; }

        /// <summary>底层Socket</summary>
        public Socket Client { get; protected set; }

        /// <summary>是否抛出异常，默认false不抛出。Send/Receive时可能发生异常，该设置决定是直接抛出异常还是通过<see cref="Error"/>事件</summary>
        public Boolean ThrowException { get; set; }

        /// <summary>发送数据包统计信息，默认关闭，通过<see cref="IStatistics.Enable"/>打开。</summary>
        public IStatistics StatSend { get; set; }

        /// <summary>接收数据包统计信息，默认关闭，通过<see cref="IStatistics.Enable"/>打开。</summary>
        public IStatistics StatReceive { get; set; }

        /// <summary>通信开始时间</summary>
        public DateTime StartTime { get; private set; }

        /// <summary>最后一次通信时间，主要表示活跃时间，包括收发</summary>
        public DateTime LastTime { get; protected set; }

        /// <summary>是否使用动态端口。如果Port为0则为动态端口</summary>
        public Boolean DynamicPort { get; private set; }

        /// <summary>最大并行接收数。默认1</summary>
        public Int32 MaxAsync { get; set; }
        #endregion

        #region 构造
        /// <summary>构造函数，初始化默认名称</summary>
        public SessionBase()
        {
            Name = this.GetType().Name;
            Local = new NetUri();
            Remote = new NetUri();
            Timeout = 3000;
            StartTime = DateTime.Now;

            StatSend = new Statistics();
            StatReceive = new Statistics();

            Log = Logger.Null;

            MaxAsync = 1;
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            _seSend.TryDispose();

            try
            {
                Close("销毁");
            }
            catch (Exception ex) { OnError("Dispose", ex); }
        }
        #endregion

        #region 打开关闭
        /// <summary>打开</summary>
        /// <returns>是否成功</returns>
        public virtual Boolean Open()
        {
            if (Disposed) throw new ObjectDisposedException(this.GetType().Name);

            if (Active) return true;

            // 即使没有事件，也允许强行打开异步接收
            if (!UseReceiveAsync && Received != null) UseReceiveAsync = true;

            // 估算完成时间，执行过长时提示
            using (var tc = new TimeCost("{0}.Open".F(this.GetType().Name), 1500))
            {
                tc.Log = Log;

                Active = OnOpen();
                if (!Active) return false;

                if (Timeout > 0) Client.ReceiveTimeout = Timeout;

                // 触发打开完成的事件
                if (Opened != null) Opened(this, EventArgs.Empty);
            }

            if (UseReceiveAsync) ReceiveAsync();

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
        /// <returns>是否成功</returns>
        public virtual Boolean Close(String reason = null)
        {
            if (!Active) return true;

            // 估算完成时间，执行过长时提示
            using (var tc = new TimeCost("{0}.Close".F(this.GetType().Name), 500))
            {
                tc.Log = Log;

                if (OnClose(reason)) Active = false;

                _RecvCount = 0;

                // 触发关闭完成的事件
                if (Closed != null) Closed(this, EventArgs.Empty);
            }

            // 如果是动态端口，需要清零端口
            if (DynamicPort) Port = 0;

            return !Active;
        }

        /// <summary>关闭</summary>
        /// <returns></returns>
        protected abstract Boolean OnClose(String reason);

        Boolean ITransport.Close() { return Close("传输口关闭"); }

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
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <returns>是否成功</returns>
        public abstract Boolean Send(Byte[] buffer, Int32 offset = 0, Int32 count = -1);

        /// <summary>异步发送数据</summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public virtual Boolean SendAsync(Byte[] buffer)
        {
            return SendAsync_(buffer, Remote.EndPoint);
        }

        private SocketAsyncEventArgs _seSend;
        private Int32 _Sending;
        private ConcurrentQueue<QueueItem> _SendQueue = new ConcurrentQueue<QueueItem>();

        /// <summary>异步发送数据</summary>
        /// <param name="buffer"></param>
        /// <param name="remote"></param>
        /// <returns></returns>
        public virtual Boolean SendAsync(Byte[] buffer, IPEndPoint remote) { return SendAsync_(buffer, remote); }

        internal Boolean SendAsync_(Byte[] buffer, IPEndPoint remote)
        {
            if (!Open()) return false;

            var count = buffer.Length;

            if (StatSend != null) StatSend.Increment(count);
            if (Log.Enable && LogSend) WriteLog("SendAsync [{0}]: {1}", count, buffer.ToHex(0, Math.Min(count, 32)));

            LastTime = DateTime.Now;

            // 估算完成时间，执行过长时提示
            using (var tc = new TimeCost("{0}.SendAsync".F(this.GetType().Name), 500))
            {
                tc.Log = Log;

                // 同时只允许一个异步发送，其它发送放入队列

                // 考虑到超长数据包，拆分为多个包
                var max = 1472;
                if (buffer.Length <= max)
                {
                    var qi = new QueueItem();
                    qi.Buffer = buffer;
                    qi.Remote = remote;

                    _SendQueue.Enqueue(qi);
                }
                else
                {
                    var ms = new MemoryStream(buffer);
                    while (true)
                    {
                        var remain = (Int32)(ms.Length - ms.Position);
                        if (remain <= 0) break;

                        var len = Math.Min(remain, max);

                        var qi = new QueueItem();
                        qi.Buffer = ms.ReadBytes(len);
                        qi.Remote = remote;

                        _SendQueue.Enqueue(qi);
                    }
                }

                CheckSendQueue(false);
            }

            return true;
        }

        void CheckSendQueue(Boolean io)
        {
            var qu = _SendQueue;
            if (qu.Count == 0) return;

            // 如果没有在发送，就开始发送
            if (Interlocked.CompareExchange(ref _Sending, 1, 0) != 0) return;

            //todo 如果是Tcp发送，这里可以考虑拿出来多个包合并发送
            QueueItem qi = null;
            if (!qu.TryDequeue(out qi)) return;

            var se = _seSend;
            if (se == null)
            {
                var buf = new Byte[1500];
                se = _seSend = new SocketAsyncEventArgs();
                se.SetBuffer(buf, 0, buf.Length);
                se.Completed += (s, e) => ProcessSend(e);
            }

            se.RemoteEndPoint = qi.Remote;

            // 拷贝缓冲区，设置长度
            {
                var p = 0;
                var len = qi.Buffer.Length;
                var max = 1472;
                var remote = qi.Remote;

                // 为了提高吞吐量，减少数据收发次数，尽可能的把发送队列多个数据包合并成为一个大包发出
                while (true)
                {
                    Buffer.BlockCopy(qi.Buffer, 0, se.Buffer, p, len);
                    p += len;

                    // 不足最大长度，试试下一个
                    if (!qu.TryPeek(out qi)) break;
                    if (qi.Remote != remote) break;
                    if (p + qi.Buffer.Length > max) break;

                    if (!qu.TryDequeue(out qi)) break;

                    len = qi.Buffer.Length;
                }

                se.SetBuffer(0, p);
            }

            if (!OnSendAsync(se))
            {
                if (io)
                    ProcessSend(se);
                else
                    Task.Factory.StartNew(s => ProcessSend(s as SocketAsyncEventArgs), se);
            }
        }

        internal abstract Boolean OnSendAsync(SocketAsyncEventArgs se);

        void ProcessSend(SocketAsyncEventArgs se)
        {
            if (!Active)
            {
                se.TryDispose();
                return;
            }

            // 判断成功失败
            if (se.SocketError != SocketError.Success)
            {
                // 未被关闭Socket时，可以继续使用
                //if (!se.IsNotClosed())
                {
                    var ex = se.GetException();
                    if (ex != null) OnError("SendAsync", ex);

                    se.TryDispose();

                    //if (se.SocketError == SocketError.ConnectionReset) Dispose();
                    if (se.SocketError == SocketError.ConnectionReset) Close("SendAsync " + se.SocketError);

                    return;
                }
            }

            // 发送新的数据
            if (Interlocked.CompareExchange(ref _Sending, 0, 1) == 1) CheckSendQueue(true);
        }

        class QueueItem
        {
            public Byte[] Buffer { get; set; }
            public IPEndPoint Remote { get; set; }
        }
        #endregion

        #region 接收
        /// <summary>接收数据</summary>
        /// <returns></returns>
        public abstract Byte[] Receive();

        /// <summary>读取指定长度的数据，一般是一帧</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        public abstract Int32 Receive(Byte[] buffer, Int32 offset = 0, Int32 count = -1);

        /// <summary>是否异步接收数据</summary>
        public Boolean UseReceiveAsync { get; set; }

        private Int32 _RecvCount;

        /// <summary>开始监听</summary>
        /// <returns>是否成功</returns>
        public virtual Boolean ReceiveAsync()
        {
            if (Disposed) throw new ObjectDisposedException(this.GetType().Name);

            if (!Open()) return false;

            if (!UseReceiveAsync) UseReceiveAsync = true;

            if (_RecvCount >= MaxAsync) return false;

            // 按照最大并发创建异步委托
            for (int i = _RecvCount; i < MaxAsync; i++)
            {
                if (Interlocked.Increment(ref _RecvCount) > MaxAsync)
                {
                    Interlocked.Decrement(ref _RecvCount);
                    return false;
                }

                var buf = new Byte[1500];
                var se = new SocketAsyncEventArgs();
                se.SetBuffer(buf, 0, buf.Length);
                se.Completed += (s, e) => ProcessReceive(e);

                WriteDebugLog("创建SA {0}", _RecvCount);

                if (se == null) return false;

                ReceiveAsync(se, false);
            }

            return true;
        }

        Boolean ReceiveAsync(SocketAsyncEventArgs se, Boolean io)
        {
            if (Disposed)
            {
                Interlocked.Decrement(ref _RecvCount);
                se.TryDispose();

                throw new ObjectDisposedException(this.GetType().Name);
            }

            var rs = false;
            try
            {
                // 开始新的监听
                rs = OnReceiveAsync(se);
            }
            catch (Exception ex)
            {
                Interlocked.Decrement(ref _RecvCount);
                se.TryDispose();

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
                    Task.Factory.StartNew(() => ProcessReceive(se));
            }

            return true;
        }

        internal abstract Boolean OnReceiveAsync(SocketAsyncEventArgs se);

        void ProcessReceive(SocketAsyncEventArgs se)
        {
            if (!Active)
            {
                se.TryDispose();
                return;
            }

            // 判断成功失败
            if (se.SocketError != SocketError.Success)
            {
                // 未被关闭Socket时，可以继续使用
                //if (!se.IsNotClosed())
                if (OnReceiveError(se))
                {
                    var ex = se.GetException();
                    if (ex != null) OnError("ReceiveAsync", ex);

                    se.TryDispose();

                    return;
                }
            }
            else
            {
                // 拷贝走数据，参数要重复利用
                var data = se.Buffer.ReadBytes(se.Offset, se.BytesTransferred);
                var ep = se.RemoteEndPoint as IPEndPoint;

                // 在用户线程池里面去处理数据
                //Task.Factory.StartNew(() => OnReceive(data, ep)).LogException(ex => OnError("OnReceive", ex));
                //ThreadPool.QueueUserWorkItem(s => OnReceive(data, ep));

                // 直接在IO线程调用业务逻辑
                try
                {
                    // 估算完成时间，执行过长时提示
                    using (var tc = new TimeCost("{0}.OnReceive".F(this.GetType().Name), 1000))
                    {
                        tc.Log = Log;

                        OnReceive(data, ep);
                    }
                }
                catch (Exception ex)
                {
                    if (!ex.IsDisposed()) OnError("OnReceive", ex);
                }
            }

            // 开始新的监听
            if (Active && !Disposed)
                ReceiveAsync(se, true);
            else
                se.TryDispose();
        }

        /// <summary>收到异常时如何处理</summary>
        /// <param name="se"></param>
        /// <returns>是否当作异常处理并结束会话</returns>
        internal virtual Boolean OnReceiveError(SocketAsyncEventArgs se)
        {
            //if (se.SocketError == SocketError.ConnectionReset) Dispose();
            if (se.SocketError == SocketError.ConnectionReset) Close("ReceiveAsync " + se.SocketError);

            return true;
        }

        /// <summary>处理收到的数据</summary>
        /// <param name="data"></param>
        /// <param name="remote"></param>
        internal abstract void OnReceive(Byte[] data, IPEndPoint remote);

        /// <summary>数据到达事件</summary>
        public event EventHandler<ReceivedEventArgs> Received;

        /// <summary>触发数据到达事件</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void RaiseReceive(Object sender, ReceivedEventArgs e)
        {
            LastTime = DateTime.Now;
            if (StatReceive != null) StatReceive.Increment(e.Length);

            if (Received != null) Received(sender, e);
        }
        #endregion

        #region 异常处理
        /// <summary>错误发生/断开连接时</summary>
        public event EventHandler<ExceptionEventArgs> Error;

        /// <summary>触发异常</summary>
        /// <param name="action">动作</param>
        /// <param name="ex">异常</param>
        protected virtual void OnError(String action, Exception ex)
        {
            if (Log != null) Log.Error("{0}{1}Error {2} {3}", LogPrefix, action, this, ex == null ? null : ex.Message);
            if (Error != null) Error(this, new ExceptionEventArgs { Action = action, Exception = ex });
        }
        #endregion

        #region 日志
        private String _LogPrefix;
        /// <summary>日志前缀</summary>
        public virtual String LogPrefix
        {
            get
            {
                if (_LogPrefix == null)
                {
                    var name = Name == null ? "" : Name.TrimEnd("Server", "Session", "Client");
                    _LogPrefix = "{0}.".F(name);
                }
                return _LogPrefix;
            }
            set { _LogPrefix = value; }
        }

        /// <summary>日志对象。禁止设为空对象</summary>
        public ILog Log { get; set; }

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

        /// <summary>输出调试日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        [Conditional("DEBUG")]
        public void WriteDebugLog(String format, params Object[] args)
        {
            if (Log != null && Log.Enable) Log.Info(LogPrefix + format, args);
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Local.ToString();
        }
        #endregion
    }
}