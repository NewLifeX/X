using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Remoting;

namespace NewLife.MessageQueue
{
    /// <summary>主题控制器</summary>
    public class TopicController : IApi
    {
        /// <summary>Api接口会话</summary>
        public IApiSession Session { get; set; }

        /// <summary>添加主题</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Boolean Add(String name)
        {
            //var host = Session["Host"] as IApiServer;
            var host = Session.GetService<ApiServer>();
            var topics = host["Topics"] as IDictionary<String, Topic>;

            if (topics.ContainsKey(name)) throw new Exception("主题[{0}]已存在".F(name));

            var tp = new Topic();
            tp.Name = name;

            topics[name] = tp;

            return true;
        }
    }
}