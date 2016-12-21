using System;
using System.ComponentModel;
using NewLife.Log;

namespace NewLife.MessageQueue
{
    class ClientController
    {
        /// <summary>通知</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [DisplayName("通知")]
        public Boolean Notify(Message msg)
        {
            XTrace.WriteLine("订阅通知 {0}", msg?.Body?.ToStr());

            return true;
        }
    }
}