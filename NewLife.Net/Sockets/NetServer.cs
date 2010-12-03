using System;
using System.Net;
using System.Net.Sockets;

namespace NewLife.Net.Sockets
{
    /// <summary>
    /// 网络服务器
    /// </summary>
    /// <remarks>
    /// 网络服务器模型，所有网络应用服务器可以通过继承该类实现。
    /// 该类仅实现了业务应用对网络流的操作，与具体网络协议无关。
    /// </remarks>
    public abstract class NetServer : Netbase
    {
        #region 属性
        private IPAddress _Address = IPAddress.Any;
        /// <summary>监听本地地址</summary>
        public IPAddress Address
        {
            get { return _Address; }
            set { _Address = value; }
        }

        private Int32 _Port;
        /// <summary>端口</summary>
        public Int32 Port
        {
            get { return _Port; }
            set { _Port = value; }
        }

        private SocketServer _Server;
        /// <summary>服务器</summary>
        public SocketServer Server
        {
            get { return _Server; }
            set { _Server = value; }
        }

        /// <summary>是否活动</summary>
        public Boolean Active
        {
            get { return _Server == null ? false : _Server.Active; }
        }

        private String _Name;
        /// <summary>服务名</summary>
        public String Name
        {
            get { return _Name ?? (_Name = GetType().Name); }
            set { _Name = value; }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 开始
        /// </summary>
        public void Start()
        {
            if (Active) throw new InvalidOperationException("服务已经开始！");

            OnStart();
        }

        /// <summary>
        /// 确保建立服务器
        /// </summary>
        protected abstract void EnsureCreateServer();

        /// <summary>
        /// 开始时调用的方法
        /// </summary>
        protected virtual void OnStart()
        {
            EnsureCreateServer();

            Server.Error += new EventHandler<NetEventArgs>(OnError);
            Server.Start();

            WriteLog("{0} 开始监听{1}", Name, Server.Server.LocalEndPoint);
        }

        /// <summary>
        /// 断开连接/发生错误
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnError(object sender, NetEventArgs e)
        {
            if (e.SocketError != SocketError.Success || e.UserToken is Exception)
                WriteLog("{2}错误 {0} {1}", e.SocketError, e.UserToken as Exception, e.LastOperation);
            else
                WriteLog("{0}断开！", e.LastOperation);
        }

        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {
            if (!Active) throw new InvalidOperationException("服务没有开始！");

            WriteLog("{0}停止监听{1}", this.GetType().Name, Server.Server.LocalEndPoint);

            OnStop();
        }

        /// <summary>
        /// 停止时调用的方法
        /// </summary>
        protected virtual void OnStop()
        {
            Dispose();
        }

        /// <summary>
        /// 子类重载实现资源释放逻辑
        /// </summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            // 释放托管资源
            //if (disposing)
            {
                if (Server != null) Server.Stop();
            }
        }
        #endregion
    }
}