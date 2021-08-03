using System;
using System.Collections.Generic;
using System.ComponentModel;
using NewLife.Net;

namespace NewLife.Http
{
    /// <summary>Http服务器</summary>
    [DisplayName("Http服务器")]
    public class HttpServer : NetServer
    {
        #region 属性
        /// <summary>Http响应头Server名称</summary>
        public String ServerName { get; set; }

        /// <summary>路由映射</summary>
        public IDictionary<String, Delegate> Routes { get; set; } = new Dictionary<String, Delegate>(StringComparer.OrdinalIgnoreCase);
        #endregion

        /// <summary>实例化</summary>
        public HttpServer()
        {
            Name = "Http";
            Port = 80;
            ProtocolType = NetType.Http;

            var ver = GetType().Assembly.GetName().Version;
            ServerName = $"NewLife-HttpServer/{ver.Major}.{ver.Minor}";
        }

        /// <summary>创建会话</summary>
        /// <param name="session"></param>
        /// <returns></returns>
        protected override INetSession CreateSession(ISocketSession session) => new HttpSession();

        #region 方法
        /// <summary>映射路由处理器</summary>
        /// <param name="path"></param>
        /// <param name="handler"></param>
        public void Map(String path, HttpProcessDelegate handler) => Routes[path] = handler;

        /// <summary>映射路由处理器</summary>
        /// <param name="path"></param>
        /// <param name="handler"></param>
        public void Map(String path, Func<Object> handler) => Routes[path] = handler;
        #endregion
    }
}