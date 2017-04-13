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
            else if (config is String)
                Client = new Uri(config + "").CreateRemote();

            // Http封包协议
            //Client.Packet = new HttpPacket();
            //Client.Packet = new PacketProvider { Offset = -1 };

            return true;
        }
    }
}