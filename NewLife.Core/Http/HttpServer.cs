using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Net;

namespace NewLife.Http
{
    /// <summary>Http服务器</summary>
    [DisplayName("Http服务器")]
    public class HttpServer : NetServer
    {
        /// <summary>实例化</summary>
        public HttpServer()
        {
            Name = "Http";
            Port = 80;
            ProtocolType = NetType.Http;

            // Http封包协议
            //SessionPacket = new PacketFactory { Offset = -1 };
        }
    }
}