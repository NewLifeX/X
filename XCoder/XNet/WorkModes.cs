using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace XNet
{
    /// <summary>工作模式</summary>
    enum WorkModes
    {
        [Description("TCP/UDP混合")]
        UDP_TCP = 1,
        [Description("UDP")]
        UDP,
        [Description("TCP服务端")]
        TCP_Server,
        [Description("TCP客户端")]
        TCP_Client
    }
}