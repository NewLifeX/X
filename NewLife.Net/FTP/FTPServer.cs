using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;
using System.Net.Sockets;

namespace NewLife.Net.FTP
{
    /// <summary>FTP服务器</summary>
    public class FTPServer : NetServer<FTPSession>
    {
        //TODO 未实现FTP服务端

        #region 属性
        //private Dictionary<Int32, FTPSession> _Sessions;
        ///// <summary>会话集合</summary>
        //public IDictionary<Int32, FTPSession> Sessions { get { return _Sessions ?? (_Sessions = new Dictionary<int, FTPSession>()); } }
        #endregion

        /// <summary>实例化一个FTP服务器</summary>
        public FTPServer()
        {
            Port = 21;
            ProtocolType = ProtocolType.Tcp;
        }
    }
}