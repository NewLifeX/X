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
        public IServiceProvider Host { get; set; }

        /// <summary>编码器</summary>
        public IEncoder Encoder { get; set; }

        /// <summary>处理器</summary>
        public IApiHandler Handler { get; set; }

        /// <summary>监听器</summary>
        public HttpListener Listener { get; set; }

        private readonly List<string> _prefixes = new List<string>();
        #endregion

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            Stop();
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

        public void Stop()
        {
            if (Listener == null) return;

            Log.Info("停止{0}", this.GetType().Name);

            Listener.Stop();
            Listener = null;
        }

        /// <summary>获取服务提供者</summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public Object GetService(Type serviceType)
        {
            if (serviceType == typeof(ApiServer)) return Host;

            return Host.GetService(serviceType);
        }

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;
        #endregion
    }
}