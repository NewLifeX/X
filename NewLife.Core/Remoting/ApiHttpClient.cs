using System;
using NewLife.Http;
using NewLife.Net;

namespace NewLife.Remoting
{
    class ApiHttpClient : ApiNetClient
    {
        public override Boolean Init(Object config)
        {
            if (config is NetUri uri)
                Client = uri.CreateRemote();
            //else if (config is Uri)
            //    Client = ((Uri)config).CreateRemote();
            //else if (config is String)
            //    Client = new Uri(config + "").CreateRemote();

            // Http封包协议
            Client.Add<HttpCodec>();

            // 网络非法断开时，自动恢复
            Client.OnDisposed += (s, e) => { if (Active) { Init(config); Open(); } };

            return true;
        }
    }
}