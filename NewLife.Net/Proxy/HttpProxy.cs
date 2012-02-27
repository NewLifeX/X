using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using NewLife.IO;
using NewLife.Net.Http;
using NewLife.Net.Sockets;
using NewLife.Serialization;

namespace NewLife.Net.Proxy
{
    /// <summary>Http代理。可用于代理各种Http通讯请求。</summary>
    /// <remarks>Http代理请求与普通请求唯一的不同就是Uri，Http代理请求收到的是可能包括主机名的完整Uri</remarks>
    public class HttpProxy : ProxyBase<HttpProxy.Session>
    {
        #region 构造
        /// <summary>实例化</summary>
        public HttpProxy()
        {
            Port = 8080;
            ProtocolType = ProtocolType.Tcp;
        }
        #endregion

        #region 事件
        /// <summary>收到请求时发生。</summary>
        public event EventHandler<HttpProxyEventArgs> OnRequest;

        /// <summary>收到响应时发生。</summary>
        public event EventHandler<HttpProxyEventArgs> OnResponse;

        /// <summary>收到请求主体时发生。</summary>
        public event EventHandler<HttpProxyEventArgs> OnRequestBody;

        /// <summary>收到响应主体时发生。</summary>
        public event EventHandler<HttpProxyEventArgs> OnResponseBody;

        /// <summary>
        /// 返回是否取消操作
        /// </summary>
        /// <param name="kind"></param>
        /// <param name="he"></param>
        /// <returns></returns>
        Boolean RaiseEvent(EventKind kind, HttpProxyEventArgs he)
        {
            var handler = GetHandler(kind);

            if (handler != null)
            {
                handler(this, he);

                //return he.Cancel;
            }

            //return false;
            return he.Cancel;
        }

        EventHandler<HttpProxyEventArgs> GetHandler(EventKind kind)
        {
            switch (kind)
            {
                case EventKind.OnRequest:
                    return OnRequest;
                case EventKind.OnResponse:
                    return OnResponse;
                case EventKind.OnRequestBody:
                    return OnRequestBody;
                case EventKind.OnResponseBody:
                    return OnResponseBody;
                default:
                    break;
            }
            return null;
        }

        enum EventKind
        {
            OnRequest,
            OnResponse,
            OnRequestBody,
            OnResponseBody
        }
        #endregion

        #region 缓存
        private Boolean _EnableCache;
        /// <summary>激活缓存</summary>
        public Boolean EnableCache
        {
            get { return _EnableCache; }
            set
            {
                if (value)
                {
                    OnRequest += new EventHandler<HttpProxyEventArgs>(HttpProxy_OnRequest);
                    OnResponse += new EventHandler<HttpProxyEventArgs>(HttpProxy_OnResponse);
                }
                else
                {
                    OnResponse -= new EventHandler<HttpProxyEventArgs>(HttpProxy_OnResponse);
                }

                _EnableCache = value;
            }
        }

        void HttpProxy_OnRequest(object sender, HttpProxyEventArgs e)
        {
            throw new NotImplementedException();
        }

        void HttpProxy_OnResponse(object sender, HttpProxyEventArgs e)
        {
            // 缓存Get请求的304响应
            var request = e.Session.CurrentRequest;
            var response = e.Header;
            if (request != null && request.Method.EqualIgnoreCase("GET")
                && response != null && request.StatusCode == 304)
            {

            }
        }

        private HttpCache _Cache;
        /// <summary>Http缓存</summary>
        HttpCache Cache { get { return _Cache ?? (_Cache = new HttpCache()); } set { _Cache = value; } }
        #endregion

        #region 会话
        /// <summary>Http反向代理会话</summary>
        public class Session : ProxySession<HttpProxy, Session>
        {
            /// <summary>
            /// 当前正在处理的请求。一个连接同时只能处理一个请求，除非是Http 1.2
            /// </summary>
            HttpHeader Request;

            /// <summary>
            /// 已完成处理，正在转发数据的请求头
            /// </summary>
            public HttpHeader CurrentRequest;

            //HttpCacheItem CacheItem = null;

            /// <summary>收到客户端发来的数据。子类可通过重载该方法来修改数据</summary>
            /// <remarks>
            /// 如果数据包包括头部和主体，可以分开处理。
            /// 最麻烦的就是数据包不是一个完整的头部，还落了一部分在后面的包上。
            /// </remarks>
            /// <param name="e"></param>
            /// <param name="stream">数据</param>
            /// <returns>修改后的数据</returns>
            protected override Stream OnReceive(NetEventArgs e, Stream stream)
            {
                // 解析请求头。
                var entity = Request;
                if (entity == null)
                {
                    // 如果当前请求为空，说明这是第一个数据包，可能包含头部
                    entity = HttpHeader.Read(stream, HttpHeaderReadMode.Request);
                    if (entity == null)
                    {
                        var he = new HttpProxyEventArgs(this, CurrentRequest, e, stream);
                        if (Proxy.RaiseEvent(EventKind.OnRequestBody, he)) return null;
                        return base.OnReceive(e, he.Stream);
                    }

                    if (!entity.IsFinish)
                        Request = entity;
                    else
                        CurrentRequest = entity;
                }
                else if (!entity.IsFinish)
                {
                    // 如果请求未完成，说明现在的数据内容还是头部
                    entity.ReadHeaders(new BinaryReaderX(stream));
                    if (entity.IsFinish)
                    {
                        CurrentRequest = entity;
                        Request = null;
                    }
                }
                else
                {
                    // 否则，头部已完成，现在就是内容
                    var he = new HttpProxyEventArgs(this, CurrentRequest, e, stream);
                    if (Proxy.RaiseEvent(EventKind.OnRequestBody, he)) return null;
                    return base.OnReceive(e, he.Stream);
                }

                // 请求头不完整，不发送，等下一部分到来
                if (!entity.IsFinish) return null;

                var rs = OnRequest(entity, e, stream);
                {
                    var he = new HttpProxyEventArgs(this, CurrentRequest, e, stream);
                    he.Cancel = !rs;
                    rs = !Proxy.RaiseEvent(EventKind.OnRequest, he);
                }
                if (!rs) return null;

                if (stream.Position < stream.Length)
                {
                    var he = new HttpProxyEventArgs(this, CurrentRequest, e, stream);
                    if (Proxy.RaiseEvent(EventKind.OnRequestBody, he)) return null;
                    stream = he.Stream;
                }

                // 重新构造请求
                var ms = new MemoryStream();
                entity.Write(ms);
                stream.CopyTo(ms);
                ms.Position = 0;

                return ms;
            }

            String LastHost;
            Int32 LastPort;

            /// <summary>
            /// 是否保持连接
            /// </summary>
            Boolean KeepAlive = false;

            /// <summary>收到请求时</summary>
            /// <param name="entity"></param>
            /// <param name="e"></param>
            /// <param name="stream"></param>
            /// <returns></returns>
            protected virtual Boolean OnRequest(HttpHeader entity, NetEventArgs e, Stream stream)
            {
                var host = "";
                var oriUrl = entity.Url;

                // 特殊处理CONNECT
                #region 特殊处理CONNECT
                if (entity.Method.EqualIgnoreCase("CONNECT"))
                {
                    WriteLog("请求：{0} {1} [{2}]", entity.Method, entity.Url, entity.ContentLength);

                    host = entity.Url.ToString();
                    RemoteEndPoint = NetHelper.ParseEndPoint(entity.Url.ToString(), 80);

                    // 不要连自己，避免死循环
                    if (RemoteEndPoint.Port == Proxy.Server.Port &&
                        (RemoteEndPoint.Address == IPAddress.Loopback || RemoteEndPoint.Address == IPAddress.IPv6Loopback))
                    {
                        this.Dispose();
                        return false;
                    }

                    var rs = new HttpHeader();
                    rs.Version = entity.Version;
                    try
                    {
                        // 连接远程服务器，启动数据交换
                        if (Remote == null) StartRemote(e);

                        rs.StatusCode = 200;
                        rs.StatusDescription = "OK";
                    }
                    catch (Exception ex)
                    {
                        rs.StatusCode = 500;
                        rs.StatusDescription = ex.Message;
                    }

                    var session = Session;
                    if (session != null) session.Send(rs.GetStream(), ClientEndPoint);
                    return false;
                }
                #endregion

                if (entity.Url.IsAbsoluteUri)
                {
                    var uri = entity.Url;
                    host = uri.Host + ":" + uri.Port;

                    // 如果地址或端口改变，则重新连接服务器
                    if (Remote != null && (uri.Host != LastHost || uri.Port != LastPort))
                    {
                        Remote.Dispose();
                        Remote = null;
                    }
                    LastHost = uri.Host;
                    LastPort = uri.Port;

                    RemoteEndPoint = new IPEndPoint(NetHelper.ParseAddress(uri.Host), uri.Port);
                    entity.Url = new Uri(uri.PathAndQuery, UriKind.Relative);
                }
                else if (!String.IsNullOrEmpty(entity.Host))
                {
                    RemoteEndPoint = NetHelper.ParseEndPoint(entity.Host, 80);
                }
                else
                    throw new NetException("无法处理的请求！{0}", entity);

                WriteLog("请求：{0} {1} [{2}]", entity.Method, oriUrl, entity.ContentLength);

                // 可能不含Host
                if (String.IsNullOrEmpty(entity.Host)) entity.Host = host;

                // 处理KeepAlive
                KeepAlive = false;
                if (!String.IsNullOrEmpty(entity.ProxyConnection))
                {
                    entity.KeepAlive = entity.ProxyKeepAlive;
                    entity.ProxyConnection = null;

                    KeepAlive = entity.ProxyKeepAlive;
                }

                #region 缓存
                // 缓存Get请求的304响应
                if (Proxy.EnableCache && entity != null && entity.Method.EqualIgnoreCase("GET"))
                {
                    // 查找缓存
                    var citem = Proxy.Cache.GetItem(entity.Url.ToString());
                    if (citem != null)
                    {
                        // 响应缓存
                        var cs = citem.Stream;
                        lock (cs)
                        {
                            var p = cs.Position;
                            Send(cs);
                            cs.Position = p;
                        }
                        return false;
                    }
                }
                #endregion

                return true;
            }

            HttpHeader Response;
            HttpHeader CurrentResponse;

            /// <summary>收到客户端发来的数据。子类可通过重载该方法来修改数据</summary>
            /// <param name="e"></param>
            /// <param name="stream">数据</param>
            /// <returns>修改后的数据</returns>
            protected override Stream OnReceiveRemote(NetEventArgs e, Stream stream)
            {
                var parseHeader = Proxy.GetHandler(EventKind.OnResponse) != null;
                var parseBody = Proxy.GetHandler(EventKind.OnResponseBody) != null;

                if (parseHeader || parseBody)
                {
                    // 解析头部
                    var entity = Response;
                    if (entity == null)
                    {
                        // 如果当前请求为空，说明这是第一个数据包，可能包含头部
                        entity = HttpHeader.Read(stream, HttpHeaderReadMode.Response);
                        if (entity == null)
                        {
                            var he = new HttpProxyEventArgs(this, CurrentResponse, e, stream);
                            if (Proxy.RaiseEvent(EventKind.OnResponseBody, he)) return null;
                            return base.OnReceiveRemote(e, he.Stream);
                        }

                        if (!entity.IsFinish)
                            Response = entity;
                        else
                            CurrentResponse = entity;
                    }
                    else if (!entity.IsFinish)
                    {
                        // 如果请求未完成，说明现在的数据内容还是头部
                        entity.ReadHeaders(new BinaryReaderX(stream));
                        if (entity.IsFinish)
                        {
                            CurrentResponse = entity;
                            Response = null;
                        }
                    }
                    else
                    {
                        // 否则，头部已完成，现在就是内容
                        var he = new HttpProxyEventArgs(this, CurrentResponse, e, stream);
                        if (Proxy.RaiseEvent(EventKind.OnResponseBody, he)) return null;
                        return base.OnReceiveRemote(e, he.Stream);
                    }

                    // 请求头不完整，不发送，等下一部分到来
                    if (!entity.IsFinish) return null;

                    {
                        var he = new HttpProxyEventArgs(this, entity, e, stream);
                        if (Proxy.RaiseEvent(EventKind.OnResponse, he)) return null;
                        stream = he.Stream;
                    }

                    // 重新构造请求
                    var ms = new MemoryStream();
                    entity.Write(ms);
                    stream.CopyTo(ms);
                    ms.Position = 0;

                    stream = ms;
                }

                if (parseBody && stream.Position < stream.Length)
                {
                    var he = new HttpProxyEventArgs(this, CurrentResponse, e, stream);
                    if (Proxy.RaiseEvent(EventKind.OnResponseBody, he)) return null;
                    stream = he.Stream;
                }

                return base.OnReceiveRemote(e, stream);
            }

            ///// <summary>收到响应时</summary>
            ///// <param name="entity"></param>
            ///// <param name="e"></param>
            ///// <param name="stream"></param>
            ///// <returns></returns>
            //protected virtual Boolean OnResponse(HttpHeader entity, NetEventArgs e, Stream stream)
            //{

            //}

            /// <summary>远程连接断开时触发。默认销毁整个会话，子类可根据业务情况决定客户端与代理的链接是否重用。</summary>
            /// <param name="client"></param>
            protected override void OnRemoteDispose(ISocketClient client)
            {
                // 如果客户端不要求保持连接，就销毁吧
                if (!KeepAlive) base.OnRemoteDispose(client);
            }
        }
        #endregion

        #region 浏览器代理
        struct Struct_INTERNET_PROXY_INFO
        {
            public int dwAccessType;
            public IntPtr proxy;
            public IntPtr proxyBypass;
        }

        /// <summary>定义API函数</summary>
        /// <param name="hInternet"></param>
        /// <param name="dwOption"></param>
        /// <param name="lpBuffer"></param>
        /// <param name="lpdwBufferLength"></param>
        /// <returns></returns>
        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int lpdwBufferLength);

        /// <summary>设置IE代理。传入空地址取消代理设置</summary>
        /// <param name="proxy">地址与端口以冒号分开</param>
        /// <param name="proxyOverride">代理是否跳过本地地址</param>
        public static void SetIEProxy(string proxy, Boolean proxyOverride = true)
        {
            const int INTERNET_OPTION_PROXY = 38;
            const int INTERNET_OPEN_TYPE_PROXY = 3;
            const int INTERNET_OPEN_TYPE_DIRECT = 1;

            Boolean isCancel = String.IsNullOrEmpty(proxy);

            Struct_INTERNET_PROXY_INFO info;

            // 填充结构体 
            info.dwAccessType = !isCancel ? INTERNET_OPEN_TYPE_PROXY : INTERNET_OPEN_TYPE_DIRECT;
            info.proxy = Marshal.StringToHGlobalAnsi("" + proxy);
            info.proxyBypass = Marshal.StringToHGlobalAnsi("local");

            // 分配内存
            IntPtr ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(info));

            // 获取结构体指针
            Marshal.StructureToPtr(info, ptr, true);

            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
            if (!isCancel)
            {
                key.SetValue("ProxyServer", proxy);
                key.SetValue("ProxyEnable", 1);
                if (proxyOverride)
                    key.SetValue("ProxyOverride", "<local>");
                else
                    key.DeleteValue("ProxyOverride");
            }
            else
                key.SetValue("ProxyEnable", 0);
            key.Close();

            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_PROXY, ptr, Marshal.SizeOf(info));

            const int INTERNET_OPTION_REFRESH = 0x000025;
            const int INTERNET_OPTION_SETTINGS_CHANGED = 0x000027;
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }
        #endregion
    }

    /// <summary>Http代理事件参数</summary>
    public class HttpProxyEventArgs : EventArgs
    {
        private HttpProxy.Session _Session;
        /// <summary>会话</summary>
        public HttpProxy.Session Session { get { return _Session; } set { _Session = value; } }

        private HttpHeader _Header;
        /// <summary>头部</summary>
        public HttpHeader Header { get { return _Header; } set { _Header = value; } }

        private NetEventArgs _Arg;
        /// <summary>网络时间参数</summary>
        public NetEventArgs Arg { get { return _Arg; } set { _Arg = value; } }

        private Stream _Stream;
        /// <summary>主体数据流。外部可以更改，如果只是读取，请一定注意保持指针在原来的位置</summary>
        public Stream Stream { get { return _Stream; } set { _Stream = value; } }

        private Boolean _Cancel;
        /// <summary>是否取消操作</summary>
        public Boolean Cancel { get { return _Cancel; } set { _Cancel = value; } }

        /// <summary>
        /// 实例化
        /// </summary>
        public HttpProxyEventArgs() { }

        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="e"></param>
        /// <param name="stream"></param>
        public HttpProxyEventArgs(HttpProxy.Session session, HttpHeader header, NetEventArgs e, Stream stream)
        {
            Session = session;
            Header = header;
            Arg = e;
            Stream = stream;
        }
    }
}