using System;
using System.IO;
using NewLife.IO;
using NewLife.Net.Protocols.Http;
using NewLife.Net.Sockets;
using System.Net;

namespace NewLife.Net.Proxy
{
    /// <summary>Http代理。可用于代理各种Http通讯请求。</summary>
    /// <remarks>Http代理请求与普通请求唯一的不同就是Uri，Http代理请求收到的是可能包括主机名的完整Uri</remarks>
    public class HttpProxy : ProxyBase
    {
        /// <summary>创建会话</summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected override INetSession CreateSession(NetEventArgs e)
        {
            return new Session();
        }

        #region 会话
        /// <summary>Http反向代理会话</summary>
        public class Session : ProxySession
        {
            /// <summary>代理对象</summary>
            public new HttpReverseProxy Proxy { get { return base.Proxy as HttpReverseProxy; } set { base.Proxy = value; } }

            /// <summary>收到客户端发来的数据。子类可通过重载该方法来修改数据</summary>
            /// <param name="e"></param>
            /// <param name="stream">数据</param>
            /// <returns>修改后的数据</returns>
            protected override Stream OnReceive(NetEventArgs e, Stream stream)
            {
                // 解析请求头
                var entity = HttpHeader.Read(stream);
                WriteLog("请求：{0}", entity.RawUrl);

                //entity.Headers["Host"] = Proxy.ServerAddress;
                if (entity.Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    entity.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    var uri = new Uri(entity.Url);
                    var host = uri.Host;
                    var port = uri.Port;
                    RemoteEndPoint = new IPEndPoint(NetHelper.ParseAddress(host), port);
                }

                var ms = new MemoryStream();
                entity.Write(ms);
                stream.CopyTo(ms);
                ms.Position = 0;

                return ms;
            }
        }
        #endregion
    }
}