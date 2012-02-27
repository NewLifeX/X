using System;
using System.Net;
using NewLife.Messaging;
using NewLife.Net.Sockets;

namespace NewLife.Net.Common
{
    /// <summary>网络会话消息提供者</summary>
    public class SessionMessageProvider : MessageProvider
    {
        private INetSession _Session;
        /// <summary>网络会话</summary>
        public INetSession Session { get { return _Session; } set { _Session = value; } }

        /// <summary>实例化一个网络会话消息提供者</summary>
        /// <param name="session"></param>
        public SessionMessageProvider(INetSession session)
        {
            Session = session;

            session.Received += new EventHandler<NetEventArgs>(client_Received);
        }

        void client_Received(object sender, NetEventArgs e)
        {
            var message = Message.Read(e.GetStream());
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