using System.ComponentModel;
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
        }

        /// <summary>创建会话</summary>
        /// <param name="session"></param>
        /// <returns></returns>
        protected override INetSession CreateSession(ISocketSession session) => new HttpSession();
    }
}