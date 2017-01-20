using System;
using NewLife.Data;
using NewLife.Messaging;

namespace NewLife.Remoting
{
    /// <summary>Api过滤上下文</summary>
    public class ApiFilterContext : FilterContext
    {
        /// <summary>会话</summary>
        public IApiSession Session { get; set; }

        /// <summary>消息</summary>
        public IMessage Message { get; set; }

        /// <summary>是否准备发送</summary>
        public Boolean IsSend { get; set; }

        ///// <summary>是否响应</summary>
        //public Boolean Reply { get; set; }
    }
}