using System;
using NewLife.Net;

namespace NewLife.Remoting
{
    class ApiHttpClient : ApiNetClient
    {
        public override Boolean Init(Object config)
        {
            var uri = config as NetUri;
            if (uri != null)
                Client = uri.CreateRemote();
            else if (config is Uri)
                Client = ((Uri)config).CreateRemote();

            // 新生命标准网络封包协议
            Client.Packet = new DefaultPacket();

            return true;
        }
    }
}