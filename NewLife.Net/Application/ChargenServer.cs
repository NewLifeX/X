using System;
using System.Net.Sockets;
using System.Threading;
using NewLife.Net.Sockets;

namespace NewLife.Net.Application
{
    /// <summary>Chargen服务器。不停的向连接者发送数据</summary>
    public class ChargenServer : NetServer
    {
        /// <summary>实例化一个Chargen服务</summary>
        public ChargenServer()
        {
            //// 默认Tcp协议
            //ProtocolType = ProtocolType.Tcp;
            // 默认19端口
            Port = 19;

            Name = "Chargen服务";
        }

        /// <summary>已重载。</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnAccepted(object sender, NetEventArgs e)
        {
            WriteLog("Chargen {0}", e.Session.RemoteUri);

            // 如果没有远程地址，或者远程地址是广播地址，则跳过。否则会攻击广播者。
            // Tcp的该属性可能没值，可以忽略
            var remote = e.RemoteIPEndPoint;
            if (remote != null && remote.Address.IsAny()) return;

            // 使用多线程
            Thread thread = new Thread(LoopSend);
            thread.Name = "Chargen.LoopSend";
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.Lowest;
            thread.Start(e.Session);

            // 调用基类，为接收数据准备，避免占用过大内存
            base.OnAccepted(sender, e);
        }

        /// <summary>已重载。</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnReceived(object sender, NetEventArgs e)
        {
            if (e.BytesTransferred > 100)
                WriteLog("Chargen {0} [{1}]", e.Session.RemoteUri, e.BytesTransferred);
            else
                WriteLog("Chargen {0} [{1}] {2}", e.Session.RemoteUri, e.BytesTransferred, e.GetString());
        }

        /// <summary>出错时</summary>
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
            var session = state as ISocketSession;
            if (session == null) return;

            hasError = false;

            try
            {
                // 不断的发送数据，直到连接断开为止
                while (!hasError)
                {
                    try
                    {
                        Send(session);

                        // 暂停100ms
                        Thread.Sleep(100);
                    }
                    catch { break; }
                }
            }
            finally
            {
                //session.Disconnect();
                session.Dispose();
            }
        }

        Int32 Length = 72;
        Int32 Index = 0;

        void Send(ISocketSession session)
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

            //Send(sender, buffer, 0, buffer.Length, remoteEP);
            session.Send(buffer, 0, buffer.Length);
        }
    }
}