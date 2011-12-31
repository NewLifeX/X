using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;
using System.Net.Sockets;

namespace NewLife.Net.POP3
{
    /// <summary>POP3服务器</summary>
    public class POP3Server : NetServer
    {
        //TODO 未实现POP3服务端

        #region 属性
        private Dictionary<Int32, POP3Session> _Sessions;
        /// <summary>会话集合</summary>
        public IDictionary<Int32, POP3Session> Sessions { get { return _Sessions ?? (_Sessions = new Dictionary<int, POP3Session>()); } }
        #endregion

        /// <summary>实例化一个POP3服务器</summary>
        public POP3Server()
        {
            Port = 110;
            ProtocolType = ProtocolType.Tcp;
        }
    }
}