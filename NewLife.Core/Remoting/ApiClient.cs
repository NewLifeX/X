using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;

namespace NewLife.Remoting
{
    /// <summary>应用接口客户端</summary>
    public class ApiClient : DisposeBase
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
        }
        #endregion

        #region 属性
        /// <summary>客户端</summary>
        public IApiClient Client { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化应用接口客户端</summary>
        public ApiClient() { }

        /// <summary>智力和应用接口客户端</summary>
        /// <param name="uri"></param>
        public ApiClient(NetUri uri)
        {
            Type type = null;
            if (Providers.TryGetValue(uri.Protocol, out type)) Client = type.CreateInstance(uri) as IApiClient;
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            Close();
        }
        #endregion

        #region 方法
        /// <summary>打开客户端</summary>
        public void Open()
        {
            Client.Log = Log;
            Client.Open();
        }

        /// <summary>关闭客户端</summary>
        public void Close()
        {
            Client.Close();
        }

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
        public async Task<TResult> Invoke<TResult>(String action, Object args = null)
        {
            return default(TResult);
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;
        #endregion
    }
}
