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
        public IDictionary<String, Topic> Topics { get; private set; }

        /// <summary>实例化</summary>
        public MQServer()
        {
            Port = 2234;

            Topics = new Dictionary<string, Topic>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>MQ会话</summary>
    public class MQSession : NetSession
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        public MQServer Host { get; private set; }

        /// <summary>发布或订阅的主题。暂时没想好怎么做发布多主题或者订阅多主题</summary>
        public Topic Topic { get; set; }
        #endregion

        #region 主要方法
        public override void Start()
        {
            base.Start();

            Host = (this as INetSession).Host as MQServer;
        }

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

            Topic tp = null;
            if (!Host.Topics.TryGetValue(str, out tp))
            {
                tp = new Topic();
                tp.Name = str;
                Host.Topics.Add(str, tp);
            }

            Topic = tp;
        }
        #endregion

        #region 订阅主题
        protected virtual void OnSubscribe(String str)
        {
            WriteLog("订阅：{0}", str);

            Topic tp = null;
            if (Host.Topics.TryGetValue(str, out tp))
            {
                Topic = tp;

                tp.Subscribers.Add(this);
            }
        }
        #endregion

        #region 发送消息
        protected virtual void OnMessage(String str)
        {
            WriteLog("消息：{0}", str);

            if (Topic != null) Topic.Enqueue(str);
        }
        #endregion

        #region 推送消息
        public virtual Boolean SendMessage(String str)
        {
            Send(str);

            return true;
        }
        #endregion

        #region 辅助
        #endregion
    }
}