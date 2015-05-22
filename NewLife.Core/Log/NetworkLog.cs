using System;
using System.Net;
using System.Net.Sockets;

namespace NewLife.Log
{
    /// <summary>网络日志</summary>
    public class NetworkLog : Logger, IDisposable
    {
        private Socket _Client;
        /// <summary>网络套接字</summary>
        public Socket Client { get { return _Client; } set { _Client = value; } }

        private IPEndPoint _Remote = new IPEndPoint(IPAddress.Broadcast, 514);
        /// <summary>远程服务器地址</summary>
        public IPEndPoint Remote { get { return _Remote; } set { _Remote = value; } }

        /// <summary>销毁</summary>
        public void Dispose()
        {
            if (Client != null) Client.Close();
        }

        private Boolean _inited;
        void Init()
        {
            if (_inited) return;

            // 默认Udp广播
            var client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            client.EnableBroadcast = true;
            Client = client;

            try
            {
                // 首先发送日志头
                client.SendTo(GetHead().GetBytes(), Remote);

                // 尝试向日志服务器表名身份
                var buf = "{0} {1}/{2} 准备上报日志".F(DateTime.Now.ToFullString(), Environment.UserName, Environment.MachineName).GetBytes();
                client.SendTo(buf, Remote);
            }
            catch { }

            _inited = true;
        }

        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected override void OnWrite(LogLevel level, String format, params Object[] args)
        {
            Init();

            var e = WriteLogEventArgs.Current.Set(level, Format(format, args), null, true);
            var buf = e.ToString().GetBytes();
            if (Client.ProtocolType == ProtocolType.Udp)
            {
                // 捕获异常，不能因为写日志异常导致上层出错
                try
                {
                    Client.SendTo(buf, Remote);
                }
                catch
                {
                    // 出错后重新初始化
                    _inited = false;
                }
            }
        }
    }
}