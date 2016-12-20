using System;
using System.Threading.Tasks;
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
        #endregion

        #region 构造函数
        /// <summary>实例化</summary>
        public MQClient()
        {
            Remote = new NetUri(NetType.Tcp, NetHelper.MyIP(), 2234);
            // 还未上消息格式，暂时用Udp替代Tcp，避免粘包问题
            //Remote = new NetUri(ProtocolType.Udp, NetHelper.MyIP(), 2234);
        }
        #endregion

        #region 启动方法
        /// <summary>打开</summary>
        public void Open()
        {
            var ac = Client;
            if (ac == null || ac.Disposed)
            {
                ac = new ApiClient(Remote);
                ac.Open();

                Client = ac;

                //SendPack("Name", Name);
            }
        }
        #endregion

        #region 发布订阅
        /// <summary>发布主题</summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        public async Task<Boolean> AddTopic(String topic)
        {
            Open();

            //SendPack("Public", topic);
            var rs = await Client.InvokeAsync<Message>("Topic/Add", new { });

            return rs != null;
        }

        /// <summary>订阅主题</summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        public async Task<Boolean> Subscribe(String topic)
        {
            Open();

            //SendPack("Subscribe", topic);

            var rs = await Client.InvokeAsync<Message>("Topic/Subscribe", new { });

            return rs != null;
        }
        #endregion

        #region 收发消息
        /// <summary>发布消息</summary>
        /// <param name="topic"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<Boolean> Public(String topic, Object msg)
        {
            Open();

            //SendPack("Message", msg + "");

            var rs = await Client.InvokeAsync<Message>("Message/Public", new { });

            return rs != null;
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
    }
}