using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NewLife.Net;
using NewLife.Net.Sockets;

namespace NewLife.MessageQueue
{
    /// <summary>MQ服务器</summary>
    public class MQServer : NetServer<MQSession>
    {
        /// <summary>实例化</summary>
        public MQServer()
        {
            Port = 2234;
        }
    }

    /// <summary>MQ会话</summary>
    public class MQSession : NetSession
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }
        #endregion

        #region 接收分流
        protected override void OnReceive(ReceivedEventArgs e)
        {
            base.OnReceive(e);

            var str = e.ToStr();

            var act = str.Substring(null, " ");
            str = str.TrimStart(act).Trim();

            switch (act)
            {
                case "Name":
                    OnName(str);
                    break;
                case "Public":
                    OnPublic(str);
                    break;
                case "Subscribe":
                    OnSubscribe(str);
                    break;
                case "Message":
                    OnMessage(str);
                    break;
                default:
                    WriteLog("MQ会话收到：{0} {1}", act, str);
                    break;
            }
        }
        #endregion

        #region 标识
        protected virtual void OnName(String str)
        {
            Name = str;
            LogPrefix = Name;

            WriteLog("名称：{0}", Name);
        }
        #endregion

        #region 发布主题
        protected virtual void OnPublic(String str)
        {
            WriteLog("发布：{0}", str);
        }
        #endregion

        #region 订阅主题
        protected virtual void OnSubscribe(String str)
        {
            WriteLog("订阅：{0}", str);
        }
        #endregion

        #region 发送消息
        protected virtual void OnMessage(String str)
        {
            WriteLog("消息：{0}", str);
        }
        #endregion

        #region 辅助
        #endregion
    }
}