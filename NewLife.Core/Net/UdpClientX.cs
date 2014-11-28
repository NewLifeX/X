using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NewLife.Net
{
    /// <summary>增强的UDP客户端</summary>
    public class UdpClientX : DisposeBase
    {
        #region 属性
        private NetUri _Local = new NetUri(ProtocolType.Udp, IPAddress.Any, 0);
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

        private UdpClient _Client;
        /// <summary>客户端</summary>
        public UdpClient Client { get { return _Client; } set { _Client = value; } }
        #endregion

        #region 构造
        /// <summary>使用监听口初始化</summary>
        /// <param name="listenPort"></param>
        public UdpClientX(Int32 listenPort)
        {
            Port = listenPort;
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            Close();
        }
        #endregion

        #region 方法
        /// <summary>打开</summary>
        public void Open()
        {
            if (Client == null || !Client.Client.IsBound)
            {
                Client = new UdpClient(Port);
                if (Timeout > 0) Client.Client.ReceiveTimeout = Timeout;
            }
        }

        /// <summary>关闭</summary>
        public void Close()
        {
            if (Client != null) Client.Close();
            Client = null;
        }

        /// <summary>发送数据</summary>
        /// <remarks>
        /// 目标地址由<seealso cref="Remote"/>决定，如需精细控制，可直接操作<seealso cref="Client"/>
        /// </remarks>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        public void Send(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
        {
            Open();

            if (count < 0) count = buffer.Length - offset;

            var sp = Client;
            lock (sp)
            {
                //if (Client.Client.Connected)
                //{
                //    if (offset == 0)
                //        sp.Send(buffer, count);
                //    else
                //        sp.Send(buffer.ReadBytes(offset, count), count);
                //}
                //else
                {
                    if (offset == 0)
                        sp.Send(buffer, count, Remote.EndPoint);
                    else
                        sp.Send(buffer.ReadBytes(offset, count), count, Remote.EndPoint);
                }
            }
        }

        /// <summary>读取指定长度的数据，一般是一帧</summary>
        /// <remarks>如需直接返回数据，可直接操作<seealso cref="Client"/></remarks>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        public Int32 Receive(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
        {
            Open();

            if (count < 0) count = buffer.Length - offset;

            var size = 0;
            var sp = Client;

            IPEndPoint remoteEP = null;
            var data = Client.Receive(ref remoteEP);
            Remote.EndPoint = remoteEP;
            if (data != null && data.Length > 0)
            {
                size = data.Length;
                // 计算还有多少可用空间
                if (size > count) size = count;
                buffer.Write(offset, data, 0, size);
            }

            return size;
        }
        #endregion

        #region 异步接收
        /// <summary>开始监听</summary>
        public void ReceiveAsync()
        {
            if (Client == null) Open();

            // 开始新的监听
            Client.BeginReceive(OnReceive, Client);
        }

        void OnReceive(IAsyncResult ar)
        {
            // 接收数据
            IPEndPoint ep = null;
            var data = Client.EndReceive(ar, ref ep);
            Remote.EndPoint = ep;

            // 开始新的监听
            Client.BeginReceive(OnReceive, Client);

            OnReceive(data, ep);
        }

        /// <summary>处理收到的数据</summary>
        /// <param name="data"></param>
        /// <param name="remote"></param>
        protected virtual void OnReceive(Byte[] data, IPEndPoint remote)
        {
            // 分析处理
            if (Received != null)
            {
                var e = new UdpReceivedEventArgs();
                e.Data = data;
                e.Remote = remote;

                Received(this, e);

                // 数据发回去
                if (e.Feedback) Client.Send(e.Data, e.Length, e.Remote);
            }
        }

        /// <summary>数据到达事件，事件里调用<see cref="Receive"/>读取数据</summary>
        public event EventHandler<ReceivedEventArgs> Received;
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

    /// <summary>收到Udp数据包的事件参数</summary>
    public class UdpReceivedEventArgs : ReceivedEventArgs
    {
        private IPEndPoint _Remote;
        /// <summary>远程地址</summary>
        public IPEndPoint Remote { get { return _Remote; } set { _Remote = value; } }
    }


    /// <summary>Udp扩展</summary>
    public static class UdpHelper
    {
        /// <summary>发送数据流</summary>
        /// <param name="udp"></param>
        /// <param name="stream"></param>
        /// <param name="remoteEP"></param>
        /// <returns>返回自身，用于链式写法</returns>
        public static UdpClient Send(this UdpClient udp, Stream stream, IPEndPoint remoteEP = null)
        {
            Int64 total = 0;

            //var size = stream.CanSeek ? stream.Length - stream.Position : udp.BufferSize;
            var size = 1472;
            Byte[] buffer = new Byte[size];
            while (true)
            {
                Int32 n = stream.Read(buffer, 0, buffer.Length);
                if (n <= 0) break;

                udp.Send(buffer, n, remoteEP);
                total += n;

                if (n < buffer.Length) break;
            }
            return udp;
        }

        /// <summary>向指定目的地发送信息</summary>
        /// <param name="udp"></param>
        /// <param name="buffer">缓冲区</param>
        /// <param name="remoteEP"></param>
        /// <returns>返回自身，用于链式写法</returns>
        public static UdpClient Send(this UdpClient udp, Byte[] buffer, IPEndPoint remoteEP = null)
        {
            udp.Send(buffer, buffer.Length, remoteEP);
            return udp;
        }

        /// <summary>向指定目的地发送信息</summary>
        /// <param name="udp"></param>
        /// <param name="message"></param>
        /// <param name="encoding"></param>
        /// <param name="remoteEP"></param>
        /// <returns>返回自身，用于链式写法</returns>
        public static UdpClient Send(this UdpClient udp, String message, Encoding encoding = null, IPEndPoint remoteEP = null)
        {
            //Send(udp, Encoding.UTF8.GetBytes(message), remoteEP);

            if (encoding == null)
                Send(udp, Encoding.UTF8.GetBytes(message), remoteEP);
            else
                Send(udp, encoding.GetBytes(message), remoteEP);
            return udp;
        }

        /// <summary>广播数据包</summary>
        /// <param name="udp"></param>
        /// <param name="buffer">缓冲区</param>
        /// <param name="port"></param>
        public static UdpClient Broadcast(this UdpClient udp, Byte[] buffer, Int32 port)
        {
            if (!udp.EnableBroadcast) udp.EnableBroadcast = true;

            udp.Send(buffer, buffer.Length, new IPEndPoint(IPAddress.Broadcast, port));

            return udp;
        }

        /// <summary>广播字符串</summary>
        /// <param name="udp"></param>
        /// <param name="message"></param>
        /// <param name="port"></param>
        public static UdpClient Broadcast(this UdpClient udp, String message, Int32 port)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            return Broadcast(udp, buffer, port);
        }

        /// <summary>接收字符串</summary>
        /// <param name="udp"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static String ReceiveString(this UdpClient udp, Encoding encoding = null)
        {
            IPEndPoint ep = null;
            Byte[] buffer = udp.Receive(ref ep);
            if (buffer == null || buffer.Length < 1) return null;

            if (encoding == null) encoding = Encoding.UTF8;
            return encoding.GetString(buffer);
        }
    }
}