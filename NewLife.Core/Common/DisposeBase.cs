using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using System.Xml.Serialization;
using NewLife.Log;

namespace NewLife
{
    /// <summary>具有是否已释放和释放后事件的接口</summary>
    public interface IDisposable2 : IDisposable
    {
        /// <summary>是否已经释放</summary>
        [XmlIgnore]
        Boolean Disposed { get; }

        /// <summary>被销毁时触发事件</summary>
        event EventHandler OnDisposed;
    }

    /// <summary>具有销毁资源处理的抽象基类</summary>
    /// <example>
    /// <code>
    /// /// &lt;summary&gt;子类重载实现资源释放逻辑时必须首先调用基类方法&lt;/summary&gt;
    /// /// &lt;param name="disposing"&gt;从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）。
    /// /// 因为该方法只会被调用一次，所以该参数的意义不太大。&lt;/param&gt;
    /// protected override void OnDispose(bool disposing)
    /// {
    ///     base.OnDispose(disposing);
    /// 
    ///     if (disposing)
    ///     {
    ///         // 如果是构造函数进来，不执行这里的代码
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class DisposeBase : CriticalFinalizerObject, IDisposable2
    {
        #region 释放资源
        /// <summary>释放资源</summary>
        public void Dispose() { Dispose(true); }

        [NonSerialized]
        private Int32 disposed = 0;
        /// <summary>是否已经释放</summary>
        [XmlIgnore]
        public Boolean Disposed { get { return disposed > 0; } }

        /// <summary>被销毁时触发事件</summary>
        [field: NonSerialized]
        public event EventHandler OnDisposed;
        
        /// <summary>释放资源，参数表示是否由Dispose调用。该方法保证OnDispose只被调用一次！</summary>
        /// <param name="disposing"></param>
        private void Dispose(Boolean disposing)
        {
            if (disposed != 0) return;
            if (Interlocked.CompareExchange(ref disposed, 1, 0) != 0) return;

            if (XTrace.Debug)
            {
                try
                {
                    OnDispose(disposing);
                }
                catch (Exception ex)
                {
                    XTrace.WriteLine("设计错误，OnDispose中尽可能的不要抛出异常！{0}", ex.ToString());
                    throw;
                }
            }
            else
            {
                OnDispose(disposing);
            }

            // 只有基类的OnDispose被调用，才有可能是2
            if (Interlocked.CompareExchange(ref disposed, 3, 2) != 2) throw new XException("设计错误，OnDispose应该只被调用一次！代码不应该直接调用OnDispose，而应该调用Dispose。子类重载OnDispose时必须首先调用基类方法！");
        }

        /// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）。
        /// 因为该方法只会被调用一次，所以该参数的意义不太大。</param>
        protected virtual void OnDispose(Boolean disposing)
        {
            // 只有从Dispose中调用，才有可能是1
            if (Interlocked.CompareExchange(ref disposed, 2, 1) != 1) throw new XException("设计错误，OnDispose应该只被调用一次！代码不应该直接调用OnDispose，而应该调用Dispose。");

            if (disposing)
            {
                // 释放托管资源

                // 告诉GC，不要调用析构函数
                GC.SuppressFinalize(this);
            }

            // 释放非托管资源

            if (OnDisposed != null) OnDisposed(this, EventArgs.Empty);
        }

        /// <summary>析构函数</summary>
        /// <remarks>
        /// 如果忘记调用Dispose，这里会释放非托管资源
        /// 如果曾经调用过Dispose，因为GC.SuppressFinalize(this)，不会再调用该析构函数
        /// </remarks>
        ~DisposeBase() { Dispose(false); }
        #endregion
    }

    /// <summary>销毁助手。扩展方法专用</summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static class DisposeHelper
    {
        /// <summary>尝试销毁对象，如果有<see cref="IDisposable"/>则调用</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Object TryDispose(this Object obj)
        {
            if (obj == null) return obj;

            // 列表元素销毁
            if (obj is IEnumerable)
            {
                // 对于枚举成员，先考虑添加到列表，再逐个销毁，避免销毁过程中集合改变
                var list = obj as IList;
                if (list == null)
                {
                    list = new List<Object>();
                    foreach (var item in (obj as IEnumerable))
                    {
                        if (item is IDisposable) list.Add(item);
                    }
                }
                foreach (var item in list)
                {
                    if (item is IDisposable)
                    {
                        try
                        {
                            //(item as IDisposable).TryDispose();
                            // 只需要释放一层，不需要递归
                            // 因为一般每一个对象负责自己内部成员的释放
                            (item as IDisposable).Dispose();
                        }
                        catch { }
                    }
                }
            }
            // 对象销毁
            if (obj is IDisposable)
            {
                try
                {
                    (obj as IDisposable).Dispose();
                }
                catch { }
            }

            return obj;
        }
    }
}