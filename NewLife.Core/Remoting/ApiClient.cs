using System;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Net;
using NewLife.Threading;

namespace NewLife.Remoting
{
    /// <summary>应用接口客户端</summary>
    public class ApiClient : ApiHost, IApiSession
    {
        #region 属性
        /// <summary>是否已打开</summary>
        public Boolean Active { get; set; }

        /// <summary>服务端地址集合。负载均衡</summary>
        public String[] Servers { get; set; }

        /// <summary>主机</summary>
        IApiHost IApiSession.Host => this;

        /// <summary>最后活跃时间</summary>
        public DateTime LastActive { get; set; }

        /// <summary>所有服务器所有会话，包含自己</summary>
        IApiSession[] IApiSession.AllSessions => new IApiSession[] { this };

        ///// <summary>调用超时时间。默认30_000ms</summary>
        //public Int32 Timeout { get; set; } = 30_000;

        /// <summary>发送数据包统计信息</summary>
        public ICounter StatSend { get; set; }

        /// <summary>接收数据包统计信息</summary>
        public ICounter StatReceive { get; set; }

        private readonly Object Root = new Object();
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
            if (!uri.IsNullOrEmpty()) Servers = uri.Split(",");
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            _Timer.TryDispose();

            Close(Name + (disposing ? "Dispose" : "GC"));
        }
        #endregion

        #region 打开关闭
        /// <summary>打开客户端</summary>
        public virtual Boolean Open()
        {
            if (Active) return true;
            lock (Root)
            {
                if (Active) return true;

                var ss = Servers;
                if (ss == null || ss.Length == 0) throw new ArgumentNullException(nameof(Servers), "未指定服务端地址");

                //if (Pool == null) Pool = new MyPool { Host = this };

                if (Encoder == null) Encoder = new JsonEncoder();
                //if (Encoder == null) Encoder = new BinaryEncoder();
                if (Handler == null) Handler = new ApiHandler { Host = this };
                //if (StatInvoke == null) StatInvoke = new PerfCounter();
                //if (StatProcess == null) StatProcess = new PerfCounter();
                //if (StatSend == null) StatSend = new PerfCounter();
                //if (StatReceive == null) StatReceive = new PerfCounter();

                Encoder.Log = EncoderLog;

                // 不要阻塞打开，各个线程从池里借出连接来使用
                //var ct = Pool.Get();
                //try
                //{
                //    // 打开网络连接
                //    if (!ct.Open()) return false;
                //}
                //finally
                //{
                //    Pool.Put(ct);
                //}

                ShowService();

                var ms = StatPeriod * 1000;
                if (ms > 0)
                {
                    if (StatInvoke == null) StatInvoke = new PerfCounter();
                    if (StatProcess == null) StatProcess = new PerfCounter();
                    if (StatSend == null) StatSend = new PerfCounter();
                    if (StatReceive == null) StatReceive = new PerfCounter();

                    _Timer = new TimerX(DoWork, null, ms, ms) { Async = true };
                }

                return Active = true;
            }
        }

        /// <summary>关闭</summary>
        /// <param name="reason">关闭原因。便于日志分析</param>
        /// <returns>是否成功</returns>
        public virtual void Close(String reason)
        {
            if (!Active) return;

            var ct = GetClient(false);
            if (ct != null) ct.Close(reason ?? (GetType().Name + "Close"));
            //Pool.TryDispose();
            //Pool = null;

            Active = false;
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
#if !NET4
            // 让上层异步到这直接返回，后续代码在另一个线程执行
            await Task.Yield();
#endif

            Open();

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
                    var client = GetClient(true);
                    await OnLoginAsync(client);

                    return await ApiHostHelper.InvokeAsync(this, this, resultType, act, args, flag);
                }

                throw;
            }
            // 截断任务取消异常，避免过长
            catch (TaskCanceledException ex)
            {
                throw new TaskCanceledException($"[{action}]超时取消", ex);
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
            // 发送失败时，返回空
            var rs = await InvokeAsync(typeof(TResult), action, args, flag);
            if (rs == null) return default(TResult);

            return (TResult)rs;
        }

        /// <summary>同步调用，阻塞等待</summary>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <param name="flag">标识</param>
        /// <returns></returns>
        public virtual TResult Invoke<TResult>(String action, Object args = null, Byte flag = 0)
        {
            // 发送失败时，返回空
            var rs = InvokeAsync(typeof(TResult), action, args, flag).Result;
            if (rs == null) return default(TResult);

            return (TResult)rs;
        }

        /// <summary>单向发送。同步调用，不等待返回</summary>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <param name="flag">标识</param>
        /// <returns></returns>
        public virtual Boolean InvokeOneWay(String action, Object args = null, Byte flag = 0)
        {
            if (!Open()) return false;

            var act = action;

            return ApiHostHelper.Invoke(this, this, act, args, flag);
        }

        /// <summary>指定客户端的异步调用，等待返回结果</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="client">客户端</param>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <param name="flag">标识</param>
        /// <returns></returns>
        protected virtual async Task<TResult> InvokeWithClientAsync<TResult>(ISocketClient client, String action, Object args = null, Byte flag = 0)
        {
            var act = action;

            return (TResult)await ApiHostHelper.InvokeAsync(this, client, typeof(TResult), act, args, flag);
        }

        async Task<Tuple<IMessage, Object>> IApiSession.SendAsync(IMessage msg)
        {
            Exception last = null;
            ISocketClient client = null;

            var count = Servers.Length;
            for (var i = 0; i < count; i++)
            {
                try
                {
                    client = GetClient(true);
                    var rs = (await client.SendMessageAsync(msg)) as IMessage;
                    return new Tuple<IMessage, Object>(rs, client);
                }
                catch (Exception ex)
                {
                    last = ex;
                    client.TryDispose();
                }
            }

            if (ShowError) WriteLog("请求[{0}]错误！Timeout=[{1}ms] {2}", client, Timeout, last?.GetMessage());

            throw last;
        }

        Boolean IApiSession.Send(IMessage msg)
        {
            Exception last = null;
            ISocketClient client = null;

            var count = Servers.Length;
            for (var i = 0; i < count; i++)
            {
                try
                {
                    client = GetClient(true);
                    return client.SendMessage(msg);
                }
                catch (Exception ex)
                {
                    last = ex;
                    client.TryDispose();
                }
            }

            throw last;
        }
        #endregion

        #region 事件
        /// <summary>新会话。客户端每次连接或断线重连后，可用InvokeWithClientAsync做登录</summary>
        /// <param name="session">会话</param>
        /// <param name="state">状态。客户端ISocketClient</param>
        public override void OnNewSession(IApiSession session, Object state)
        {
            var client = state as ISocketClient;
            OnLoginAsync(client)?.Wait();
        }

        /// <summary>连接后自动登录</summary>
        /// <param name="client">客户端</param>
        protected virtual Task<Object> OnLoginAsync(ISocketClient client) => null;
        #endregion

        #region 连接池
        ///// <summary>连接池</summary>
        //public IPool<ISocketClient> Pool { get; private set; }

        /// <summary>创建回调</summary>
        public Action<ISocketClient> CreateCallback { get; set; }

        //class MyPool : ObjectPool<ISocketClient>
        //{
        //    public ApiClient Host { get; set; }

        //    public MyPool()
        //    {
        //        // 最小值为0，连接池不再使用栈，只使用队列
        //        Min = 0;
        //        Max = 100000;
        //    }

        //    protected override ISocketClient OnCreate() => Host.OnCreate();
        //}

        private ISocketClient _Client;
        /// <summary>获取客户端</summary>
        /// <param name="create">是否创建</param>
        /// <returns></returns>
        protected virtual ISocketClient GetClient(Boolean create)
        {
            var tc = _Client;
            if (!create) return tc;

            if (tc != null && tc.Active && !tc.Disposed) return tc;
            lock (this)
            {
                tc = _Client;
                if (tc != null && tc.Active && !tc.Disposed) return tc;

                return _Client = OnCreate();
            }
        }

        /// <summary>Round-Robin 负载均衡</summary>
        private Int32 _index = -1;

        /// <summary>为连接池创建连接</summary>
        /// <returns></returns>
        protected virtual ISocketClient OnCreate()
        {
            // 遍历所有服务，找到可用服务端
            var svrs = Servers;
            if (svrs == null || svrs.Length == 0) throw new InvalidOperationException("没有设置服务端地址Servers");

            var idx = Interlocked.Increment(ref _index);
            Exception last = null;
            for (var i = 0; i < svrs.Length; i++)
            {
                // Round-Robin 负载均衡
                var k = (idx + i) % svrs.Length;
                var svr = svrs[k];
                try
                {
                    var client = OnCreate(svr);
                    CreateCallback?.Invoke(client);
                    client.Open();

                    return client;
                }
                catch (Exception ex)
                {
                    last = ex;
                }
            }

            throw last;
        }

        /// <summary>创建客户端之后，打开连接之前</summary>
        /// <param name="svr"></param>
        protected virtual ISocketClient OnCreate(String svr)
        {
            var client = new NetUri(svr).CreateRemote();
            //client.Timeout = Timeout;
            //if (Log != null) client.Log = Log;
            client.StatSend = StatSend;
            client.StatReceive = StatReceive;

            //client.Add(new StandardCodec { Timeout = Timeout, UserPacket = false });
            client.Add(GetMessageCodec());

            client.Opened += (s, e) => OnNewSession(this, s);
            client.Received += Client_Received;

            return client;
        }

        private void Client_Received(Object sender, ReceivedEventArgs e)
        {
            LastActive = DateTime.Now;

            // Api解码消息得到Action和参数
            var msg = e.Message as IMessage;
            if (msg == null || msg.Reply) return;

            var ss = sender as ISocketRemote;
            var host = this as IApiHost;
            var rs = host.Process(this, msg);
            if (rs != null) ss?.SendMessage(rs);
        }
        #endregion

        #region 统计
        private TimerX _Timer;
        private String _Last;

        /// <summary>显示统计信息的周期。默认600秒，0表示不显示统计信息</summary>
        public Int32 StatPeriod { get; set; } = 600;

        private void DoWork(Object state)
        {
            var msg = this.GetStat();
            if (msg.IsNullOrEmpty() || msg == _Last) return;
            _Last = msg;

            WriteLog(msg);
        }
        #endregion
    }
}