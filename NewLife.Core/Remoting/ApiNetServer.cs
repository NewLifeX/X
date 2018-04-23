using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Net;
using NewLife.Net.Handlers;
using NewLife.Reflection;

namespace NewLife.Remoting
{
    class ApiNetServer : NetServer<ApiNetSession>, IApiServer
    {
        /// <summary>服务提供者</summary>
        public IServiceProvider Provider { get; set; }

        /// <summary>编码器</summary>
        public IEncoder Encoder { get; set; }

        /// <summary>处理器</summary>
        public IApiHandler Handler { get; set; }

        /// <summary>当前服务器所有会话</summary>
        public IApiSession[] AllSessions => Sessions.Values.ToArray().Where(e => e is IApiSession).Cast<IApiSession>().ToArray();

        public ApiNetServer()
        {
            Name = "Api";
            UseSession = true;
            //SessionTimeout = 10 * 60;
        }

        /// <summary>初始化</summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public virtual Boolean Init(String config)
        {
            Local = new NetUri(config);
            // 如果主机为空，监听所有端口
            if (Local.Host.IsNullOrEmpty() || Local.Host == "*") AddressFamily = System.Net.Sockets.AddressFamily.Unspecified;

            // 新生命标准网络封包协议
            Add(new StandardCodec { UserPacket = false });

            return true;
        }

        /// <summary>启动中</summary>
        protected override void OnStart()
        {
            //if (Encoder == null) Encoder = new JsonEncoder();
            if (Encoder == null) throw new ArgumentNullException(nameof(Encoder), "未指定编码器");

            base.OnStart();
        }

        /// <summary>获取服务提供者</summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public virtual Object GetService(Type serviceType)
        {
            // 服务类是否当前类的基类
            if (GetType().As(serviceType)) return this;

            if (serviceType == typeof(ApiServer)) return Provider;
            if (serviceType == typeof(IEncoder) && Encoder != null) return Encoder;
            if (serviceType == typeof(IApiHandler) && Handler != null) return Handler;

            return Provider?.GetService(serviceType);
        }
    }

    class ApiNetSession : NetSession<ApiNetServer>, IApiSession
    {
        private IApiHost _Host;
        /// <summary>主机</summary>
        IApiHost IApiSession.Host => _Host;

        /// <summary>最后活跃时间</summary>
        public DateTime LastActive { get; set; }

        /// <summary>所有服务器所有会话，包含自己</summary>
        public virtual IApiSession[] AllSessions
        {
            get
            {
                // 需要收集所有服务器的所有会话
                var svr = _Host as ApiServer;
                return svr.Servers.SelectMany(e => e.AllSessions).ToArray();
            }
        }

        /// <summary>开始会话处理</summary>
        public override void Start()
        {
            base.Start();

            _Host = Host.Provider as ApiServer;
        }

        /// <summary>查找Api动作</summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public virtual ApiAction FindAction(String action) => _Host.Manager.Find(action);

        /// <summary>创建控制器实例</summary>
        /// <param name="api"></param>
        /// <returns></returns>
        public virtual Object CreateController(ApiAction api) => _Host.CreateController(this, api);

        protected override void OnReceive(ReceivedEventArgs e)
        {
            LastActive = DateTime.Now;

            // Api解码消息得到Action和参数
            var msg = e.Message as IMessage;
            if (msg == null || msg.Reply) return;

            var rs = _Host.Process(this, msg);
            if (rs != null) Session?.SendAsync(rs);
        }

        /// <summary>创建消息</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public IMessage CreateMessage(Packet pk) => new Message { Payload = pk };

        /// <summary>远程调用</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<TResult> InvokeAsync<TResult>(String action, Object args = null) => await ApiHostHelper.InvokeAsync<TResult>(_Host, this, action, args);

        async Task<IMessage> IApiSession.SendAsync(IMessage msg) => await Session.SendAsync(msg) as IMessage;

        /// <summary>获取服务提供者</summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public Object GetService(Type serviceType)
        {
            // 服务类是否当前类的基类
            if (GetType().As(serviceType)) return this;

            if (serviceType == typeof(IApiSession)) return this;
            if (serviceType == typeof(IApiServer)) return Host;

            return Host?.GetService(serviceType);
        }
    }
}