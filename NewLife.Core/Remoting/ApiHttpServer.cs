using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NewLife.Log;

namespace NewLife.Remoting
{
    class ApiHttpServer : DisposeBase, IApiServer
    {
        #region 属性
        /// <summary>Api服务器主机</summary>
        public IServiceProvider Provider { get; set; }

        /// <summary>编码器</summary>
        public IEncoder Encoder { get; set; }

        /// <summary>处理器</summary>
        public IApiHandler Handler { get; set; }

        /// <summary>当前服务器所有会话</summary>
        public IApiSession[] AllSessions { get { return null; } }

        /// <summary>监听器</summary>
        public HttpListener Listener { get; set; }

        private readonly List<string> _prefixes = new List<string>();
        #endregion

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            Stop(GetType().Name + (disposing ? "Dispose" : "GC"));
        }

        /// <summary>初始化</summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public bool Init(string config)
        {
            _prefixes.AddRange(config.Split(";"));

            return true;
        }

        public void Start()
        {
            if (Listener != null) return;

            Log.Info("启动{0}，监听 {1}", this.GetType().Name, _prefixes.Join(";"));

            var svr = new HttpListener();
            foreach (var item in _prefixes)
            {
                svr.Prefixes.Add(item.EnsureEnd("/"));
            }
            svr.Start();

            Listener = svr;
        }

        /// <summary>关闭</summary>
        /// <param name="reason">关闭原因。便于日志分析</param>
        public void Stop(String reason)
        {
            if (Listener == null) return;

            Log.Info("停止{0} {1}", this.GetType().Name, reason);

            Listener.Stop();
            Listener = null;
        }

        /// <summary>获取服务提供者</summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public Object GetService(Type serviceType)
        {
            if (serviceType == typeof(ApiServer)) return Provider;

            return Provider?.GetService(serviceType);
        }

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;
        #endregion
    }
}