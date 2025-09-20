using System.Collections;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using NewLife.Log;

namespace NewLife;

/// <summary>具有是否已释放和释放后事件的接口</summary>
public interface IDisposable2 : IDisposable
{
    /// <summary>是否已经释放</summary>
    [XmlIgnore, IgnoreDataMember]
    Boolean Disposed { get; }

    /// <summary>被销毁时触发事件</summary>
    /// <remarks>
    /// 事件可能在 <see cref="DisposeBase.Dispose()"/> 或终结器线程（即析构函数路径）中触发，
    /// 订阅方应避免在回调中依赖特定线程上下文。
    /// </remarks>
    event EventHandler OnDisposed;
}

/// <summary>具有销毁资源处理的抽象基类</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/disposebase
/// </remarks>
/// <example>
/// <code>
/// /// &lt;summary&gt;子类重载实现资源释放逻辑时必须首先调用基类方法&lt;/summary&gt;
/// /// &lt;param name="disposing"&gt;从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）。
/// /// 因为该方法只会被调用一次，所以该参数的意义不太大。&lt;/param&gt;
/// protected override void Dispose(Boolean disposing)
/// {
///     // 先调用基类，确保只执行一次并处理通用逻辑
///     base.Dispose(disposing);
///
///     if (disposing)
///     {
///         // 这里释放托管资源（仅当从 Dispose() 进入时）
///     }
///
///     // 这里释放非托管资源（两种路径都可执行）
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
    /// <remarks>
    /// 该事件在对象销毁后触发，可能来自终结器线程。事件处理程序应尽量避免阻塞或抛出异常。
    /// </remarks>
    [field: NonSerialized]
    public event EventHandler? OnDisposed;

    /// <summary>释放资源，参数表示是否由Dispose调用。重载时先调用基类方法</summary>
    /// <param name="disposing">true 表示从 <see cref="Dispose()"/> 调用；false 表示终结器调用</param>
    protected virtual void Dispose(Boolean disposing)
    {
        // 保证只执行一次释放逻辑
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0) return;

        if (disposing)
        {
            // 释放托管资源
            //OnDispose(disposing);

            //// 告诉GC，不要调用析构函数
            //GC.SuppressFinalize(this);
        }

        // 释放非托管资源

        // 触发销毁事件（可能在终结器线程）
        try
        {
            OnDisposed?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            // 事件回调不应影响释放流程，吞掉异常
        }
    }

    ///// <summary>释放资源，参数表示是否由Dispose调用。该方法保证OnDispose只被调用一次！</summary>
    ///// <param name="disposing"></param>
    //[Obsolete("=>Dispose")]
    //protected virtual void OnDispose(Boolean disposing) { }

    /// <summary>在公开方法中调用，若对象已释放则抛出 <see cref="ObjectDisposedException"/></summary>
    protected void ThrowIfDisposed()
    {
        if (Disposed) throw new ObjectDisposedException(GetType().FullName);
    }

    /// <summary>析构函数</summary>
    /// <remarks>
    /// 如果忘记调用Dispose，这里会释放非托管资源。
    /// 如果曾经调用过Dispose，因为GC.SuppressFinalize(this)，不会再调用该析构函数。
    /// 在 .NET 中，析构函数（Finalizer）不应该抛出未捕获的异常。如果析构函数引发未捕获的异常，它将导致应用程序崩溃或进程退出。
    /// </remarks>
    ~DisposeBase()
    {
        // 在 .NET 中，析构函数（Finalizer）不应该抛出未捕获的异常。如果析构函数引发未捕获的异常，它将导致应用程序崩溃或进程退出。
        try
        {
            Dispose(false);
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }
    }
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