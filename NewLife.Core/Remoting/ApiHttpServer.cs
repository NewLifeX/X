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

            //// Http不能使用封包协议
            //SessionPacket = null;

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