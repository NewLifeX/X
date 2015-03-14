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
        [Description("UDP服务端")]
        UDP_Server,
        [Description("UDP客户端")]
        UDP_Client,
        [Description("TCP服务端")]
        TCP_Server,
        [Description("TCP客户端")]
        TCP_Client,

        [Description("Echo服务")]
        Echo,

        [Description("Chargen服务")]
        Chargen,

        [Description("Daytime服务")]
        Daytime,

        [Description("Time服务")]
        Time
    }
}