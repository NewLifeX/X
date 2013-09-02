using System;
using System.Net;
using System.Net.Sockets;
using NewLife.Model;

namespace NewLife.Net.Sockets
{
    /// <summary>针对异步模型进行封装的Socket服务器</summary>
    public class SocketServer : SocketBase, ISocketServer, IServer
    {
        #region 属性
        /// <summary>基础Socket对象</summary>
        public Socket Server { get { if (Socket == null) EnsureCreate(); return Socket; } set { Socket = value; } }
        #endregion

        #region 构造
        /// <summary>构造一个Socket服务器对象</summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public SocketServer(IPAddress address, Int32 port)
        {
            Address = address;
            Port = port;
        }

        /// <summary>构造一个Socket服务器对象</summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        public SocketServer(String hostname, Int32 port) : this(NetHelper.ParseAddress(hostname), port) { }

        /// <summary>已重载。释放会话集合等资源</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            Stop();
        }
        #endregion

        #region 开始停止
        private Boolean _Active;
        /// <summary>是否活动</summary>
        public Boolean Active { get { return _Active; } private set { _Active = value; } }

        /// <summary>开始监听</summary>
        public void Start()
        {
            if (Active) throw new InvalidOperationException("服务已经开始！");

            OnStart();

            Active = true;
        }

        /// <summary>开始时调用的方法</summary>
        protected virtual void OnStart()
        {
            EnsureCreate();

            Bind();
        }

        /// <summary>停止监听</summary>
        public void Stop()
        {
            //if (!Active) throw new InvalidOperationException("服务没有开始！");
            if (!Active) return;

            OnStop();

            Active = false;
        }

        /// <summary>停止时调用的方法</summary>
        protected virtual void OnStop() { Close(); }
        #endregion

        #region 事件
        /// <summary>已重载。服务器不会因为普通错误而关闭Socket停止服务</summary>
        /// <param name="e"></param>
        /// <param name="ex"></param>
        protected override void OnError(NetEventArgs e, Exception ex)
        {
            var isAborted = e.SocketError == SocketError.OperationAborted;
            try
            {
                ProcessError(e, ex);
            }
            finally
            {
                if (isAborted) Close();
            }
        }
        #endregion
    }
}