using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Net;
using NewLife.Reflection;

namespace NewLife.Remoting
{
    /// <summary>应用接口客户端</summary>
    [Api(null)]
    public class ApiClient : ApiHost, IApiSession
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
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>是否已打开</summary>
        public Boolean Active { get; set; }

        /// <summary>通信客户端</summary>
        public IApiClient Client { get; set; }

        /// <summary>所有服务器所有会话，包含自己</summary>
        IApiSession[] IApiSession.AllSessions { get { return new IApiSession[] { this }; } }
        #endregion

        #region 构造
        /// <summary>实例化应用接口客户端</summary>
        public ApiClient()
        {
            var type = GetType();
            Name = type.GetDisplayName() ?? type.Name.TrimEnd("Client");

            Register(new ApiController { Host = this }, null);
        }

        /// <summary>实例化应用接口客户端</summary>
        public ApiClient(String uri) : this()
        {
            SetRemote(uri);
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            Close(Name + (disposing ? "Dispose" : "GC"));
        }
        #endregion

        #region 打开关闭
        /// <summary>打开客户端</summary>
        public virtual Boolean Open()
        {
            if (Active) return true;

            if (Client == null) throw new ArgumentNullException(nameof(Client), "未指定通信客户端");
            //if (Encoder == null) throw new ArgumentNullException(nameof(Encoder), "未指定编码器");

            if (Encoder == null) Encoder = new JsonEncoder();
            if (Handler == null) Handler = new ApiHandler { Host = this };

#if DEBUG
            Client.Log = Log;
            Encoder.Log = Log;
#endif

            Client.Opened += Client_Opened;
            if (!Client.Open()) return false;

            var ms = Manager.Services;
            if (ms.Count > 0)
            {
                Log.Info("客户端可用接口{0}个：", ms.Count);
                foreach (var item in ms)
                {
                    Log.Info("\t{0,-16}{1}", item.Key, item.Value);
                }
            }

            return Active = true;
        }

        /// <summary>关闭</summary>
        /// <param name="reason">关闭原因。便于日志分析</param>
        /// <returns>是否成功</returns>
        public virtual void Close(String reason)
        {
            if (!Active) return;

            var tc = Client;
            if (tc != null)
            {
                tc.Opened -= Client_Opened;
                tc.Close(reason ?? (GetType().Name + "Close"));
            }

            Active = false;
        }

        /// <summary>打开后触发。</summary>
        public event EventHandler Opened;

        private void Client_Opened(Object sender, EventArgs e)
        {
            Opened?.Invoke(this, e);
        }

        /// <summary>设置远程地址</summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public Boolean SetRemote(String uri)
        {
            Type type;
            var nu = new NetUri(uri);
            if (!Providers.TryGetValue(nu.Protocol, out type)) return false;

            var ac = type.CreateInstance() as IApiClient;
            if (ac != null && ac.Init(uri))
            {
                ac.Provider = this;
                Client = ac;
            }

            return true;
        }
        #endregion

        #region 远程调用
        /// <summary>调用</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<TResult> InvokeAsync<TResult>(String action, Object args = null)
        {
            var ss = Client;
            if (ss == null) return default(TResult);

            return await ApiHostHelper.InvokeAsync<TResult>(this, this, action, args).ConfigureAwait(false);
        }

        /// <summary>创建消息</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        IMessage IApiSession.CreateMessage(Packet pk) { return Client?.CreateMessage(pk); }

        async Task<IMessage> IApiSession.SendAsync(IMessage msg) { return await Client.SendAsync(msg).ConfigureAwait(false); }
        #endregion

        #region 服务提供者
        /// <summary>获取服务提供者</summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public override Object GetService(Type serviceType)
        {
            if (serviceType == GetType()) return this;
            if (serviceType == typeof(IApiClient)) return Client;

            return base.GetService(serviceType);
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            Log?.Info(Name + " " + format, args);
        }

        /// <summary>已重载。返回具有本类特征的字符串</summary>
        /// <returns>String</returns>
        public override string ToString() { return Name; }
        #endregion
    }
}