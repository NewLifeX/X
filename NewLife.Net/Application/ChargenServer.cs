using System;
using System.Net.Sockets;
using NewLife.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using NewLife.Net.Tcp;

namespace NewLife.Net.Application
{
    /// <summary>
    /// Chargen服务器
    /// </summary>
    public class ChargenServer : NetServer
    {
        /// <summary>
        /// 实例化一个Chargen服务
        /// </summary>
        public ChargenServer()
        {
            // 默认Tcp协议
            ProtocolType = ProtocolType.Tcp;
            // 默认19端口
            Port = 19;
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        protected override void EnsureCreateServer()
        {
            base.EnsureCreateServer();

            Name = String.Format("Chargen服务（{0}）", ProtocolType);

            // 允许同时处理多个数据包
            Server.NoDelay = ProtocolType == ProtocolType.Udp;
            // 使用线程池来处理事件
            Server.UseThreadPool = true;
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnAccepted(object sender, NetEventArgs e)
        {
            WriteLog("Chargen {0}", e.RemoteEndPoint);

            //if (ProtocolType == ProtocolType.Tcp)
            //{
            // 使用多线程
            Thread thread = new Thread(LoopSend);
            thread.Name = "Chargen.LoopSend";
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.Lowest;
            thread.Start(new Object[] { e.UserToken, e.RemoteEndPoint });
            //}
            //else if (ProtocolType == ProtocolType.Udp)
            //{
            //    Send(e.UserToken as SocketBase, e.RemoteEndPoint as IPEndPoint);
            //}

            // 调用基类，为接收数据准备，避免占用过大内存
            base.OnAccepted(sender, e);
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnReceived(object sender, NetEventArgs e)
        {
            if (e.BytesTransferred > 100)
                WriteLog("Chargen {0} [{1}]", e.RemoteEndPoint, e.BytesTransferred);
            else
                WriteLog("Chargen {0} [{1}] {2}", e.RemoteEndPoint, e.BytesTransferred, e.GetString());
        }

        /// <summary>
        /// 出错时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnError(object sender, NetEventArgs e)
        {
            base.OnError(sender, e);

            // 出现重置错误，可能是UDP发送时客户端重置了，标识错误，让所有循环终止
            if (e.LastOperation == SocketAsyncOperation.ReceiveFrom && e.SocketError == SocketError.ConnectionReset) hasError = true;
        }

        Boolean hasError = false;

        void LoopSend(Object state)
        {
            //TcpClientX client = state as TcpClientX;
            SocketBase client = ((Object[])state)[0] as SocketBase;
            if (client == null) return;

            IPEndPoint remote = ((Object[])state)[1] as IPEndPoint;
            hasError = false;

            try
            {
                // 不断的发送数据，直到连接断开为止
                while (!hasError)
                {
                    try
                    {
                        Send(client, remote);

                        // 暂停100ms
                        Thread.Sleep(100);
                    }
                    catch
                    {
                        break;
                    }
                }
            }
            finally
            {
                //client.Close();
                Disconnect(client);
            }
        }

        Int32 Length = 72;
        Int32 Index = 0;

        void Send(SocketBase sender, IPEndPoint remoteEP)
        {
            Int32 startIndex = Index++;
            if (Index >= Length) Index = 0;

            Byte[] buffer = new Byte[Length];

            // 产生数据
            for (int i = 0; i < buffer.Length; i++)
            {
                Int32 p = startIndex + i;
                if (p >= buffer.Length) p -= buffer.Length;
                buffer[p] = (Byte)(i + 32);
            }

            Send(sender, buffer, 0, buffer.Length, remoteEP);
        }
    }
}