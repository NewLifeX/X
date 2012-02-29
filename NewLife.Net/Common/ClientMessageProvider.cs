using System;
using System.Net;
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
    }
}