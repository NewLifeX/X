using System;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using NewLife.Log;

namespace NewLife
{
    /// <summary>
    /// 具有销毁资源处理的抽象基类
    /// </summary>
    public abstract class DisposeBase : CriticalFinalizerObject, IDisposable
    {
        #region 释放资源
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>是否已经释放</summary>
        public Boolean Disposed
        {
            get { return disposed > 0; }
        }

        private Int32 disposed = 0;
        /// <summary>
        /// 释放资源，参数表示是否由Dispose调用。该方法保证OnDispose只被调用一次！
        /// </summary>
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
            if (Interlocked.CompareExchange(ref disposed, 3, 2) != 2) throw new Exception("设计错误，OnDispose应该只被调用一次！代码不应该直接调用OnDispose，而应该调用Dispose。子类重载OnDispose时必须首先调用基类方法！");
        }

        /// <summary>
        /// 子类重载实现资源释放逻辑时必须首先调用基类方法
        /// </summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        protected virtual void OnDispose(Boolean disposing)
        {
            // 只有从Dispose中调用，才有可能是1
            if (Interlocked.CompareExchange(ref disposed, 2, 1) != 1) throw new Exception("设计错误，OnDispose应该只被调用一次！代码不应该直接调用OnDispose，而应该调用Dispose。");

            if (disposing)
            {
                // 释放托管资源

                // 告诉GC，不要调用析构函数
                GC.SuppressFinalize(this);
            }

            // 释放非托管资源
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~DisposeBase()
        {
            // 如果忘记调用Dispose，这里会释放非托管资源
            // 如果曾经调用过Dispose，因为GC.SuppressFinalize(this)，不会再调用该析构函数
            Dispose(false);
        }
        #endregion
    }
}
