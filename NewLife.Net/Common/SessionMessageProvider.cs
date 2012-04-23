using System;
using System.IO;
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

            session.Received += new EventHandler<ReceivedEventArgs>(client_Received);
        }

        void client_Received(object sender, ReceivedEventArgs e)
        {
            try
            {
                //var message = Message.Read(e.Stream);
                var s = e.Stream;
                while (s.Position < s.Length && Message.IsMessage(s)) Process(Message.Read(s), Session.Session.RemoteUri);
            }
            catch (Exception ex)
            {
                //var msg = new ExceptionMessage() { Value = ex };
                //Process(msg);
                var msg = new ExceptionMessage() { Value = ex };
                var session = (sender as ISocketSession);
                session.Send(msg.GetStream());
            }
        }

        /// <summary>发送数据流。</summary>
        /// <param name="stream"></param>
        protected override void OnSend(Stream stream) { Session.Send(stream); }
    }
}