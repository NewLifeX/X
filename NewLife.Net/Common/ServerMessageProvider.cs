using System;
using System.IO;
using NewLife.Messaging;
using NewLife.Net.Sockets;

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
        public NetServer Server { get { return _Server; } set { _Server = value; } }

        /// <summary>实例化一个网络服务消息提供者</summary>
        /// <param name="server"></param>
        public ServerMessageProvider(NetServer server)
        {
            Server = server;

            server.Received += new EventHandler<NetEventArgs>(server_Received);
        }

        /// <summary>当前会话</summary>
        [ThreadStatic]
        private static WeakReference<ISocketSession> Session = null;

        void server_Received(object sender, NetEventArgs e)
        {
            var session = e.Session;
            Session = new WeakReference<ISocketSession>(session);
            var s = e.GetStream();
            try
            {
                while (s.Position < s.Length && Message.IsMessage(s))
                {
                    var msg = Message.Read(s);
                    msg.UserState = session;
                    Process(msg);
                }
            }
            catch (Exception ex)
            {
                var msg = new ExceptionMessage() { Value = ex };
                session.Send(msg.GetStream());
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