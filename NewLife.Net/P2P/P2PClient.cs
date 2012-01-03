using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;

namespace NewLife.Net.P2P
{
    /// <summary>P2P客户端</summary>
    public class P2PClient
    {
        #region 属性
        private ISocketServer _Server;
        /// <summary>监听服务器</summary>
        public ISocketServer Server { get { return _Server; } set { _Server = value; } }

        private ISocketClient _Client;
        /// <summary>客户端</summary>
        public ISocketClient Client { get { return _Client; } set { _Client = value; } }
        #endregion
    }
}