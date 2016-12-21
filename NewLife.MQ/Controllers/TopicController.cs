using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Remoting;

namespace NewLife.MessageQueue
{
    /// <summary>主题控制器</summary>
    public class TopicController : IApi
    {
        /// <summary>Api接口会话</summary>
        public IApiSession Session { get; set; }

        /// <summary>创建主题</summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        [DisplayName("创建主题")]
        public Boolean Create(String topic)
        {
            //var host = Session["Host"] as IApiServer;
            var host = Session.GetService<ApiServer>();
            var topics = host["Topics"] as IDictionary<String, Topic>;

            if (topics.ContainsKey(topic)) throw new Exception("主题[{0}]已存在".F(topic));

            XTrace.WriteLine("创建主题 {0}", topic);

            var tp = new Topic();
            tp.Name = topic;

            topics[topic] = tp;

            return true;
        }

        /// <summary>订阅</summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        [DisplayName("订阅主题")]
        public Boolean Subscribe(String topic)
        {
            XTrace.WriteLine("订阅主题 {0}", topic);

            return true;
        }
    }
}