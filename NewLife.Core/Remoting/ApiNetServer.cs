using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewLife.Messaging;
using NewLife.Net;

namespace NewLife.Remoting
{
    class ApiNetServer : NetServer<ApiNetSession>, IApiServer
    {
        /// <summary>主机</summary>
        public IApiHost Host { get; set; }

        ///// <summary>编码器</summary>
        //public IEncoder Encoder { get; set; }

        ///// <summary>处理器</summary>
        //public IApiHandler Handler { get; set; }

        /// <summary>当前服务器所有会话</summary>
        public IApiSession[] AllSessions => Sessions.ToValueArray().Where(e => e is IApiSession).Cast<IApiSession>().ToArray();

        /// <summary>调用超时时间。默认30_000ms</summary>
        public Int32 Timeout { get; set; } = 30_000;

        public ApiNetServer()
        {
            Name = "Api";
            UseSession = true;
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
            //Add(new StandardCodec { Timeout = Timeout, UserPacket = false });
            Add(Host.GetMessageCodec());

            return true;
        }

        ///// <summary>启动中</summary>
        //protected override void OnStart()
        //{
        //    //if (Encoder == null) Encoder = new JsonEncoder();
        //    if (Encoder == null) throw new ArgumentNullException(nameof(Encoder), "未指定编码器");

        //    base.OnStart();
        //}
    }

    class ApiNetSession : NetSession<ApiNetServer>, IApiSession
    {
        private IApiHost _Host;
        /// <summary>主机</summary>
        IApiHost IApiSession.Host => _Host;

        /// <summary>最后活跃时间</summary>
        public DateTime LastActive { get; set; }

        /// <summary>所有服务器所有会话，包含自己</summary>
        public virtual IApiSession[] AllSessions => (_Host as ApiServer).Server.AllSessions;

        /// <summary>开始会话处理</summary>
        public override void Start()
        {
            base.Start();

            _Host = Host.Host;

            if (_Host is ApiHost host) host.OnNewSession(this, null);
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
            if (rs != null) Session?.SendMessage(rs);
        }

        ///// <summary>创建消息</summary>
        ///// <param name="pk"></param>
        ///// <returns></returns>
        //public IMessage CreateMessage(Packet pk) => new Message { Payload = pk };

        /// <summary>远程调用</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action">服务操作</param>
        /// <param name="args">参数</param>
        /// <param name="flag">标识</param>
        /// <returns></returns>
        public async Task<TResult> InvokeAsync<TResult>(String action, Object args = null, Byte flag = 0) => (TResult)await ApiHostHelper.InvokeAsync(_Host, this, typeof(TResult), action, args, flag);

        async Task<Tuple<IMessage, Object>> IApiSession.SendAsync(IMessage msg)
        {
            var rs = await Session.SendMessageAsync(msg) as IMessage;
            return new Tuple<IMessage, Object>(rs, Session);
        }

        Boolean IApiSession.Send(IMessage msg) => Session.SendMessage(msg);
    }
}