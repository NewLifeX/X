using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Threading;
using System.Xml.Serialization;

#nullable enable
namespace NewLife
{
    /// <summary>具有是否已释放和释放后事件的接口</summary>
    public interface IDisposable2 : IDisposable
    {
        /// <summary>是否已经释放</summary>
        [XmlIgnore, IgnoreDataMember]
        Boolean Disposed { get; }

        /// <summary>被销毁时触发事件</summary>
        event EventHandler OnDisposed;
    }

    /// <summary>具有销毁资源处理的抽象基类</summary>
    /// <remarks>
    /// 文档 https://www.yuque.com/smartstone/nx/disposebase
    /// </remarks>
    /// <example>
    /// <code>
    /// /// &lt;summary&gt;子类重载实现资源释放逻辑时必须首先调用基类方法&lt;/summary&gt;
    /// /// &lt;param name="disposing"&gt;从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）。
    /// /// 因为该方法只会被调用一次，所以该参数的意义不太大。&lt;/param&gt;
    /// protected override void Dispose(bool disposing)
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
    public abstract class DisposeBase : IDisposable2
    {
        #region 释放资源
        /// <summary>释放资源</summary>
        public void Dispose()
        {
            Dispose(true);

            // 告诉GC，不要调用析构函数
            GC.SuppressFinalize(this);
        }

        [NonSerialized]
        private Int32 _disposed = 0;
        /// <summary>是否已经释放</summary>
        [XmlIgnore, IgnoreDataMember]
        public Boolean Disposed => _disposed > 0;

        /// <summary>被销毁时触发事件</summary>
        [field: NonSerialized]
        public event EventHandler? OnDisposed;

        /// <summary>释放资源，参数表示是否由Dispose调用。重载时先调用基类方法</summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(Boolean disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0) return;

            if (disposing)
            {
                // 释放托管资源
                //OnDispose(disposing);

                // 告诉GC，不要调用析构函数
                GC.SuppressFinalize(this);
            }

            // 释放非托管资源

            OnDisposed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>释放资源，参数表示是否由Dispose调用。该方法保证OnDispose只被调用一次！</summary>
        /// <param name="disposing"></param>
        [Obsolete("=>Dispose")]
        protected virtual void OnDispose(Boolean disposing) { }

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
        public static Object? TryDispose(this Object? obj)
        {
            if (obj == null) return obj;

            // 列表元素销毁
            if (obj is IEnumerable ems)
            {
                // 对于枚举成员，先考虑添加到列表，再逐个销毁，避免销毁过程中集合改变
                if (obj is not IList list)
                {
                    list = new List<Object>();
                    foreach (var item in ems)
                    {
                        if (item is IDisposable) list.Add(item);
                    }
                }
                foreach (var item in list)
                {
                    if (item is IDisposable disp)
                    {
                        try
                        {
                            //(item as IDisposable).TryDispose();
                            // 只需要释放一层，不需要递归
                            // 因为一般每一个对象负责自己内部成员的释放
                            disp.Dispose();
                        }
                        catch { }
                    }
                }
            }
            // 对象销毁
            if (obj is IDisposable disp2)
            {
                try
                {
                    disp2.Dispose();
                }
                catch { }
            }

            return obj;
        }
    }
}
#nullable restore