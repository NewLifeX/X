using System;
using System.Collections.Generic;
using System.Reflection;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;

namespace NewLife.Remoting
{
    /// <summary>应用接口服务器</summary>
    public class ApiServer : DisposeBase, IServiceProvider
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

            Stop();
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

            Log.Info("启动{0}，共有服务器{1}个", this.GetType().Name, Servers.Count);
            var handler = new ApiHandler { Server = this };
            foreach (var item in Servers)
            {
                if (item.Handler == null) item.Handler = handler;
                if (item.Encoder == null) item.Encoder = Encoder;
                item.Host = this;
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

        #region 控制器管理
        /// <summary>可提供服务的方法</summary>
        public IDictionary<string, ApiAction> Services { get; } = new Dictionary<string, ApiAction>();

        /// <summary>注册服务提供类。该类的所有公开方法将直接暴露</summary>
        /// <typeparam name="TService"></typeparam>
        public void Register<TService>() where TService : class, new()
        {
            var type = typeof(TService);
            //var name = type.Name.TrimEnd("Controller");

            foreach (var mi in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (mi.IsSpecialName) continue;
                if (mi.DeclaringType == typeof(object)) continue;

                var act = new ApiAction(mi);

                Services[act.Name] = act;
            }
        }

        /// <summary>查找服务</summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public ApiAction FindAction(string action)
        {
            ApiAction mi;
            return Services.TryGetValue(action, out mi) ? mi : null;
        }
        #endregion

        #region 服务提供者
        /// <summary>获取服务提供者</summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public Object GetService(Type serviceType)
        {
            if (serviceType == typeof(ApiServer)) return this;

            return null;
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;
        #endregion
    }
}