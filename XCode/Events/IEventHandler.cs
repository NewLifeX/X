using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XCode
{
    /// <summary>
    /// 事件处理接口
    /// </summary>
    /// <typeparam name="TEvent">继承IEvent对象的事件源对象</typeparam>
    public interface IEventHandler<TEvent> where TEvent : IEvent
    {
        /// <summary>
        /// 处理程序
        /// </summary>
        /// <param name="evt"></param>
        void Handle(TEvent evt);

    }
}
