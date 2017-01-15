using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Model;
using NewLife.Net;
using NewLife.Reflection;

namespace NewLife.Remoting
{
    /// <summary>应用接口服务器</summary>
    public class ApiServer : DisposeBase, IApiHost, IServiceProvider, IServer
    {
        #region 静态
        /// <summary>协议到提供者类的映射</summary>
        public static IDictionary<string, Type> Providers { get; } = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

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
        public bool Active { get; private set; }

        /// <summary>服务器集合</summary>
        public IList<IApiServer> Servers { get; } = new List<IApiServer>();

        /// <summary>编码器</summary>
        public IEncoder Encoder { get; set; }

        /// <summary>处理器</summary>
        public IApiHandler Handler { get; set; }

        /// <summary>过滤器</summary>
        public IList<IFilter> Filters { get; } = new List<IFilter>();

        /// <summary>用户会话数据</summary>
        public IDictionary<String, Object> Items { get; set; } = new Dictionary<String, Object>();

        /// <summary>获取/设置 用户会话数据</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual Object this[String key] { get { return Items.ContainsKey(key) ? Items[key] : null; } set { Items[key] = value; } }
        #endregion

        #region 构造
        /// <summary>实例化一个应用接口服务器</summary>
        public ApiServer() { }

        /// <summary>使用指定端口实例化网络服务应用接口提供者</summary>
        /// <param name="port"></param>
        public ApiServer(int port)
        {
            Add(new NetUri(NetType.Unknown, "", port));
        }

        /// <summary>实例化</summary>
        /// <param name="uri"></param>
        public ApiServer(NetUri uri)
        {
            Add(uri);
        }

        /// <summary>销毁时停止服务</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            Stop(GetType().Name + (disposing ? "Dispose" : "GC"));
        }
        #endregion

        #region 方法
        /// <summary>添加服务器</summary>
        /// <param name="uri"></param>
        public IApiServer Add(NetUri uri)
        {
            Type type;
            if (!Providers.TryGetValue(uri.Protocol, out type)) return null;

            var svr = type.CreateInstance() as IApiServer;
            if (svr != null && !svr.Init(uri.ToString())) return null;

            Servers.Add(svr);

            return svr;
        }

        /// <summary>添加服务器</summary>
        /// <param name="config"></param>
        public IApiServer Add(string config)
        {
            var protocol = config.Substring(null, "://");
            Type type;
            if (!Providers.TryGetValue(protocol, out type)) return null;

            var svr = type.CreateInstance() as IApiServer;
            if (svr != null && !svr.Init(config)) return null;

            Servers.Add(svr);

            return svr;
        }

        /// <summary>开始服务</summary>
        public void Start()
        {
            if (Active) return;

#if DEBUG
            Encoder.Log = Log;
#endif
            Log.Info("启动{0}，共有服务器{1}个", this.GetType().Name, Servers.Count);
            if (Handler == null) Handler = new ApiHandler { Host = this };
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
                Log.Info("\t{0}\t{1}", item.Key, item.Value);
            }

            Active = true;
        }

        /// <summary>停止服务</summary>
        /// <param name="reason">关闭原因。便于日志分析</param>
        public void Stop(String reason)
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

        #region 控制器管理
        /// <summary>接口动作管理器</summary>
        public IApiManager Manager { get; } = new ApiManager();

        /// <summary>注册服务提供类。该类的所有公开方法将直接暴露</summary>
        /// <typeparam name="TService"></typeparam>
        public void Register<TService>() where TService : class, new()
        {
            Manager.Register<TService>();
        }
        #endregion

        #region 服务提供者
        /// <summary>获取服务提供者</summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public Object GetService(Type serviceType)
        {
            if (serviceType == GetType()) return this;
            if (serviceType == typeof(IApiHost)) return this;
            if (serviceType == typeof(IApiManager)) return Manager;
            if (serviceType == typeof(IEncoder) && Encoder != null) return Encoder;
            if (serviceType == typeof(IApiHandler) && Handler != null) return Handler;

            return null;
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;
        #endregion
    }
}