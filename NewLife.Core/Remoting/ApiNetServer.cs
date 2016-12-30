using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Net;

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
        public IApiSession[] AllSessions { get { return Sessions.Values.ToArray().Where(e => e is IApiSession).Cast<IApiSession>().ToArray(); } }

        public ApiNetServer()
        {
            Name = "Api";
            UseSession = true;
            SessionTimeout = 10 * 60;
        }

        /// <summary>初始化</summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public bool Init(string config)
        {
            Local = new NetUri(config);
#if DEBUG
            //LogSend = true;
            //LogReceive = true;
#endif
            // 新生命标准网络封包协议
            SessionPacket = new DefaultPacketFactory();

            return true;
        }

        /// <summary>启动中</summary>
        protected override void OnStart()
        {
            //if (Encoder == null) Encoder = new JsonEncoder();
            if (Encoder == null) throw new ArgumentNullException(nameof(Encoder), "未指定编码器");

            //base.EnsureCreateServer();

            //foreach (var item in Servers)
            //{
            //    // 新生命标准网络封包协议
            //    item.SessionPacket = new DefaultPacketFactory();
            //}

            base.OnStart();
        }

        /// <summary>获取服务提供者</summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public Object GetService(Type serviceType)
        {
            if (serviceType == typeof(ApiServer)) return Provider;
            if (serviceType == typeof(IEncoder) && Encoder != null) return Encoder;
            if (serviceType == typeof(IApiHandler) && Handler != null) return Handler;

            return Provider?.GetService(serviceType);
        }
    }

    class ApiNetSession : NetSession<ApiNetServer>, IApiSession
    {

        /// <summary>所有服务器所有会话，包含自己</summary>
        public virtual IApiSession[] AllSessions
        {
            get
            {
                //return Host.Sessions.Values.ToArray().Where(e => e is IApiSession).Cast<IApiSession>().ToArray();
                // 需要收集所有服务器的所有会话
                var svr = this.GetService<ApiServer>();
                return svr.Servers.SelectMany(e => e.AllSessions).ToArray();
            }
        }

        protected override void OnReceive(MessageEventArgs e)
        {
            var enc = Host.Encoder;

            var msg = e.Message;
            var dic = enc.Decode(msg?.Payload);

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
            var enc = Host.Encoder;
            object result = null;
            var rs = msg.CreateReply();
            var r = false;
            try
            {
                result = await Host.Handler.Execute(this, action, args);
                r = true;
            }
            catch (Exception ex)
            {
                var msg2 = rs as DefaultMessage;
                if (msg2 != null) msg2.Error = true;
                result = ex;
            }

            rs.Payload = enc.Encode(r, result);

            // 发送响应
            await Session.SendAsync(rs);
        }

        /// <summary>远程调用</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<TResult> InvokeAsync<TResult>(string action, object args = null)
        {
            var enc = Host.Encoder;
            var data = enc.Encode(action, args);

            var msg = Session.Packet.CreateMessage(new Packet(data));
            var rs = await Session.SendAsync(msg);

            var dic = enc.Decode(rs?.Payload);

            return enc.Decode<TResult>(dic);
        }

        /// <summary>获取服务提供者</summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public Object GetService(Type serviceType)
        {
            if (serviceType == typeof(IApiSession)) return this;
            if (serviceType == typeof(IApiServer)) return Host;

            return Host?.GetService(serviceType);
        }
    }
}
