using System;
using System.IO;
using System.Net.Sockets;
using NewLife.Messaging;
using NewLife.Net.Sockets;

namespace NewLife.Net.Common
{
    /// <summary>客户端消息提供者</summary>
    public class ClientMessageProvider : MessageProvider
    {
        private ISocketSession _Session;
        /// <summary>客户端</summary>
        public ISocketSession Session
        {
            get { return _Session; }
            set
            {
                if (_Session != value)
                {
                    _Session = value;

                    if (value != null && value.UseReceiveAsync)
                    {
                        value.Received += new EventHandler<ReceivedEventArgs>(client_Received);

                        //if (!value.UseReceiveAsync) value.ReceiveAsync();
                    }
                }
            }
        }

        /// <summary>Session被关闭需要更新时触发。外部应该重新给Session赋值</summary>
        public event EventHandler OnUpdate;

        void client_Received(object sender, ReceivedEventArgs e)
        {
            try
            {
                var message = Message.Read(e.Stream);
                Process(message);
            }
            catch (Exception ex)
            {
                var msg = new ExceptionMessage() { Value = ex };
                Process(msg);
            }
        }

        /// <summary>发送消息。如果有响应，可在消息到达事件中获得。</summary>
        /// <param name="message"></param>
        public override void Send(Message message)
        {
            var session = GetSession();

            //Session.Send(message.GetStream());
            var ms = message.GetStream();
            if (ms.Length < 1460)
                session.Send(ms);
            else
            {
                var mg = new MessageGroup();
                mg.Split(ms, 1460);
                foreach (var item in mg)
                {
                    session.Send(item.GetStream());
                }
            }
        }

        ISocketSession GetSession()
        {
            var session = Session;
            Boolean needUpdate = false;
            if (session == null || session.Disposed || session.Host == null)
                needUpdate = true;
            else
            {
                var socket = session.Host.Socket;
                if (socket == null || socket.ProtocolType == ProtocolType.Tcp && !socket.Connected)
                    needUpdate = true;
            }

            if (needUpdate && OnUpdate != null) OnUpdate(this, EventArgs.Empty);

            return Session;
        }

        /// <summary>发送并接收</summary>
        /// <param name="message"></param>
        /// <param name="millisecondsTimeout"></param>
        /// <returns></returns>
        public override Message SendAndReceive(Message message, int millisecondsTimeout = 0)
        {
            var session = GetSession();
            if (session.UseReceiveAsync) return base.SendAndReceive(message, millisecondsTimeout);

            Send(message);
            var data = session.Receive();
            if (data == null || data.Length < 1) return null;

            var ms = new MemoryStream(data);
            return Message.Read(ms);
        }
    }
}