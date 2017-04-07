using System;
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
        /// <returns></returns>
        public override Boolean Init(String config)
        {
            RawUrl = config;

            if (!base.Init(config)) return false;

            // Http封包协议
            //SessionPacket = new HttpPacketFactory();
            SessionPacket = new PacketFactory { Offset = -1 };

            return true;
        }

        /// <summary>获取服务提供者</summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public override Object GetService(Type serviceType)
        {
            if (serviceType == typeof(ApiHttpServer)) return Provider;

            return base.GetService(serviceType);
        }
    }
}