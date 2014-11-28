using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NewLife.Net
{
    /// <summary>会话基类</summary>
    public abstract class SessionBase : DisposeBase
    {
        #region 属性
        private NetUri _Local;
        /// <summary>本地绑定信息</summary>
        public NetUri Local { get { return _Local; } set { _Local = value; } }

        /// <summary>端口</summary>
        public Int32 Port { get { return _Local.Port; } set { _Local.Port = value; } }

        private NetUri _Remote;
        /// <summary>远程结点地址</summary>
        public NetUri Remote { get { return _Remote; } set { _Remote = value; } }

        private Int32 _Timeout = 3000;
        /// <summary>超时。默认3000ms</summary>
        public Int32 Timeout { get { return _Timeout; } set { _Timeout = value; } }
        #endregion

        #region 构造
        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            Close();
        }
        #endregion

        #region 方法
        /// <summary>打开</summary>
        public abstract void Open();

        /// <summary>关闭</summary>
        public abstract void Close();

        /// <summary>发送数据</summary>
        /// <remarks>
        /// 目标地址由<seealso cref="Remote"/>决定，如需精细控制，可直接操作<seealso cref="Client"/>
        /// </remarks>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        public abstract void Send(Byte[] buffer, Int32 offset = 0, Int32 count = -1);

        /// <summary>读取指定长度的数据，一般是一帧</summary>
        /// <remarks>如需直接返回数据，可直接操作<seealso cref="Client"/></remarks>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        public abstract Int32 Receive(Byte[] buffer, Int32 offset = 0, Int32 count = -1);
        #endregion

        #region 异步接收
        /// <summary>开始异步接收</summary>
        public abstract void ReceiveAsync();

        /// <summary>数据到达事件</summary>
        public event EventHandler<ReceivedEventArgs> Received;

        /// <summary>触发数据到达时间</summary>
        /// <param name="e"></param>
        protected virtual void RaiseReceive(ReceivedEventArgs e)
        {
            if (Received != null) Received(this, e);
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

    /// <summary>会话扩展</summary>
    public static class SessionHelper
    {
        /// <summary>发送数据流</summary>
        /// <param name="session"></param>
        /// <param name="stream"></param>
        /// <returns>返回自身，用于链式写法</returns>
        public static SessionBase Send(this SessionBase session, Stream stream)
        {
            Int64 total = 0;

            var size = 1472;
            var buffer = new Byte[size];
            while (true)
            {
                var count = stream.Read(buffer, 0, buffer.Length);
                if (count <= 0) break;

                session.Send(buffer, 0, count);
                total += count;

                if (count < buffer.Length) break;
            }
            return session;
        }

        /// <summary>向指定目的地发送信息</summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <param name="encoding"></param>
        /// <param name="remoteEP"></param>
        /// <returns>返回自身，用于链式写法</returns>
        public static SessionBase Send(this SessionBase session, String message, Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.UTF8;

            session.Send(encoding.GetBytes(message));

            return session;
        }

        /// <summary>接收字符串</summary>
        /// <param name="session"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static String ReceiveString(this SessionBase session, Encoding encoding = null)
        {
            var buf = new Byte[1500];
            var count = session.Receive(buf);
            if (count == 0) return null;

            if (encoding == null) encoding = Encoding.UTF8;
            return encoding.GetString(buf, 0, count);
        }
    }
}