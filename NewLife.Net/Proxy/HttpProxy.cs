using System;
using System.IO;
using System.Net;
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
        ///// <summary>创建会话</summary>
        ///// <param name="e"></param>
        ///// <returns></returns>
        //protected override INetSession CreateSession(NetEventArgs e)
        //{
        //    return new Session();
        //}

        #region 会话
        /// <summary>Http反向代理会话</summary>
        public class Session : ProxySession<HttpProxy, Session>
        {
            HttpHeader Request;

            /// <summary>收到客户端发来的数据。子类可通过重载该方法来修改数据</summary>
            /// <param name="e"></param>
            /// <param name="stream">数据</param>
            /// <returns>修改后的数据</returns>
            protected override Stream OnReceive(NetEventArgs e, Stream stream)
            {
                // 解析请求头
                var entity = Request;
                if (entity == null)
                {
                    entity = HttpHeader.Read(stream, HttpHeaderReadMode.Request);
                    if (entity == null) return base.OnReceive(e, stream);

                    if (!entity.IsFinish) Request = entity;
                }
                else if (!entity.IsFinish)
                {
                    entity.ReadHeaders(new BinaryReaderX(stream));
                    if (entity.IsFinish) Request = null;
                }
                else
                    return base.OnReceive(e, stream);

                // 请求头不完整，不发送，等下一部分到来
                if (!entity.IsFinish) return null;

                if (!OnRequest(entity, e, stream)) return null;

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
            /// 
            /// </summary>
            /// <param name="entity"></param>
            /// <param name="e"></param>
            /// <param name="stream"></param>
            /// <returns></returns>
            protected virtual Boolean OnRequest(HttpHeader entity, NetEventArgs e, Stream stream)
            {
                WriteLog("{3}请求：{0} {1} [{2}]", entity.Method, entity.Url, entity.ContentLength, ID);

                var host = "";
                if (entity.Url.IsAbsoluteUri)
                {
                    // 特殊处理CONNECT
                    if (entity.Method.EqualIgnoreCase("CONNECT"))
                    {
                        host = entity.Url.ToString();
                        RemoteEndPoint = NetHelper.ParseEndPoint(entity.Url.ToString(), 80);

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

                        Session.Send(rs.GetStream(), ClientEndPoint);
                        return false;
                    }
                    else
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
                }
                else if (!String.IsNullOrEmpty(entity.Host))
                {
                    RemoteEndPoint = NetHelper.ParseEndPoint(entity.Host, 80);
                }
                else
                    throw new NetException("无法处理的请求！{0}", entity);

                // 可能不含Host
                if (String.IsNullOrEmpty(entity.Host)) entity.Host = host;

                // 处理KeepAlive
                if (!String.IsNullOrEmpty(entity.ProxyConnection))
                {
                    entity.KeepAlive = entity.ProxyKeepAlive;
                    entity.ProxyConnection = null;
                }

                return true;
            }

            ///// <summary>收到客户端发来的数据。子类可通过重载该方法来修改数据</summary>
            ///// <param name="e"></param>
            ///// <param name="stream">数据</param>
            ///// <returns>修改后的数据</returns>
            //protected override Stream OnReceiveRemote(NetEventArgs e, Stream stream)
            //{
            //    return base.OnReceiveRemote(e, stream);
            //}
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
}