using System;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Net;
using NewLife.Remoting;

namespace NewLife.MessageQueue
{
    /// <summary>MQ客户端</summary>
    public class MQClient : DisposeBase
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>远程地址</summary>
        public NetUri Remote { get; set; }

        /// <summary>网络客户端</summary>
        public ApiClient Client { get; set; }

        /// <summary>已登录</summary>
        public Boolean Logined { get; private set; }
        #endregion

        #region 构造函数
        /// <summary>实例化</summary>
        public MQClient()
        {
            Remote = new NetUri(NetType.Tcp, NetHelper.MyIP(), 2234);
            // 还未上消息格式，暂时用Udp替代Tcp，避免粘包问题
            //Remote = new NetUri(ProtocolType.Udp, NetHelper.MyIP(), 2234);
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            Close(GetType().Name + (disposing ? "Dispose" : "GC"));

            Client.TryDispose();
        }
        #endregion

        #region 打开关闭
        /// <summary>打开</summary>
        public void Open()
        {
            var ac = Client;
            if (ac == null || ac.Disposed)
            {
                ac = new ApiClient(Remote);
                ac.Encoder = new JsonEncoder();
                ac.Log = Log;

                var ss = ac.Client as IApiSession;
                ss["user"] = Name;

                Logined = false;

                Client = ac;

                // 连接成功后自动登录
                ac.Opened += (s, e) => Task.Run(Login);

                ac.Open();

                //// 异步登录
                //if (!Name.IsNullOrEmpty()) Task.Run(Login);
            }
        }

        /// <summary>关闭</summary>
        public void Close(String reason)
        {
            Client.Close(reason ?? (GetType().Name + "Close"));
        }
        #endregion

        #region 登录验证
        /// <summary>登录</summary>
        /// <returns></returns>
        public async Task<Boolean> Login()
        {
            if (!Name.IsNullOrEmpty()) return false;

            Open();

            if (Logined) return true;

            var rs = await Client.InvokeAsync<Boolean>("User/Login", new { user = Name, pass = Name.MD5() });
            Logined = rs;

            return rs;
        }
        #endregion

        #region 发布订阅
        /// <summary>发布主题</summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        public async Task<Boolean> CreateTopic(String topic)
        {
            Open();

            Log.Info("{0} 创建主题 {1}", Name, topic);

            var rs = await Client.InvokeAsync<Boolean>("Topic/Create", new { topic });

            return rs;
        }

        /// <summary>订阅主题</summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        public async Task<Boolean> Subscribe(String topic)
        {
            Open();

            Log.Info("{0} 订阅主题 {1}", Name, topic);

            var rs = await Client.InvokeAsync<Boolean>("Topic/Subscribe", new { topic });

            if (rs) Client.Register<ClientController>();

            return rs;
        }
        #endregion

        #region 收发消息
        /// <summary>发布消息</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<Boolean> Public(Object msg)
        {
            Open();

            Log.Info("{0} 发布消息 {1}", Name, msg);

            // 对象编码为二进制
            var buf = Client.Encoder.Encode(msg);

            var m = new Message
            {
                Sender = Name,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddSeconds(60),
                Body = buf
            };

            var rs = await Client.InvokeAsync<Boolean>("Message/Public", new { msg = m });

            return rs;
        }

        /// <summary>接收</summary>
        public EventHandler<EventArgs<Message>> Received;

        //void Client_Received(object sender, ReceivedEventArgs e)
        //{
        //    Received?.Invoke(this, new EventArgs<String>(e.ToStr()));
        //}
        #endregion

        #region 辅助
        ///// <summary>响应</summary>
        ///// <param name="act"></param>
        ///// <param name="msg"></param>
        //protected virtual void SendPack(String act, String msg)
        //{
        //    Client.Send("{0}+{1}".F(act, msg));
        //    Thread.Sleep(200);
        //}
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;
        #endregion
    }
}