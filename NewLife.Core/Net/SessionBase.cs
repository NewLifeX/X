using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using NewLife.Log;

namespace NewLife.Net
{
    /// <summary>会话基类</summary>
    public abstract class SessionBase : DisposeBase, ISocketClient
    {
        #region 属性
        private String _Name;
        /// <summary>名称</summary>
        public String Name { get { return _Name; } set { _Name = value; } }

        private NetUri _Local = new NetUri();
        /// <summary>本地绑定信息</summary>
        public NetUri Local { get { return _Local; } set { _Local = value; } }

        /// <summary>端口</summary>
        public Int32 Port { get { return _Local.Port; } set { _Local.Port = value; } }

        private NetUri _Remote = new NetUri();
        /// <summary>远程结点地址</summary>
        public NetUri Remote { get { return _Remote; } set { _Remote = value; } }

        private Int32 _Timeout = 3000;
        /// <summary>超时。默认3000ms</summary>
        public Int32 Timeout { get { return _Timeout; } set { _Timeout = value; } }

        private Boolean _Active;
        /// <summary>是否活动</summary>
        public Boolean Active { get { return _Active; } set { _Active = value; } }

        private Stream _Stream = new MemoryStream();
        /// <summary>会话数据流。可用于解决Tcp粘包的问题，把多余的分片放入该数据流中。</summary>
        public Stream Stream { get { return _Stream; } set { _Stream = value; } }

        /// <summary>底层Socket</summary>
        public Socket Socket { get { return GetSocket(); } }

        /// <summary>获取Socket</summary>
        /// <returns></returns>
        internal abstract Socket GetSocket();

        private Boolean _ThrowException;
        /// <summary>是否抛出异常，默认false不抛出。Send/Receive时可能发生异常，该设置决定是直接抛出异常还是通过<see cref="Error"/>事件</summary>
        public Boolean ThrowException { get { return _ThrowException; } set { _ThrowException = value; } }

        private IStatistics _Statistics = new Statistics();
        /// <summary>统计信息</summary>
        public IStatistics Statistics { get { return _Statistics; } private set { _Statistics = value; } }

        private DateTime _StartTime = DateTime.Now;
        /// <summary>通信开始时间</summary>
        public DateTime StartTime { get { return _StartTime; } }

        private DateTime _LastTime;
        /// <summary>最后一次通信时间，主要表示活跃时间，包括收发</summary>
        public DateTime LastTime { get { return _LastTime; } internal protected set { _LastTime = value; } }
        #endregion

        #region 构造
        /// <summary>构造函数，初始化默认名称</summary>
        public SessionBase()
        {
            Name = this.GetType().Name;
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            try
            {
                Close();
            }
            catch (Exception ex) { OnError("Dispose", ex); }
        }
        #endregion

        #region 方法
        /// <summary>打开</summary>
        /// <returns>是否成功</returns>
        public virtual Boolean Open()
        {
            if (Disposed) throw new ObjectDisposedException(this.GetType().Name);

            //if (Disposed) return false;
            if (Active) return true;

            // 即使没有事件，也允许强行打开异步接收
            if (!UseReceiveAsync && Received != null) UseReceiveAsync = true;

            Active = OnOpen();
            if (!Active) return false;

            if (Port == 0) Port = (Socket.LocalEndPoint as IPEndPoint).Port;
            if (Timeout > 0) Socket.ReceiveTimeout = Timeout;

            // 触发打开完成的事件
            if (Opened != null) Opened(this, EventArgs.Empty);

            if (UseReceiveAsync) ReceiveAsync();

            return true;
        }

        /// <summary>打开</summary>
        /// <returns></returns>
        protected abstract Boolean OnOpen();

        /// <summary>关闭</summary>
        /// <returns>是否成功</returns>
        public virtual Boolean Close()
        {
            if (!Active) return true;

            if (OnClose()) Active = false;

            // 触发关闭完成的事件
            if (Closed != null) Closed(this, EventArgs.Empty);

            return !Active;
        }

        /// <summary>关闭</summary>
        /// <returns></returns>
        protected abstract Boolean OnClose();

        /// <summary>打开后触发。</summary>
        public event EventHandler Opened;

        /// <summary>关闭后触发。可实现掉线重连</summary>
        public event EventHandler Closed;

        /// <summary>发送数据</summary>
        /// <remarks>
        /// 目标地址由<seealso cref="Remote"/>决定
        /// </remarks>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <returns>是否成功</returns>
        public abstract Boolean Send(Byte[] buffer, Int32 offset = 0, Int32 count = -1);

        /// <summary>接收数据</summary>
        /// <returns></returns>
        public abstract Byte[] Receive();

        /// <summary>读取指定长度的数据，一般是一帧</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        public abstract Int32 Receive(Byte[] buffer, Int32 offset = 0, Int32 count = -1);
        #endregion

        #region 异步接收
        private Boolean _UseReceiveAsync;
        /// <summary>是否异步接收数据</summary>
        public Boolean UseReceiveAsync { get { return _UseReceiveAsync; } set { _UseReceiveAsync = value; } }

        private Boolean _UseProcessAsync = true;
        /// <summary>是否异步处理接收到的数据，默认true利于提升网络吞吐量。异步处理有可能造成数据包乱序，特别是Tcp</summary>
        public Boolean UseProcessAsync { get { return _UseProcessAsync; } set { _UseProcessAsync = value; } }

        /// <summary>开始异步接收</summary>
        /// <returns>是否成功</returns>
        public abstract Boolean ReceiveAsync();

        /// <summary>数据到达事件</summary>
        public event EventHandler<ReceivedEventArgs> Received;

        /// <summary>触发数据到达时间</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void RaiseReceive(Object sender, ReceivedEventArgs e)
        {
            _LastTime = DateTime.Now;

            Log.Debug("{0}.Receive {1} [{2}]: {3}", Name, Remote, e.Length, e.Data.ToHex(0, Math.Min(e.Length, 32)));

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
            if (Log != null) Log.Error("{0}.{1}Error {2} {3}", Name, action, this, ex == null ? null : ex.Message);
            if (Error != null) Error(this, new ExceptionEventArgs { Action = action, Exception = ex });
        }
        #endregion

        #region 日志
#if DEBUG
        private ILog _Log = XTrace.Log;
#else
        private ILog _Log = Logger.Null;
#endif
        /// <summary>日志对象</summary>
        public ILog Log { get { return _Log; } set { _Log = value; } }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            if (Log != null) Log.Info(format, args);
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