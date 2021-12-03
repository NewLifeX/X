using System;
using System.Collections.Generic;
using NewLife.Collections;
using NewLife.Net;

namespace NewLife.Http
{
    /// <summary>Http上下文</summary>
    public interface IHttpContext
    {
        #region 属性
        /// <summary>请求</summary>
        HttpRequest Request { get; }

        /// <summary>响应</summary>
        HttpResponse Response { get; }

        /// <summary>连接会话</summary>
        INetSession Connection { get; }

        /// <summary>WebSocket连接</summary>
        WebSocket WebSocket { get; }

        /// <summary>执行路径</summary>
        String Path { get; }

        /// <summary>处理器</summary>
        IHttpHandler Handler { get; }

        /// <summary>请求参数</summary>
        IDictionary<String, Object> Parameters { get; }
        #endregion
    }

    /// <summary>默认Http上下文</summary>
    public class DefaultHttpContext : IHttpContext
    {
        #region 属性
        /// <summary>请求</summary>
        public HttpRequest Request { get; set; }

        /// <summary>响应</summary>
        public HttpResponse Response { get; set; }

        /// <summary>连接会话</summary>
        public INetSession Connection { get; set; }

        /// <summary>WebSocket连接</summary>
        public WebSocket WebSocket { get; set; }

        /// <summary>执行路径</summary>
        public String Path { get; set; }

        /// <summary>处理器</summary>
        public IHttpHandler Handler { get; set; }

        /// <summary>请求参数</summary>
        public IDictionary<String, Object> Parameters { get; } = new NullableDictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
        #endregion

        #region 静态
        [ThreadStatic]
        private static IHttpContext _current;

        /// <summary>当前上下文</summary>
        public static IHttpContext Current { get => _current; set => _current = value; }
        #endregion
    }
}