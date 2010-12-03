using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace XCode.Common
{
    class WeakEventHandler<TEventArgs>
    {
        #region 属性
        /// <summary>
        /// 目标对象。弱引用，使得调用方对象可以被GC回收
        /// </summary>
        WeakReference Target;

        /// <summary>
        /// 委托方法
        /// </summary>
        MethodInfoEx Method;

        /// <summary>
        /// 经过包装的新的委托
        /// </summary>
        Action<TEventArgs> Handler;

        /// <summary>
        /// 取消注册的委托
        /// </summary>
        Action<Action<TEventArgs>> UnHandler;

        /// <summary>
        /// 是否只使用一次，如果只使用一次，执行委托后马上取消注册
        /// </summary>
        Boolean Once;
        #endregion

        public WeakEventHandler(Action<TEventArgs> handler, Action<Action<TEventArgs>> unHandler, Boolean once)
        {
            if (handler.Target != null)
            {
                Target = new WeakReference(handler.Target);
            }
            else
            {
                if (!handler.Method.IsStatic) throw new InvalidOperationException("非法事件，没有指定类实例且不是静态方法！");
            }

            //Method = MethodInfoEx.Create(handler.Method);
            Method = handler.Method;
            Handler = Invoke;
            UnHandler = unHandler;
            Once = once;
        }

        public void Invoke(TEventArgs e)
        {
            //if (!Target.IsAlive) return;
            // Keep in mind that，不要用上面的写法，因为判断可能通过，但是接着就被GC回收了，如果判断Target，则会增加引用
            Object target = null;
            if (Target == null)
            {
                if (Method.Method.IsStatic) Method.Invoke(null, new Object[] { e });
            }
            else
            {
                target = Target.Target;
                if (target != null)
                    Method.Invoke(target, new Object[] { e });
            }

            // 调用方已被回收，或者该事件只使用一次，则取消注册
            if ((Target != null && target == null || Once) && UnHandler != null)
            {
                UnHandler(Handler);
                UnHandler = null;
            }
        }

        public static implicit operator Action<TEventArgs>(WeakEventHandler<TEventArgs> handler)
        {
            return handler.Handler;
        }
    }
}