using System;
using System.Threading;
using NewLife.Reflection;

namespace NewLife
{
    /// <summary>弱引用事件</summary>
    /// <remarks>
    /// 很多绑定事件的场合，并不适合取消绑定，这就造成了事件资源无法得到回收。
    /// 更加麻烦的是，事件本身除了包含事件处理方法外，还会包含目标对象，也就导致目标对象无法得到释放。
    /// 弱引用事件的原理是把目标对象与事件处理方法分拆开来，使用弱引用来引用目标对象，保证目标对象能够得到有效的释放。
    /// 触发弱引用事件时，首先判断目标对象是否可用，因为其可能已经被GC回收，然后再通过快速访问方法调用事件处理方法。
    /// 也许有人会问，如果目标对象不可用怎么办？岂不是无法执行事件处理方法了？
    /// 我们换一个角度来看，既然目标对象都已经不存在了，它绑定的事件自然也就无需过问了！
    /// </remarks>
    /// <typeparam name="TEventArgs"></typeparam>
    public class WeakEventHandler<TEventArgs> where TEventArgs : EventArgs
    {
        #region 属性
        /// <summary>目标对象。弱引用，使得调用方对象可以被GC回收</summary>
        WeakReference Target;

        /// <summary>委托方法</summary>
        MethodInfoX Method;

        /// <summary>经过包装的新的委托</summary>
        EventHandler<TEventArgs> Handler;

        /// <summary>取消注册的委托</summary>
        Action<EventHandler<TEventArgs>> UnHandler;

        /// <summary>是否只使用一次，如果只使用一次，执行委托后马上取消注册</summary>
        Boolean Once;
        #endregion

        /// <summary>使用事件处理器、取消注册回调、是否一次性事件来初始化</summary>
        /// <param name="handler"></param>
        /// <param name="unHandler"></param>
        /// <param name="once"></param>
        public WeakEventHandler(EventHandler<TEventArgs> handler, Action<EventHandler<TEventArgs>> unHandler, Boolean once)
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

        /// <summary>调用委托</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Invoke(Object sender, TEventArgs e)
        {
            //if (!Target.IsAlive) return;
            // Keep in mind that，不要用上面的写法，因为判断可能通过，但是接着就被GC回收了，如果判断Target，则会增加引用
            Object target = null;
            if (Target == null)
            {
                if (Method.Method.IsStatic) Method.Invoke(null, new Object[] { sender, e });
            }
            else
            {
                target = Target.Target;
                if (target != null)
                    Method.Invoke(target, new Object[] { sender, e });
            }

            // 调用方已被回收，或者该事件只使用一次，则取消注册
            if ((Target != null && target == null || Once) && UnHandler != null)
            {
                UnHandler(Handler);
                UnHandler = null;
            }
        }

        /// <summary>把弱引用事件处理器转换为普通事件处理器</summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static implicit operator EventHandler<TEventArgs>(WeakEventHandler<TEventArgs> handler)
        {
            return handler.Handler;
        }

        /// <summary>绑定</summary>
        /// <param name="handler"></param>
        public void Combine(ref EventHandler<TEventArgs> handler)
        {
            //handler += Handler;

            EventHandler<TEventArgs> oldHandler;
            EventHandler<TEventArgs> newHandler = null;
            do
            {
                oldHandler = handler;
                if (oldHandler == null) return;

                newHandler = Delegate.Combine(oldHandler, Handler) as EventHandler<TEventArgs>;

            } while (Interlocked.CompareExchange<EventHandler<TEventArgs>>(ref handler, newHandler, oldHandler) != oldHandler);
        }

        /// <summary>移除</summary>
        /// <param name="handler"></param>
        /// <param name="value"></param>
        public static void Remove(ref EventHandler<TEventArgs> handler, EventHandler<TEventArgs> value)
        {
            //handler -= value;

            EventHandler<TEventArgs> oldHandler;
            EventHandler<TEventArgs> newHandler = null;
            do
            {
                oldHandler = handler;
                if (oldHandler == null) return;

                Delegate[] ds = oldHandler.GetInvocationList();
                if (ds == null || ds.Length < 1) return;

                for (int i = 0; i < ds.Length; i++)
                {
                    WeakEventHandler<TEventArgs> wh = ds[i].Target as WeakEventHandler<TEventArgs>;
                    if (wh == null || wh.Method.Method != value.Method) continue;

                    // 判断对象
                    if (wh.Target == null)
                    {
                        if (value.Target != null) continue;
                    }
                    else
                    {
                        if (value.Target == null || wh.Target.Target != value.Target) continue;
                    }

                    // 移除
                    newHandler = Delegate.Remove(oldHandler, ds[i]) as EventHandler<TEventArgs>;
                }
            } while (Interlocked.CompareExchange<EventHandler<TEventArgs>>(ref handler, newHandler, oldHandler) != oldHandler);
        }
    }
}