using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewLife.Log;
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

            // Udp客户端默认超时时间
            if (Client is UdpServer) (Client as UdpServer).SessionTimeout = 10 * 60;

            return true;
        }

        public void Open()
        {
            Client.Received += Client_Received;
            Client.Log = Log;
#if DEBUG
            //Client.LogSend = true;
            //Client.LogReceive = true;
#endif
            Client.Opened += Client_Opened;
            Client.Open();
        }

        /// <summary>关闭</summary>
        /// <param name="reason">关闭原因。便于日志分析</param>
        public void Close(String reason)
        {
            Client.Received -= Client_Received;
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
        //public Task<byte[]> SendAsync(byte[] data)
        //{
        //    return Client.SendAsync(data);
        //}

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

            var rs = await Client.SendAsync(data);

            var dic = enc.Decode(rs?.ToArray());

            return enc.Decode<TResult>(dic);
        }
        #endregion

        #region 异步接收
        private void Client_Received(Object sender, ReceivedEventArgs e)
        {
            var ac = this.GetService<ApiClient>();
            var enc = ac.Encoder;

            // 这里会导致二次解码，因为解码以后才知道是不是请求
            var dic = enc.Decode(e.Data);

            var act = "";
            Object args = null;
            if (enc.TryGet(dic, out act, out args))
            {
                OnInvoke(act, args as IDictionary<string, object>);
            }
        }

        /// <summary>处理远程调用</summary>
        /// <param name="action"></param>
        /// <param name="args"></param>
        protected virtual async void OnInvoke(string action, IDictionary<string, object> args)
        {
            var ac = this.GetService<ApiClient>();
            var enc = ac.Encoder;
            object result = null;
            var rs = false;
            try
            {
                result = await ac.Handler.Execute(this, action, args);

                rs = true;
            }
            catch (Exception ex)
            {
                //result = ex.Message;
                result = ex;
            }

            var buf = enc.Encode(rs, result);

            Client.Send(buf);
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