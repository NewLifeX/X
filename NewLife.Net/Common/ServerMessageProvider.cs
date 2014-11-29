using System;
using System.IO;
using System.Net.Sockets;
using NewLife.Messaging;
using NewLife.Net.Sockets;
using NewLife.Reflection;

namespace NewLife.Net.Common
{
    /// <summary>网络服务消息提供者</summary>
    /// <remarks>
    /// 服务端是异步接收，在处理消息时不方便进一步了解网络相关数据，可通过<see cref="Message.UserState"/>附带用户会话。
    /// 采用线程静态的弱引用<see cref="Session"/>来保存用户会话，便于发送消息。
    /// </remarks>
    public class ServerMessageProvider : MessageProvider
    {
        private NetServer _Server;
        /// <summary>网络会话</summary>
        public NetServer Server
        {
            get { return _Server; }
            set
            {
                _Server = value;
                if (value != null)
                    MaxMessageSize = value.ProtocolType == ProtocolType.Udp ? 1472 : 1460;
                else
                    MaxMessageSize = 0;
            }
        }

        /// <summary>实例化一个网络服务消息提供者</summary>
        /// <param name="server"></param>
        public ServerMessageProvider(NetServer server)
        {
            Server = server;

            server.Received += new EventHandler<NetEventArgs>(server_Received);
        }

        /// <summary>当前会话</summary>
        [ThreadStatic]
        private static WeakReference<ISocketClient> Session = null;

        void server_Received(object sender, NetEventArgs e)
        {
            var session = e.Session;
            Session = new WeakReference<ISocketClient>(session);
            var stream = e.GetStream();
            // 如果上次还留有数据，复制进去
            if (session.Stream != null && session.Stream.Position < session.Stream.Length)
            {
                // 这个流是上一次的完整数据，位置在最后，直接合并即可
                var ms = session.Stream;
                var p = ms.Position;
                ms.Position = ms.Length;
                stream.CopyTo(ms);
                ms.Position = p;
                stream = ms;
            }

            OnReceive(session, stream);
        }

        /// <summary>收到数据流</summary>
        /// <param name="session"></param>
        /// <param name="stream"></param>
        protected virtual void OnReceive(ISocketClient session, Stream stream)
        {
            try
            {
                Process(stream, session, session.Remote);

                // 如果还有剩下，写入数据流，供下次使用
                if (stream.Position < stream.Length)
                {
                    var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    ms.Position = 0;
                    session.Stream = ms;
                }
                else
                    session.Stream = null;
            }
            catch (Exception ex)
            {
                if (NetHelper.Debug) NetHelper.WriteLog(ex.ToString());

                // 去掉内部异常，以免过大
                if (ex.InnerException != null) ex.SetValue("_innerException", null);
                var msg = new ExceptionMessage() { Value = ex };
                //session.Send(msg.GetStream());
                OnSend(msg.GetStream());

                // 出错后清空数据流，避免连锁反应
                session.Stream = null;
            }
        }

        /// <summary>发送数据流。</summary>
        /// <param name="stream"></param>
        protected override void OnSend(Stream stream)
        {
            var session = Session.Target;
            // 如果为空，这里让它报错，否则无法查找问题所在
            //if (session != null) session.Send(stream);
            session.Send(stream);
        }
    }
}