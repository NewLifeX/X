using System;
using System.Collections.Generic;
using System.Reflection;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;

namespace NewLife.Remoting
{
    /// <summary>应用接口服务器</summary>
    public class ApiServer : DisposeBase
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

        /// <summary>服务器集合</summary>
        public IList<IApiServer> Servers { get; } = new List<IApiServer>();

        /// <summary>编码器</summary>
        public IEncoder Encoder { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化一个应用接口服务器</summary>
        public ApiServer() { }

        /// <summary>使用指定端口实例化网络服务应用接口提供者</summary>
        /// <param name="port"></param>
        public ApiServer(Int32 port)
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
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            Stop();
        }
        #endregion

        #region 方法
        /// <summary>添加服务器</summary>
        /// <param name="uri"></param>
        public IApiServer Add(NetUri uri)
        {
            Type type = null;
            if (Providers.TryGetValue(uri.Protocol, out type))
            {
                var svr = type.CreateInstance() as IApiServer;
                if (svr.Init(uri.ToString()))
                {
                    Servers.Add(svr);
                    return svr;
                }
            }
            return null;
        }

        /// <summary>添加服务器</summary>
        /// <param name="config"></param>
        public IApiServer Add(String config)
        {
            var protocol = config.Substring(null, "://");
            Type type = null;
            if (Providers.TryGetValue(protocol, out type))
            {
                var svr = type.CreateInstance() as IApiServer;
                if (svr.Init(config))
                {
                    Servers.Add(svr);
                    return svr;
                }
            }
            return null;
        }

        /// <summary>开始服务</summary>
        public void Start()
        {
            if (Active) return;

            Log.Info("启动{0}，共有服务器{1}个", this.GetType().Name, Servers.Count);
            var handler = new ApiHandler { Server = this };
            foreach (var item in Servers)
            {
                if (item.Handler == null) item.Handler = handler;
                if (item.Encoder == null) item.Encoder = Encoder;
                item.Log = Log;
                item.Start();
            }

            Log.Info("可用接口{0}个：", Services.Count);
            foreach (var item in Services)
            {
                Log.Info("\t{0}\t{1}", item.Key, item.Value);
            }

            Active = true;
        }

        /// <summary>停止服务</summary>
        public void Stop()
        {
            if (!Active) return;

            Log.Info("停止{0}", this.GetType().Name);
            foreach (var item in Servers)
            {
                item.Stop();
            }

            Active = false;
        }
        #endregion

        #region 服务提供者管理
        /// <summary>可提供服务的方法</summary>
        public IDictionary<String, ApiAction> Services { get; } = new Dictionary<String, ApiAction>();

        /// <summary>注册服务提供类。该类的所有公开方法将直接暴露</summary>
        /// <typeparam name="TService"></typeparam>
        public void Register<TService>() where TService : class, new()
        {
            var type = typeof(TService);
            //var name = type.Name.TrimEnd("Controller");

            foreach (var mi in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (mi.IsSpecialName) continue;
                if (mi.DeclaringType == typeof(Object)) continue;

                var act = new ApiAction(mi);

                Services[act.Name] = act;
            }
        }

        /// <summary>查找服务</summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public ApiAction FindAction(String action)
        {
            ApiAction mi = null;
            if (Services.TryGetValue(action, out mi)) return mi;

            return null;
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;
        #endregion
    }
}