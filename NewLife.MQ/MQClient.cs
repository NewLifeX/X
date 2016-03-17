using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife.Net;

namespace NewLife.MessageQueue
{
    /// <summary>MQ客户端</summary>
    public class MQClient : DisposeBase
    {
        #region 属性
        public NetUri Remote { get; set; }

        public ISocketClient Client { get; set; }
        #endregion

        #region 构造函数
        public MQClient()
        {
            Remote = new NetUri(ProtocolType.Tcp, NetHelper.MyIP(), 2234);
        }
        #endregion

        #region 启动方法
        public void Open()
        {
            if (Client == null || Client.Disposed)
            {
                Client = Remote.CreateRemote();
                Client.Open();
            }
        }
        #endregion

        #region 发布订阅
        /// <summary>发布主题</summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        public Boolean Public(String topic)
        {
            return true;
        }

        /// <summary>订阅主题</summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        public Boolean Subscribe(String topic)
        {
            return true;
        }
        #endregion

        #region 收发消息
        /// <summary>发布消息</summary>
        /// <param name="topic"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public Boolean Send(String topic, Object msg)
        {
            return true;
        }

        public EventHandler<EventArgs<Object>> Received;
        #endregion
    }
}