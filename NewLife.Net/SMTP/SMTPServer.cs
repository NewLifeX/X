using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;
using System.Net.Sockets;

namespace NewLife.Net.SMTP
{
    /// <summary>SMTP服务器</summary>
    public class SMTPServer : NetServer
    {
        //TODO 未实现SMTP服务端

        #region 属性
        private Dictionary<Int32, SMTPSession> _Sessions;
        /// <summary>会话集合</summary>
        public IDictionary<Int32, SMTPSession> Sessions { get { return _Sessions ?? (_Sessions = new Dictionary<int, SMTPSession>()); } }
        #endregion

        /// <summary>实例化一个SMTP服务器</summary>
        public SMTPServer()
        {
            Port = 25;
            ProtocolType = ProtocolType.Tcp;
        }
    }
}