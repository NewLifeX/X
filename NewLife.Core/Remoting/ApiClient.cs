using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewLife.Log;
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

            Close();
        }
        #endregion

        #region 打开关闭
        /// <summary>打开客户端</summary>
        public void Open()
        {
            if (Encoder == null) throw new ArgumentNullException(nameof(Encoder), "未指定编码器");
            //if (Handler == null) throw new ArgumentNullException(nameof(Handler), "未指定处理器");

            if (Handler == null) Handler = new ApiHandler();

            Client.Log = Log;
            Client.Open();

            Log.Info("客户端可用接口{0}个：", Manager.Services.Count);
            foreach (var item in Manager.Services)
            {
                Log.Info("\t{0}\t{1}", item.Key, item.Value);
            }
        }

        /// <summary>关闭客户端</summary>
        public void Close()
        {
            Client.Close();
        }
        #endregion

        #region 远程调用
        /// <summary>登录</summary>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        public void Login(String user, String pass)
        {

        }

        /// <summary>调用</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<TResult> InvokeAsync<TResult>(String action, object args = null)
        {
            var data = Encoder.Encode(action, args);

            var rs = await Client.SendAsync(data);

            var dic = Encoder.Decode(rs);

            return Encoder.Decode<TResult>(dic);
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
