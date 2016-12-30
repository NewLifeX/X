using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Net;

namespace NewLife.Remoting
{
    class ApiNetClient : DisposeBase, IApiClient, IApiSession, IServiceProvider
    {
        #region 属性
        public NetUri Remote { get; set; }

        public ISocketClient Client { get; set; }

        /// <summary>服务提供者</summary>
        public IServiceProvider Provider { get; set; }

        /// <summary>所有服务器所有会话，包含自己</summary>
        public virtual IApiSession[] AllSessions { get { return new IApiSession[] { this }; } }

        /// <summary>用户会话数据</summary>
        public IDictionary<String, Object> Items { get; set; } = new Dictionary<String, Object>();

        /// <summary>获取/设置 用户会话数据</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual Object this[String key] { get { return Items.ContainsKey(key) ? Items[key] : null; } set { Items[key] = value; } }
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
        public virtual bool Init(object config)
        {
            var uri = config as NetUri;
            if (uri == null) return false;

            Client = uri.CreateRemote();
            Remote = uri;

            // 新生命标准网络封包协议
            Client.Packet = new DefaultPacket();

            // Udp客户端默认超时时间
            if (Client is UdpServer) (Client as UdpServer).SessionTimeout = 10 * 60;

            return true;
        }

        public void Open()
        {
            Client.MessageReceived += Client_Received;
            Client.Log = Log;
#if DEBUG
            Client.LogSend = true;
            Client.LogReceive = true;
            Client.Timeout = 60 * 1000;
#endif
            Client.Opened += Client_Opened;
            Client.Open();
        }

        /// <summary>关闭</summary>
        /// <param name="reason">关闭原因。便于日志分析</param>
        public void Close(String reason)
        {
            Client.MessageReceived -= Client_Received;
            Client.Opened -= Client_Opened;
            Client.Close(reason);
        }

        /// <summary>打开后触发。</summary>
        public event EventHandler Opened;

        private void Client_Opened(Object sender, EventArgs e)
        {
            Opened?.Invoke(this, e);
        }
        #endregion

        #region 发送
        /// <summary>远程调用</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<TResult> InvokeAsync<TResult>(string action, object args = null)
        {
            var ac = Provider.GetService<ApiClient>();
            var enc = ac.Encoder;
            var data = enc.Encode(action, args);

            var msg = Client.Packet.CreateMessage(data);

            var rs = await Client.SendAsync(msg);

            var dic = enc.Decode(rs?.Payload);

            return enc.Decode<TResult>(dic);
        }
        #endregion

        #region 异步接收
        private void Client_Received(Object sender, MessageEventArgs e)
        {
            var ac = this.GetService<ApiClient>();
            var enc = ac.Encoder;

            var msg = e.Message;
            // 这里会导致二次解码，因为解码以后才知道是不是请求
            var dic = enc.Decode(msg.Payload);

            var act = "";
            Object args = null;
            if (enc.TryGet(dic, out act, out args))
            {
                OnInvoke(msg, act, args as IDictionary<string, object>);
            }
        }

        /// <summary>处理远程调用</summary>
        /// <param name="msg"></param>
        /// <param name="action"></param>
        /// <param name="args"></param>
        protected virtual async void OnInvoke(IMessage msg, string action, IDictionary<string, object> args)
        {
            var ac = this.GetService<ApiClient>();
            var enc = ac.Encoder;
            object result = null;
            var rs = msg.CreateReply();
            var r = false;
            try
            {
                result = await ac.Handler.Execute(this, action, args);

                r = true;
            }
            catch (Exception ex)
            {
                var msg2 = rs as DefaultMessage;
                if (msg2 != null) msg2.Error = true;
                result = ex;
            }

            rs.Payload = enc.Encode(r, result);

            await Client.SendAsync(rs);
        }
        #endregion

        #region 服务提供者
        /// <summary>获取服务提供者</summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public Object GetService(Type serviceType)
        {
            if (serviceType == GetType()) return this;
            if (serviceType == typeof(IApiClient)) return this;
            if (serviceType == typeof(IApiSession)) return this;

            return Provider?.GetService(serviceType);
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;
        #endregion
    }
}