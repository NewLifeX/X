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
        /// <summary>监听器</summary>
        public HttpListener Listener { get; set; }

        private List<String> _prefixes = new List<String>();
        #endregion

        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            Stop();
        }

        /// <summary>初始化</summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public Boolean Init(String config)
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

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;
        #endregion
    }
}