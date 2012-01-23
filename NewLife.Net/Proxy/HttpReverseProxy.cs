using System;
using System.IO;
using NewLife.IO;
using NewLife.Net.Http;
using NewLife.Net.Sockets;

namespace NewLife.Net.Proxy
{
    /// <summary>Http反向代理。把所有收到的Http请求转发到目标服务器。</summary>
    /// <remarks>
    /// 主要是修改Http请求头为正确的主机，还有可能修改Http响应。
    /// 
    /// 经典用途：
    /// 1，缓存。代理缓存某些静态资源的请求结果，减少对服务器的请求压力
    /// 2，拦截。禁止访问某些资源，返回空白页或者连接重置
    /// 3，修改请求或响应。更多的可能是修改响应的页面内容
    /// 4，记录统计。记录并统计请求的网址。
    /// 
    /// 修改Http响应的一般做法：
    /// 1，反向映射888端口到目标abc.com
    /// 2，abc.com页面响应时，所有http://abc.com/的连接都修改为http://IP:888
    /// 3，注意在内网的反向代理需要使用公网IP，而不是本机IP
    /// 4，子域名也可以修改，比如http://pic.abc.com/修改为http://IP:888/http_pic.abc.com/
    /// </remarks>
    public class HttpReverseProxy : NATProxy
    {
        /// <summary>实例化</summary>
        public HttpReverseProxy()
        {
            Port = 80;
        }

        /// <summary>创建会话</summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected override INetSession CreateSession(NetEventArgs e)
        {
            return new Session();
        }

        #region 会话
        /// <summary>Http反向代理会话</summary>
        class Session : NATSession
        {
            ///// <summary>代理对象</summary>
            //public new HttpReverseProxy Proxy { get { return base.Proxy as HttpReverseProxy; } set { base.Proxy = value; } }

            private HttpHeader _Request;
            /// <summary>请求头部</summary>
            public HttpHeader Request { get { return _Request; } set { _Request = value; } }

            private String _Host;
            /// <summary>属性说明</summary>
            public String Host { get { return _Host; } set { _Host = value; } }

            private String _RawHost;
            /// <summary>属性说明</summary>
            public String RawHost { get { return _RawHost; } set { _RawHost = value; } }

            /// <summary>请求时触发。</summary>
            public event EventHandler<EventArgs<NetEventArgs, Stream, HttpHeader>> OnRequest;

            /// <summary>收到客户端发来的数据。子类可通过重载该方法来修改数据</summary>
            /// <param name="e"></param>
            /// <param name="stream">数据</param>
            /// <returns>修改后的数据</returns>
            protected override Stream OnReceive(NetEventArgs e, Stream stream)
            {
                // 解析请求头
                var entity = HttpHeader.Read(stream, HttpHeaderReadMode.Request);
                if (entity == null) return base.OnReceive(e, stream);

                WriteLog("{3}请求：{0} {1} [{2}]", entity.Method, entity.Url, entity.ContentLength, ID);

                if (OnRequest != null) OnRequest(this, new EventArgs<NetEventArgs, Stream, HttpHeader>(e, stream, entity));

                var host = entity.Url.IsAbsoluteUri ? entity.Url.Host : Proxy.ServerHost;
                Host = host;
                RawHost = entity.Host;
                entity.Host = host;

                // 引用
                var r = entity.Referer;
                if (!String.IsNullOrEmpty(r))
                {
                    Uri ri = new Uri(r, UriKind.RelativeOrAbsolute);
                    if (ri.IsAbsoluteUri && ri.Authority == RawHost)
                    {
                        r = r.Replace(RawHost, host);
                        entity.Referer = r;
                    }
                }

                //// 取消压缩
                //var key = "Accept-Encoding";
                //if (entity.Headers.ContainsKey(key)) entity.Headers.Remove(key);

                // 重新构造请求
                var ms = new MemoryStream();
                entity.Write(ms);
                stream.CopyTo(ms);
                ms.Position = 0;

                return ms;
            }

            ///// <summary>收到客户端发来的数据。子类可通过重载该方法来修改数据</summary>
            ///// <param name="e"></param>
            ///// <param name="stream">数据</param>
            ///// <returns>修改后的数据</returns>
            //protected override Stream OnReceiveRemote(NetEventArgs e, Stream stream)
            //{
            //    //var entity = HttpHeader.Read(stream, HttpHeaderReadMode.Response);
            //    //if (entity == null) return base.OnReceive(e, stream);

            //    var html = e.GetString();
            //    html = html.Replace(Host, RawHost);
            //    stream = new MemoryStream(Encoding.UTF8.GetBytes(html));

            //    return base.OnReceiveRemote(e, stream);
            //}
        }
        #endregion
    }
}