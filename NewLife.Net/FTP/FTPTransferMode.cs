using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.FTP
{
    /// <summary>传输模式</summary>
    public enum FTPTransferMode
    {
        /// <summary>主动。服务器主动连接客户端</summary>
        Active,

        /// <summary>被动。客户端连接服务器</summary>
        Passive
    }
}