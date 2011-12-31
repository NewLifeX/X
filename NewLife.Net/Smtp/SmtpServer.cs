using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;
using System.Net.Sockets;

namespace NewLife.Net.Smtp
{
    /// <summary>Smtp服务器</summary>
    public class SmtpServer : NetServer
    {
        //TODO 未实现Smtp服务端

        #region 属性
        private Dictionary<Int32, SmtpSession> _Sessions;
        /// <summary>会话集合</summary>
        public IDictionary<Int32, SmtpSession> Sessions { get { return _Sessions ?? (_Sessions = new Dictionary<int, SmtpSession>()); } }
        #endregion

        /// <summary>实例化一个Smtp服务器</summary>
        public SmtpServer()
        {
            Port = 25;
            ProtocolType = ProtocolType.Tcp;
        }
    }
}