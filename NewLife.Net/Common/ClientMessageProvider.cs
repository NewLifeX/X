using System;
using System.Net;
using NewLife.Messaging;
using NewLife.Net.Sockets;

namespace NewLife.Net.Common
{
    /// <summary>客户端消息提供者</summary>
    public class ClientMessageProvider : MessageProvider
    {
        private ISocketClient _Client;
        /// <summary>客户端</summary>
        public ISocketClient Client { get { return _Client; } set { _Client = value; } }

        private IPEndPoint _Remote;
        /// <summary>远程</summary>
        public IPEndPoint Remote { get { return _Remote; } set { _Remote = value; } }

        /// <summary>实例化一个客户端消息提供者</summary>
        /// <param name="client"></param>
        /// <param name="ep"></param>
        public ClientMessageProvider(ISocketClient client, IPEndPoint ep)
        {
            Client = client;
            Remote = ep;

            client.Received += new EventHandler<NetEventArgs>(client_Received);
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
            Client.Send(message.GetStream(), Remote);
        }
    }
}