using System;
using System.ComponentModel;
using NewLife.Log;
using NewLife.Remoting;

namespace NewLife.MessageQueue
{
    class ClientController : IApi
    {
        /// <summary>Api接口会话</summary>
        public IApiSession Session { get; set; }

        /// <summary>通知</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [DisplayName("通知")]
        public Boolean Notify(Message msg)
        {
            XTrace.WriteLine("{0} 收到订阅通知 {1}", Session["user"], msg);

            return true;
        }
    }
}