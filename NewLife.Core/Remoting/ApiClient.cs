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
        public static IDictionary<string, Type> Providers { get; } = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

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
            if (ac != null && ac.Init(uri)) Client = ac;
        }

        /// <summary>实例化应用接口客户端</summary>
        /// <param name="uri"></param>
        public ApiClient(Uri uri)
        {
            Type type;
            if (!Providers.TryGetValue("http", out type)) return;
            var ac = type.CreateInstance() as IApiClient;
            if (ac != null && ac.Init(uri)) Client = ac;
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            Close();
        }
        #endregion

        #region 方法
        /// <summary>打开客户端</summary>
        public void Open()
        {
            if (Encoder == null) throw new ArgumentNullException(nameof(Encoder), "未指定编码器");

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
        public void Login(string user, string pass)
        {

        }

        /// <summary>调用</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<TResult> InvokeAsync<TResult>(string action, object args = null)
        {
            var data = Encoder.Encode(action, args);

            var rs = await Client.SendAsync(data);

            var dic = Encoder.Decode(rs);

            return Encoder.Decode<TResult>(dic);
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;
        #endregion
    }
}
