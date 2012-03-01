using System;
using System.IO;
using NewLife.Messaging;
using NewLife.Net.Sockets;

namespace NewLife.Net.Common
{
    /// <summary>客户端消息提供者</summary>
    public class ClientMessageProvider : MessageProvider
    {
        private ISocketSession _Session;
        /// <summary>客户端</summary>
        public ISocketSession Session { get { return _Session; } set { _Session = value; } }

        /// <summary>实例化一个客户端消息提供者</summary>
        /// <param name="session"></param>
        public ClientMessageProvider(ISocketSession session)
        {
            Session = session;

            session.Received += new EventHandler<ReceivedEventArgs>(client_Received);
        }

        void client_Received(object sender, ReceivedEventArgs e)
        {
            var message = Message.Read(e.Stream);
            OnReceive(message);
        }

        /// <summary>发送消息</summary>
        /// <param name="message"></param>
        public override void Send(Message message)
        {
            Session.Send(message.GetStream());
        }

        /// <summary>接收消息。这里将得到所有消息</summary>
        /// <param name="millisecondsTimeout">等待的毫秒数，或为 <see cref="F:System.Threading.Timeout.Infinite" /> (-1)，表示无限期等待。默认0表示不等待</param>
        /// <returns></returns>
        public override Message Receive(int millisecondsTimeout = 0)
        {
            var session = Session;
            if (session.UseReceiveAsync)
            {
                var bts = session.Receive();
                if (bts == null || bts.Length <= 0) return null;

                return Message.Read(new MemoryStream(bts));
            }

            return base.Receive(millisecondsTimeout);
        }
    }
}