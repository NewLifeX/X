using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Reflection;

namespace NewLife
{
    /// <summary>
    /// 弱引用Action
    /// </summary>
    /// <typeparam name="TArgs"></typeparam>
    public class WeakAction<TArgs>
    {
         #region 属性
        /// <summary>
        /// 目标对象。弱引用，使得调用方对象可以被GC回收
        /// </summary>
        WeakReference Target;

        /// <summary>
        /// 委托方法
        /// </summary>
        MethodInfoX Method;

        /// <summary>
        /// 经过包装的新的委托
        /// </summary>
        Action<TArgs> Handler;

        /// <summary>
        /// 取消注册的委托
        /// </summary>
        Action<Action<TArgs>> UnHandler;

        /// <summary>
        /// 是否只使用一次，如果只使用一次，执行委托后马上取消注册
        /// </summary>
        Boolean Once;
        #endregion

        /// <summary>
        /// 使用事件处理器、取消注册回调、是否一次性事件来初始化
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="unHandler"></param>
        /// <param name="once"></param>
        public WeakAction(Action<TArgs> handler, Action<Action<TArgs>> unHandler, Boolean once)
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

        /// <summary>
        /// 调用委托
        /// </summary>
        /// <param name="e"></param>
        public void Invoke(TArgs e)
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

        /// <summary>
        /// 把弱引用事件处理器转换为普通事件处理器
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static implicit operator Action<TArgs>(WeakAction<TArgs> handler)
        {
            return handler.Handler;
        }
   }
}
