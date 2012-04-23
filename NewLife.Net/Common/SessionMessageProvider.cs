using System;
using System.IO;
using NewLife.Messaging;
using NewLife.Net.Sockets;
using NewLife.Reflection;

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
            var session = Session.Session;
            var s = e.Stream;
            // 如果上次还留有数据，复制进去
            if (session.Stream != null && session.Stream.Position < session.Stream.Length)
            {
                var ms = new MemoryStream();
                session.Stream.CopyTo(ms);
                s.CopyTo(ms);
                s = ms;
            }
            try
            {
                Process(s, session, session.RemoteUri);

                // 如果还有剩下，写入数据流，供下次使用
                if (s.Position < s.Length)
                    session.Stream = s;
                else
                    session.Stream = null;
            }
            catch (Exception ex)
            {
                if (NetHelper.Debug) NetHelper.WriteLog(ex.ToString());

                // 去掉内部异常，以免过大
                if (ex.InnerException != null) FieldInfoX.SetValue(ex, "_innerException", null);
                var msg = new ExceptionMessage() { Value = ex };
                session.Send(msg.GetStream());

                // 出错后清空数据流，避免连锁反应
                session.Stream = null;
            }
        }

        /// <summary>发送数据流。</summary>
        /// <param name="stream"></param>
        protected override void OnSend(Stream stream) { Session.Send(stream); }
    }
}