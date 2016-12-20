using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Remoting;

namespace NewLife.MessageQueue
{
    /// <summary>订阅者</summary>
    class Subscriber
    {
        /// <summary>接口会话</summary>
        public IApiSession Session { get; set; }

        /// <summary>发送消息给订阅者</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<Message> NoitfyAsync(Message msg)
        {
            return await Session.InvokeAsync<Message>("Noitfy", new { msg });
        }
    }
}