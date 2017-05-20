using System;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Net;
using NewLife.Reflection;

namespace NewLife.Remoting
{
    class ApiNetClient : DisposeBase, IApiClient, IServiceProvider
    {
        #region 属性
        /// <summary>是否已打开</summary>
        public Boolean Active { get; set; }

        public ISocketClient Client { get; set; }

        /// <summary>服务提供者</summary>
        public IServiceProvider Provider { get; set; }
        #endregion

        #region 构造
        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            Close(GetType().Name + (disposing ? "Dispose" : "GC"));
        }
        #endregion

        #region 方法
        public virtual Boolean Init(Object config)
        {
            var uri = config is String ? new NetUri(config + "") : config as NetUri;
            if (uri == null) return false;

            Client = uri.CreateRemote();

            // 新生命标准网络封包协议
            Client.Packet = new DefaultPacket();

            // Udp客户端默认超时时间
            if (Client is UdpServer) (Client as UdpServer).SessionTimeout = 10 * 60;

            // 网络非法断开时，自动恢复
            Client.OnDisposed += (s, e) => { if (Active) { Init(config); Open(); } };

            return true;
        }

        public Boolean Open()
        {
            var tc = Client;
            tc.MessageReceived += Client_Received;
            tc.Log = Log;
            tc.Opened += Client_Opened;

            return Active = tc.Open();
        }

        /// <summary>关闭</summary>
        /// <param name="reason">关闭原因。便于日志分析</param>
        public void Close(String reason)
        {
            Active = false;

            var tc = Client;
            tc.MessageReceived -= Client_Received;
            tc.Opened -= Client_Opened;
            tc.Close(reason);
        }

        /// <summary>打开后触发。</summary>
        public event EventHandler Opened;

        private void Client_Opened(Object sender, EventArgs e)
        {
            Opened?.Invoke(this, e);
        }
        #endregion

        #region 发送
        /// <summary>创建消息</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public IMessage CreateMessage(Packet pk) { return Client?.Packet?.CreateMessage(pk) ?? new Message { Payload = pk }; }

        /// <summary>远程调用</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<IMessage> SendAsync(IMessage msg) { return await Client.SendAsync(msg).ConfigureAwait(false); }
        #endregion

        #region 异步接收
        private void Client_Received(Object sender, MessageEventArgs e)
        {
            var msg = e.Message;
            if (msg.Reply) return;

            var ac = Provider as ApiClient;
            if (ac != null) ac.LastActive = DateTime.Now;

            //var host = this.GetService<IApiHost>();
            var host = Provider as IApiHost;

            var rs = host.Process(host as IApiSession, msg);
            if (rs != null) Client.SendAsync(rs);
        }
        #endregion

        #region 服务提供者
        /// <summary>获取服务提供者</summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public Object GetService(Type serviceType)
        {
            // 服务类是否当前类的基类
            if (GetType().As(serviceType)) return this;

            if (serviceType == typeof(IApiClient)) return this;
            if (serviceType == typeof(ISocketClient)) return Client;

            return Provider?.GetService(serviceType);
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;
        #endregion
    }
}