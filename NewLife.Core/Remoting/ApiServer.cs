using System;
using System.Collections.Generic;
using NewLife.Data;
using NewLife.Model;
using NewLife.Net;
using NewLife.Reflection;

namespace NewLife.Remoting
{
    /// <summary>应用接口服务器</summary>
    public class ApiServer : ApiHost, IServer
    {
        #region 静态
        /// <summary>协议到提供者类的映射</summary>
        public static IDictionary<String, Type> Providers { get; } = new Dictionary<String, Type>(StringComparer.OrdinalIgnoreCase);

        static ApiServer()
        {
            var ps = Providers;
            ps.Add(NetType.Tcp + "", typeof(ApiNetServer));
            ps.Add(NetType.Udp + "", typeof(ApiNetServer));
            ps.Add(NetType.Unknown + "", typeof(ApiNetServer));
            ps.Add(NetType.Http + "", typeof(ApiHttpServer));
            ps.Add("ws", typeof(ApiHttpServer));
        }
        #endregion

        #region 属性
        /// <summary>是否正在工作</summary>
        public Boolean Active { get; private set; }

        /// <summary>是否默认匿名访问</summary>
        public Boolean Anonymous { get; set; }

        /// <summary>服务器集合</summary>
        public IList<IApiServer> Servers { get; } = new List<IApiServer>();
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
        public ApiServer(Int32 port) : this()
        {
            Add(port);
        }

        /// <summary>实例化</summary>
        /// <param name="uri"></param>
        public ApiServer(NetUri uri) : this()
        {
            Add(uri);
        }

        /// <summary>销毁时停止服务</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            Stop(GetType().Name + (disposing ? "Dispose" : "GC"));
        }
        #endregion

        #region 启动停止
        /// <summary>添加服务器</summary>
        /// <param name="port"></param>
        public IApiServer Add(Int32 port)
        {
            return Add(new NetUri(NetType.Unknown, "", port));
        }

        /// <summary>添加服务器</summary>
        /// <param name="uri"></param>
        public IApiServer Add(NetUri uri)
        {
            Type type;
            if (!Providers.TryGetValue(uri.Protocol, out type)) return null;

            var svr = type.CreateInstance() as IApiServer;
            if (svr != null)
            {
                svr.Provider = this;
                svr.Log = Log;

                if (!svr.Init(uri.ToString())) return null;
            }

            Servers.Add(svr);

            return svr;
        }

        /// <summary>添加服务器</summary>
        /// <param name="config"></param>
        public IApiServer Add(String config)
        {
            var protocol = config.Substring(null, "://");
            Type type;
            if (!Providers.TryGetValue(protocol, out type)) return null;

            var svr = type.CreateInstance() as IApiServer;
            if (svr != null)
            {
                svr.Provider = this;
                svr.Log = Log;

                if (!svr.Init(config)) return null;
            }

            Servers.Add(svr);

            return svr;
        }

        /// <summary>开始服务</summary>
        public virtual void Start()
        {
            if (Active) return;

            if (Encoder == null) Encoder = new JsonEncoder();
            if (Handler == null) Handler = new ApiHandler { Host = this };

            Encoder.Log = EncoderLog;

            // 设置过滤器
            SetFilter();

            Log.Info("启动{0}，共有服务器{1}个", GetType().Name, Servers.Count);
            Log.Info("编码：{0}", Encoder);
            Log.Info("处理：{0}", Handler);

            foreach (var item in Servers)
            {
                if (item.Handler == null) item.Handler = Handler;
                if (item.Encoder == null) item.Encoder = Encoder;
                item.Provider = this;
                item.Log = Log;
                item.Start();
            }

            Log.Info("服务端可用接口{0}个：", Manager.Services.Count);
            foreach (var item in Manager.Services)
            {
                Log.Info("\t{0,-16}\t{1}", item.Key, item.Value);
            }

            Active = true;
        }

        /// <summary>停止服务</summary>
        /// <param name="reason">关闭原因。便于日志分析</param>
        public virtual void Stop(String reason)
        {
            if (!Active) return;

            Log.Info("停止{0} {1}", this.GetType().Name, reason);
            foreach (var item in Servers)
            {
                item.Stop(reason ?? (GetType().Name + "Stop"));
            }

            Active = false;
        }
        #endregion

        #region 加密&压缩
        /// <summary>获取通信密钥的委托</summary>
        /// <returns></returns>
        protected override Func<FilterContext, Byte[]> GetKeyFunc()
        {
            // 从Session里面拿Key
            return ApiSession.GetKey;
        }
        #endregion

        #region 服务提供者
        /// <summary>获取服务提供者</summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public override Object GetService(Type serviceType)
        {
            // 服务类是否当前类的基类
            if (GetType().As(serviceType)) return this;

            if (serviceType == typeof(IServer)) return this;

            return base.GetService(serviceType);
        }
        #endregion
    }
}