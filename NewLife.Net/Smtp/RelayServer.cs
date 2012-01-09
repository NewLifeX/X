using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;
using System.Net.Sockets;

namespace NewLife.Net.Smtp
{
    /// <summary>Relay服务器</summary>
    public class RelayServer : NetServer<RelaySession>
    {
        //TODO 未实现Relay服务端

        #region 属性
        //private Dictionary<Int32, RelaySession> _Sessions;
        ///// <summary>会话集合</summary>
        //public IDictionary<Int32, RelaySession> Sessions { get { return _Sessions ?? (_Sessions = new Dictionary<int, RelaySession>()); } }
        #endregion

        /// <summary>实例化一个Relay服务器</summary>
        public RelayServer()
        {
            Port = 25;
            ProtocolType = ProtocolType.Tcp;
        }
    }
}