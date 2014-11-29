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
        #endregion

        #region 构造
        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            Close();
        }
        #endregion

        #region 方法
        /// <summary>打开</summary>
        public virtual void Open()
        {
            if (Active) return;

            UseReceiveAsync = Received != null;

            Active = OnOpen();

            if (UseReceiveAsync) ReceiveAsync();
        }

        /// <summary>打开</summary>
        /// <returns></returns>
        protected abstract Boolean OnOpen();

        /// <summary>关闭</summary>
        public virtual void Close()
        {
            if (!Active) return;

            if (OnClose()) Active = false;
        }

        /// <summary>关闭</summary>
        /// <returns></returns>
        protected abstract Boolean OnClose();

        /// <summary>连接</summary>
        /// <param name="remoteEP"></param>
        void ISocketClient.Connect(IPEndPoint remoteEP)
        {
            Remote.EndPoint = remoteEP;

            OnConnect(remoteEP);
        }

        /// <summary>连接</summary>
        /// <param name="remoteEP"></param>
        /// <returns></returns>
        protected abstract Boolean OnConnect(IPEndPoint remoteEP);

        /// <summary>发送数据</summary>
        /// <remarks>
        /// 目标地址由<seealso cref="Remote"/>决定
        /// </remarks>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        public abstract void Send(Byte[] buffer, Int32 offset = 0, Int32 count = -1);

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
        public abstract void ReceiveAsync();

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

        #region 日志
        private Boolean _Debug = Config.GetConfig<Boolean>("NewLife.Net.Debug", true);
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
            if (Remote != null)
                return String.Format("{0}=>{1}:{2}", Local, Remote.EndPoint, Remote.Port);
            else
                return Local.ToString();
        }
        #endregion
    }
}