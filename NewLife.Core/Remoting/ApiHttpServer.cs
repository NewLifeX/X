using System;
using NewLife.Http;
using NewLife.Net;

namespace NewLife.Remoting
{
    class ApiHttpServer : ApiNetServer
    {
        #region 属性
        private String RawUrl;
        #endregion

        public ApiHttpServer()
        {
            Name = "Http";

            ProtocolType = NetType.Http;
        }

        /// <summary>初始化</summary>
        /// <param name="config"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        public override Boolean Init(Object config, IApiHost host)
        {
            Host = host;

            var uri = config as NetUri;
            Port = uri.Port;

            RawUrl = uri + "";

            // Http封包协议
            Add<HttpCodec>();

            host.Handler = new ApiHttpHandler { Host = host };
            host.Encoder = new HttpEncoder();

            return true;
        }
    }

    class ApiHttpHandler : ApiHandler
    {

    }
}