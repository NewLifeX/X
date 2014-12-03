using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using NewLife.Configuration;
using NewLife.Log;

namespace NewLife.Net
{
    /// <summary>会话基类</summary>
    public abstract class SessionBase : DisposeBase, ISocketClient
    {
        #region 属性
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
        /// <summary>会话数据流，供用户程序使用，内部不做处理。可用于解决Tcp粘包的问题，把多余的分片放入该数据流中。</summary>
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
        #endregion

        #region 构造
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
            if (Disposed) return false;
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

        ///// <summary>连接</summary>
        ///// <param name="remoteEP"></param>
        //void ISocketClient.Connect(IPEndPoint remoteEP)
        //{
        //    Remote.EndPoint = remoteEP;

        //    Open();

        //    if (Socket == null || !Socket.Connected) return;

        //    WriteLog("{0} Connect {1}", this, remoteEP);

        //    OnConnect(remoteEP);
        //}

        ///// <summary>连接</summary>
        ///// <param name="remoteEP"></param>
        ///// <returns></returns>
        //protected abstract Boolean OnConnect(IPEndPoint remoteEP);

        //void ISocketClient.Disconnect()
        //{
        //    if (Socket == null || !Socket.Connected) return;

        //    WriteLog("{0} Disconnect {1}", this, Remote.EndPoint);

        //    OnDisconnect();
        //}

        ///// <summary>断开连接</summary>
        ///// <returns></returns>
        //protected virtual Boolean OnDisconnect()
        //{
        //    if (Socket != null && Socket.Connected) Socket.Disconnect(true);
        //    return true;
        //}

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
            WriteLog("{0}.{1}Error {2} {3}", this.GetType().Name, action, this, ex == null ? null : ex.Message);
            if (Error != null) Error(this, new ExceptionEventArgs { Action = action, Exception = ex });
        }
        #endregion

        #region 日志
        private Boolean _Debug = false;
        /// <summary>调试开关</summary>
        public Boolean Debug { get { return _Debug; } set { _Debug = value; } }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            if (Debug) XTrace.WriteLine(format, args);
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            //if (Remote != null)
            //    return String.Format("{0}=>{1}", Local, Remote.EndPoint);
            //else
            return Local.ToString();
        }
        #endregion
    }
}