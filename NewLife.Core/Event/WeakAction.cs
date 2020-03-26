using System;
using System.Reflection;
using NewLife.Reflection;

namespace NewLife
{
    /// <summary>弱引用Action</summary>
    /// <remarks>
    /// 常见的事件和委托，都包括两部分：对象和方法，当然如果委托到静态方法上，对象是为空的。
    /// 如果把事件委托到某个对象的方法上，同时就间接的引用了这个对象，导致其一直无法被回收，从而造成内存泄漏。
    /// 弱引用Action，原理就是把委托拆分，然后弱引用对象部分，需要调用委托的时候，再把对象“拉”回来，如果被回收了，就没有必要再调用它的方法了。
    /// </remarks>
    /// <typeparam name="TArgs"></typeparam>
    public class WeakAction<TArgs>
    {
        #region 属性
        /// <summary>目标对象。弱引用，使得调用方对象可以被GC回收</summary>
        readonly WeakReference Target;

        /// <summary>委托方法</summary>
        readonly MethodBase Method;

        /// <summary>经过包装的新的委托</summary>
        readonly Action<TArgs> Handler;

        /// <summary>取消注册的委托</summary>
        Action<Action<TArgs>> UnHandler;

        /// <summary>是否只使用一次，如果只使用一次，执行委托后马上取消注册</summary>
        readonly Boolean Once;
        #endregion

        #region 扩展属性
        /// <summary>是否可用</summary>
        public Boolean IsAlive
        {
            get
            {
                var target = Target;
                if (target == null && Method.IsStatic) return true;

                return target != null && target.IsAlive;
            }
        }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        /// <param name="target">目标对象</param>
        /// <param name="method">目标方法</param>
        public WeakAction(Object target, MethodInfo method) : this(target, method, null, false) { }

        /// <summary>实例化</summary>
        /// <param name="target">目标对象</param>
        /// <param name="method">目标方法</param>
        /// <param name="unHandler">取消注册回调</param>
        /// <param name="once">是否一次性事件</param>
        public WeakAction(Object target, MethodInfo method, Action<Action<TArgs>> unHandler, Boolean once)
        {
            if (target != null)
            {
                Target = new WeakReference(target);
            }
            else
            {
                if (!method.IsStatic) throw new InvalidOperationException("非法事件，没有指定类实例且不是静态方法！");
            }

            Method = method;
            Handler = Invoke;
            UnHandler = unHandler;
            Once = once;
        }

        /// <summary>实例化</summary>
        /// <param name="handler">事件处理器</param>
        public WeakAction(Delegate handler) : this(handler.Target, handler.Method, null, false) { }

        /// <summary>使用事件处理器、取消注册回调、是否一次性事件来初始化</summary>
        /// <param name="handler">事件处理器</param>
        /// <param name="unHandler">取消注册回调</param>
        /// <param name="once">是否一次性事件</param>
        public WeakAction(Delegate handler, Action<Action<TArgs>> unHandler, Boolean once) : this(handler.Target, handler.Method, unHandler, once) { }
        #endregion

        #region 方法
        /// <summary>调用委托</summary>
        /// <param name="e"></param>
        public void Invoke(TArgs e)
        {
            //if (!Target.IsAlive) return;
            // Keep in mind that，不要用上面的写法，因为判断可能通过，但是接着就被GC回收了，如果判断Target，则会增加引用
            Object target = null;
            if (Target == null)
            {
                if (Method.IsStatic) Reflect.Invoke(null, Method, e);
            }
            else
            {
                target = Target.Target;
                if (target != null)
                {
                    // 优先使用委托
                    if (Method is MethodInfo mi)
                        mi.As<Action<TArgs>>(target).Invoke(e);
                    else
                        target.Invoke(Method, e);
                }
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
        public static implicit operator Action<TArgs>(WeakAction<TArgs> handler) => handler.Handler;
        #endregion

        #region 辅助
        /// <summary>已重载</summary>
        /// <returns></returns>
        public override String ToString()
        {
            if (Method == null) return base.ToString();

            if (Method.DeclaringType != null)
                return String.Format("{0}.{1}", Method.DeclaringType.Name, Method.Name);
            else
                return Method.Name;
        }
        #endregion
    }
}