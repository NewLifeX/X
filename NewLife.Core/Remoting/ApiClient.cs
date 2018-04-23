using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Net;
using NewLife.Reflection;

namespace NewLife.Remoting
{
    /// <summary>应用接口客户端</summary>
    public class ApiClient : ApiHost, IApiSession/*, IUserSession*/
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
        /// <summary>是否已打开</summary>
        public Boolean Active { get; set; }

        /// <summary>通信客户端</summary>
        public IApiClient Client { get; set; }

        /// <summary>主机</summary>
        IApiHost IApiSession.Host => this;

        /// <summary>最后活跃时间</summary>
        public DateTime LastActive { get; set; }

        /// <summary>所有服务器所有会话，包含自己</summary>
        IApiSession[] IApiSession.AllSessions => new IApiSession[] { this };

        /// <summary>附加参数，每次请求都携带</summary>
        public IDictionary<String, Object> Cookie { get; set; } = new NullableDictionary<String, Object>();
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
        public ApiClient(String uri) : this() => SetRemote(uri);

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

            var ct = Client;
            if (ct == null) throw new ArgumentNullException(nameof(Client), "未指定通信客户端");

            if (Encoder == null) Encoder = new JsonEncoder();
            if (Handler == null) Handler = new ApiHandler { Host = this };

            Encoder.Log = EncoderLog;

            ct.Provider = this;
            ct.Log = Log;

            // 打开网络连接
            if (!ct.Open()) return false;

            ShowService();

            return Active = true;
        }

        /// <summary>关闭</summary>
        /// <param name="reason">关闭原因。便于日志分析</param>
        /// <returns>是否成功</returns>
        public virtual void Close(String reason)
        {
            if (!Active) return;

            var ct = Client;
            if (ct != null) ct.Close(reason ?? (GetType().Name + "Close"));

            Active = false;
        }

        /// <summary>设置远程地址</summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public Boolean SetRemote(String uri)
        {
            var nu = new NetUri(uri);
            if (!Providers.TryGetValue(nu.Type + "", out var type)) return false;

            WriteLog("{0} SetRemote {1}", type.Name, nu);

            if (type.CreateInstance() is IApiClient ac)
            {
                ac.Provider = this;
                ac.Log = Log;

                if (ac.Init(uri))
                {
                    Client.TryDispose();
                    Client = ac;
                }
            }

            return true;
        }

        /// <summary>查找Api动作</summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public virtual ApiAction FindAction(String action) => Manager.Find(action);

        /// <summary>创建控制器实例</summary>
        /// <param name="api"></param>
        /// <returns></returns>
        public virtual Object CreateController(ApiAction api) => this.CreateController(this, api);
        #endregion

        #region 远程调用
        /// <summary>调用</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <param name="cookie">附加参数，位于顶级</param>
        /// <returns></returns>
        public virtual async Task<TResult> InvokeAsync<TResult>(String action, Object args = null, IDictionary<String, Object> cookie = null)
        {
            var ss = Client;
            if (ss == null) return default(TResult);

            var act = action;

            try
            {
                return await ApiHostHelper.InvokeAsync<TResult>(this, this, act, args, cookie ?? Cookie);
            }
            catch (ApiException ex)
            {
                // 重新登录后再次调用
                if (ex.Code == 401)
                {
                    return await ApiHostHelper.InvokeAsync<TResult>(this, this, act, args, cookie ?? Cookie);
                }

                throw;
            }
            // 截断任务取消异常，避免过长
            catch (TaskCanceledException)
            {
                throw new TaskCanceledException(action + "超时取消");
            }
        }

        /// <summary>创建消息</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        IMessage IApiSession.CreateMessage(Packet pk) => Client?.CreateMessage(pk);

        async Task<IMessage> IApiSession.SendAsync(IMessage msg) => await Client.SendAsync(msg);
        #endregion

        #region 服务提供者
        /// <summary>获取服务提供者</summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public override Object GetService(Type serviceType)
        {
            // 服务类是否当前类的基类
            if (GetType().As(serviceType)) return this;

            if (serviceType == typeof(IApiClient)) return Client;

            return base.GetService(serviceType);
        }
        #endregion
    }
}