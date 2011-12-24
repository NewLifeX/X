using System;
using System.IO;
using System.Text;
using NewLife.IO;
using NewLife.Net.Sockets;

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
            String header = stream.ToStr(Encoding.ASCII);
            //String src = String.Format("HOST: {0}", session.Server.Address);
            //if (session.Server.Port != 80) src += ":" + session.Server.Port;
            //String des = String.Format("HOST: {0}", "www.qq.com");
            String[] ss = header.Split(new String[] { Environment.NewLine }, StringSplitOptions.None);
            for (int i = 0; i < ss.Length; i++)
            {
                if (ss[i].StartsWith("host:", StringComparison.OrdinalIgnoreCase))
                {
                    ss[i] = String.Format("HOST: {0}", "www.baidu.com");
                    break;
                }
            }

            //header = header.Replace(src, des);
            header = String.Join(Environment.NewLine, ss);
            stream = new MemoryStream(Encoding.ASCII.GetBytes(header));

            return stream;
        }
    }
}