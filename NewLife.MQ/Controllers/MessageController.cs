using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Remoting;

namespace NewLife.MessageQueue
{
    /// <summary>消息控制器</summary>
    public class MessageController:IApi
    {
        /// <summary>Api接口会话</summary>
        public IApiSession Session { get; set; }
    }
}