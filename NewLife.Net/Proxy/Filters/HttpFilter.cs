using System;
using System.IO;
using System.Text;
using NewLife.IO;
using NewLife.Net.Sockets;
using NewLife.Net.Protocols.Http;

namespace NewLife.Net.Proxy
{
    /// <summary>Http过滤器</summary>
    public class HttpFilter : ProxyFilterBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="stream"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public override Stream OnClientToServer(IProxySession session, Stream stream, NetEventArgs e)
        {
            // 解析请求头
            var entity = HttpHeader.Read(stream);
            Console.WriteLine("请求：{0}", entity.RawUrl);

            entity.Headers["Host"] = "www.baidu.com";

            var ms = new MemoryStream();
            entity.Write(ms);
            stream.CopyTo(ms);
            ms.Position = 0;
            stream = ms;

            return stream;
        }
    }
}