using System;
using NewLife.Log;
using NewLife.Model;
using NewLife.Net;
using NewLife.Threading;

namespace NewLife.Remoting
{
    /// <summary>应用接口服务器</summary>
    public class ApiServer : ApiHost, IServer
    {
        #region 属性
        /// <summary>是否正在工作</summary>
        public Boolean Active { get; private set; }

        /// <summary>端口</summary>
        public Int32 Port { get; set; }

        /// <summary>服务器</summary>
        public IApiServer Server { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化一个应用接口服务器</summary>
        public ApiServer()
        {
            var type = GetType();
            Name = type.GetDisplayName() ?? type.Name.TrimEnd("Server");

            Register(new ApiController { Host = this }, null);
        }

        /// <summary>使用指定端口实例化网络服务应用接口提供者</summary>
        /// <param name="port"></param>
        public ApiServer(Int32 port) : this() => Port = port;

        /// <summary>实例化</summary>
        /// <param name="uri"></param>
        public ApiServer(NetUri uri) : this() => Use(uri);

        /// <summary>销毁时停止服务</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            _Timer.TryDispose();

            Stop(GetType().Name + (disposing ? "Dispose" : "GC"));
        }
        #endregion

        #region 启动停止
        /// <summary>添加服务器</summary>
        /// <param name="uri"></param>
        public IApiServer Use(NetUri uri)
        {
            var svr = new ApiNetServer();
            if (!svr.Init(uri.ToString())) return null;

            Server = svr;

            return svr;
        }

        /// <summary>确保已创建服务器对象</summary>
        /// <returns></returns>
        public IApiServer EnsureCreate()
        {
            var svr = Server;
            if (svr != null) return svr;

            if (Port <= 0) throw new ArgumentNullException(nameof(Server), "未指定服务器Server，且未指定端口Port！");

            svr = new ApiNetServer();
            svr.Host = this;
            svr.Init(new NetUri(NetType.Unknown, "*", Port) + "");

            return Server = svr;
        }

        /// <summary>开始服务</summary>
        public virtual void Start()
        {
            if (Active) return;

            if (Encoder == null) Encoder = new JsonEncoder();
            //if (Encoder == null) Encoder = new BinaryEncoder();
            if (Handler == null) Handler = new ApiHandler { Host = this };
            //if (StatInvoke == null) StatInvoke = new PerfCounter();
            //if (StatProcess == null) StatProcess = new PerfCounter();

            Encoder.Log = EncoderLog;

            Log.Info("启动{0}，服务器 {1}", GetType().Name, Server);
            Log.Info("编码：{0}", Encoder);
            //Log.Info("处理：{0}", Handler);

            var svr = EnsureCreate();

            //if (svr.Handler == null) svr.Handler = Handler;
            //if (svr.Encoder == null) svr.Encoder = Encoder;
            svr.Host = this;
            svr.Log = Log;
            svr.Start();

            ShowService();

            var ms = StatPeriod * 1000;
            if (ms > 0)
            {
                if (StatInvoke == null) StatInvoke = new PerfCounter();
                if (StatProcess == null) StatProcess = new PerfCounter();

                _Timer = new TimerX(DoWork, null, ms, ms) { Async = true };
            }

            Active = true;
        }

        /// <summary>停止服务</summary>
        /// <param name="reason">关闭原因。便于日志分析</param>
        public virtual void Stop(String reason)
        {
            if (!Active) return;

            Log.Info("停止{0} {1}", GetType().Name, reason);
            Server.Stop(reason ?? (GetType().Name + "Stop"));

            Active = false;
        }
        #endregion

        #region 统计
        private TimerX _Timer;
        private String _Last;

        /// <summary>显示统计信息的周期。默认600秒，0表示不显示统计信息</summary>
        public Int32 StatPeriod { get; set; } = 600;

        private void DoWork(Object state)
        {
            var msg = this.GetStat();
            if (msg.IsNullOrEmpty() || msg == _Last) return;
            _Last = msg;

            WriteLog(msg);
        }
        #endregion
    }
}