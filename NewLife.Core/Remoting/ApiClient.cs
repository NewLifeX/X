using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Net;
using NewLife.Reflection;

namespace NewLife.Remoting
{
    /// <summary>应用接口客户端</summary>
    public class ApiClient : DisposeBase, IApiHost, IServiceProvider
    {
        #region 静态
        /// <summary>协议到提供者类的映射</summary>
        public static IDictionary<String, Type> Providers { get; } = new Dictionary<String, Type>(StringComparer.OrdinalIgnoreCase);

        static ApiClient()
        {
            var ps = Providers;
            ps.Add("tcp", typeof(ApiNetClient));
            ps.Add("udp", typeof(ApiNetClient));
            ps.Add("http", typeof(ApiHttpClient));
            ps.Add("ws", typeof(ApiHttpClient));
        }
        #endregion

        #region 属性
        /// <summary>客户端</summary>
        public IApiClient Client { get; set; }

        /// <summary>编码器。用于对象与字节数组相互转换</summary>
        public IEncoder Encoder { get; set; }

        /// <summary>处理器</summary>
        public IApiHandler Handler { get; set; }

        /// <summary>过滤器</summary>
        public IList<IFilter> Filters { get; } = new List<IFilter>();
        #endregion

        #region 构造
        ///// <summary>实例化应用接口客户端</summary>
        //public ApiClient() { }

        /// <summary>实例化应用接口客户端</summary>
        /// <param name="uri"></param>
        public ApiClient(NetUri uri)
        {
            Type type;
            if (!Providers.TryGetValue(uri.Protocol, out type)) return;

            var ac = type.CreateInstance() as IApiClient;
            if (ac != null && ac.Init(uri))
            {
                ac.Provider = this;
                Client = ac;
            }
        }

        /// <summary>实例化应用接口客户端</summary>
        /// <param name="uri"></param>
        public ApiClient(Uri uri)
        {
            Type type;
            if (!Providers.TryGetValue("http", out type)) return;

            var ac = type.CreateInstance() as IApiClient;
            if (ac != null && ac.Init(uri))
            {
                ac.Provider = this;
                Client = ac;
            }
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            Close(GetType().Name + (disposing ? "Dispose" : "GC"));
        }
        #endregion

        #region 打开关闭
        /// <summary>打开客户端</summary>
        public void Open()
        {
            if (Encoder == null) throw new ArgumentNullException(nameof(Encoder), "未指定编码器");
            //if (Handler == null) throw new ArgumentNullException(nameof(Handler), "未指定处理器");

            if (Handler == null) Handler = new ApiHandler { Host = this };

            Client.Opened += Client_Opened;

#if DEBUG
            Client.Log = Log;
            Encoder.Log = Log;
#endif
            Client.Open();

            var ms = Manager.Services;
            if (ms.Count > 0)
            {
                Log.Info("客户端可用接口{0}个：", ms.Count);
                foreach (var item in ms)
                {
                    Log.Info("\t{0}\t{1}", item.Key, item.Value);
                }
            }
        }

        /// <summary>关闭</summary>
        /// <param name="reason">关闭原因。便于日志分析</param>
        /// <returns>是否成功</returns>
        public void Close(String reason)
        {
            Client.Opened -= Client_Opened;
            Client.Close(reason ?? (GetType().Name + "Close"));
        }

        /// <summary>打开后触发。</summary>
        public event EventHandler Opened;

        private void Client_Opened(Object sender, EventArgs e)
        {
            Opened?.Invoke(this, e);
        }
        #endregion

        #region 远程调用
        /// <summary>调用</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<TResult> InvokeAsync<TResult>(String action, object args = null)
        {
            var ss = Client as IApiSession;
            if (ss == null) return default(TResult);

            return await ss.InvokeAsync<TResult>(action, args);
        }
        #endregion

        #region 过滤器
        /// <summary>执行过滤器</summary>
        /// <param name="msg"></param>
        /// <param name="issend"></param>
        void IApiHost.ExecuteFilter(IMessage msg, Boolean issend)
        {
            var fs = Filters;
            if (fs.Count == 0) return;

            // 接收时需要倒序
            if (!issend) fs = fs.Reverse().ToList();

            var ctx = new ApiFilterContext { Packet = msg.Payload, Message = msg, IsSend = issend };
            foreach (var item in fs)
            {
                item.Execute(ctx);
                //Log.Debug("{0}:{1}", item.GetType().Name, ctx.Packet.ToHex());
            }
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
