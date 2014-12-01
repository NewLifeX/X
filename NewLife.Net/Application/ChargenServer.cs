using System;
using System.IO;
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
        /// <param name="session"></param>
        protected override void OnAccept(ISocketSession session)
        {
            WriteLog("Chargen {0}", session.Remote);

            // 如果没有远程地址，或者远程地址是广播地址，则跳过。否则会攻击广播者。
            // Tcp的该属性可能没值，可以忽略
            var remote = session.Remote.EndPoint;
            if (remote != null && remote.Address.IsAny()) return;

            // 使用多线程
            var thread = new Thread(LoopSend);
            thread.Name = "Chargen.LoopSend";
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.Lowest;
            thread.Start(session);

            // 调用基类，为接收数据准备，避免占用过大内存
            //base.OnAccepted(sender, e);
        }

        /// <summary>已重载。</summary>
        /// <param name="session"></param>
        /// <param name="stream"></param>
        protected override void OnReceive(ISocketSession session, Stream stream)
        {
            if (stream.Length == 0) return;

            if (stream.Length > 100)
                WriteLog("Chargen {0} [{1}]", session.Remote, stream.Length);
            else
                WriteLog("Chargen {0} [{1}] {2}", session.Remote, stream.Length, stream.ToStr());
        }

        /// <summary>触发异常</summary>
        /// <param name="action">动作</param>
        /// <param name="ex">异常</param>
        protected override void OnError(String action, Exception ex)
        {
            base.OnError(action, ex);

            // 出现重置错误，可能是UDP发送时客户端重置了，标识错误，让所有循环终止
            //if (e.LastOperation == SocketAsyncOperation.ReceiveFrom && e.SocketError == SocketError.ConnectionReset) hasError = true;
            hasError = true;
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

            session.Send(buffer);
        }
    }
}