using System;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Net;
using NewLife.Net.Handlers;

namespace NewLife.Remoting
{
    /// <summary>应用接口客户端</summary>
    public class ApiClient : ApiHost, IApiSession
    {
        #region 属性
        /// <summary>是否已打开</summary>
        public Boolean Active { get; set; }

        /// <summary>通信客户端</summary>
        public ISocketClient Client { get; set; }

        /// <summary>主机</summary>
        IApiHost IApiSession.Host => this;

        /// <summary>最后活跃时间</summary>
        public DateTime LastActive { get; set; }

        /// <summary>所有服务器所有会话，包含自己</summary>
        IApiSession[] IApiSession.AllSessions => new IApiSession[] { this };

        /// <summary>调用超时时间。默认30_000ms</summary>
        public Int32 Timeout { get; set; } = 30_000;
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
            //if (Encoder == null) Encoder = new BinaryEncoder();
            if (Handler == null) Handler = new ApiHandler { Host = this };

            Encoder.Log = EncoderLog;

            //ct.Log = Log;

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

            WriteLog("SetRemote {0}", nu);

            var ct = Client = nu.CreateRemote();
            ct.Log = Log;
            ct.Add(new StandardCodec { Timeout = Timeout, UserPacket = false });

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
        /// <summary>异步调用，等待返回结果</summary>
        /// <param name="resultType">返回类型</param>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <param name="flag">标识</param>
        /// <returns></returns>
        public virtual async Task<Object> InvokeAsync(Type resultType, String action, Object args = null, Byte flag = 0)
        {
            var ss = Client;
            if (ss == null) return null;

            var act = action;

            try
            {
                return await ApiHostHelper.InvokeAsync(this, this, resultType, act, args, flag);
            }
            catch (ApiException ex)
            {
                // 重新登录后再次调用
                if (ex.Code == 401)
                {
                    return await ApiHostHelper.InvokeAsync(this, this, resultType, act, args, flag);
                }

                throw;
            }
            // 截断任务取消异常，避免过长
            catch (TaskCanceledException ex)
            {
                throw new TaskCanceledException(action + "超时取消", ex);
            }
        }

        /// <summary>异步调用，等待返回结果</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <param name="flag">标识</param>
        /// <returns></returns>
        public virtual async Task<TResult> InvokeAsync<TResult>(String action, Object args = null, Byte flag = 0)
        {
            var rs = await InvokeAsync(typeof(TResult), action, args, flag);
            return (TResult)rs;
        }

        /// <summary>同步调用，不等待返回</summary>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <param name="flag">标识</param>
        /// <returns></returns>
        public virtual Boolean Invoke(String action, Object args = null, Byte flag = 0)
        {
            var ss = Client;
            if (ss == null) return false;

            var act = action;

            return ApiHostHelper.Invoke(this, this, act, args, flag);
        }

        /// <summary>创建消息</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        IMessage IApiSession.CreateMessage(Packet pk) => new DefaultMessage { Payload = pk };

        Task<IMessage> IApiSession.SendAsync(IMessage msg) => Client.SendMessageAsync(msg).ContinueWith(t => t.Result as IMessage);
        Boolean IApiSession.Send(IMessage msg) => Client.SendMessage(msg);
        #endregion
    }
}