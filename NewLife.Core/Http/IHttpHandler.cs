using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Http;

/// <summary>Http处理器接口</summary>
/// <remarks>
/// <para>实现该接口以处理匹配到的Http请求。处理契约为异步：返回 <see cref="Task"/>，同步实现直接返回 <c>TaskEx.CompletedTask</c>。</para>
/// <para>为什么整体异步化而不保留同步成员：处理器内常见异步等待（数据库/外部 HTTP/文件 IO），同步成员会把异步等待摊到同步桥上，
/// 异步形态也无法在同步签名上无损表达。实现者数量少且集中（本库与生态均为可控改动），随大版本一次性升级。</para>
/// </remarks>
public interface IHttpHandler
{
    /// <summary>异步处理请求</summary>
    /// <param name="context">Http上下文，包含请求、响应和参数等信息</param>
    /// <returns>处理任务</returns>
    Task ProcessRequestAsync(IHttpContext context);
}

/// <summary>Http请求处理委托</summary>
/// <param name="context">Http上下文</param>
public delegate void HttpProcessDelegate(IHttpContext context);

/// <summary>Http请求处理异步委托</summary>
/// <param name="context">Http上下文</param>
/// <returns>处理任务</returns>
public delegate Task HttpProcessAsyncDelegate(IHttpContext context);

/// <summary>委托Http处理器</summary>
/// <remarks>将委托包装为 IHttpHandler，支持多种委托签名（同步/异步、有无参数与返回值）</remarks>
public class DelegateHandler : IHttpHandler
{
    /// <summary>委托回调</summary>
    public Delegate? Callback { get; set; }

    /// <summary>异步处理请求。委托返回任务时直接等待，不再阻塞取值</summary>
    /// <param name="context">Http上下文</param>
    /// <returns>处理任务</returns>
    public virtual async Task ProcessRequestAsync(IHttpContext context)
    {
        var handler = Callback;
        if (handler is HttpProcessDelegate httpHandler)
        {
            httpHandler(context);
        }
        else if (handler is HttpProcessAsyncDelegate asyncHandler)
        {
            await asyncHandler(context).ConfigureAwait(false);
        }
        else if (handler != null)
        {
            var result = OnInvoke(handler, context);
            if (result is Task task)
            {
                // 异步委托（含任意签名的 async 方法）：等任务完成后再取结果，避免同步阻塞等待
                await task.ConfigureAwait(false);
                result = TaskHelper.GetResult(task);
            }
            if (result != null) context.Response.SetResult(result);
        }
    }

    /// <summary>执行复杂委托调用</summary>
    /// <param name="handler">委托</param>
    /// <param name="context">Http上下文</param>
    /// <returns>调用结果</returns>
    protected virtual Object? OnInvoke(Delegate handler, IHttpContext context)
    {
        var mi = handler.Method;
        var pis = mi.GetParameters();
        try
        {
            if (pis.Length == 0) return handler.DynamicInvoke();

            var args = ParameterBinder.Bind(mi, context);

            return handler.DynamicInvoke(args);
        }
        catch (TargetInvocationException tie) when (tie.InnerException != null)
        {
            // 同步委托抛出的业务异常会被 DynamicInvoke 包成 TargetInvocationException；
            // 还原原始异常，让 ApiException/HttpException 的状态码映射与日志看到真实业务错误
            ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
            throw;
        }
    }
}

/// <summary>Task结果提取辅助类</summary>
internal static class TaskHelper
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> _resultCache = new();

    /// <summary>读取已完成Task的结果属性值，不等待。用于 await 之后取回结果</summary>
    /// <param name="task">已完成的Task</param>
    /// <returns>Task的Result属性值；若Task无返回值则返回null</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Task<>))]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "按运行时泛型实参反射读取任务结果；AOT 下请使用泛型等待入口")]
    public static Object? GetResult(Task task)
    {
        var taskType = task.GetType();
        if (!taskType.IsGenericType) return null;

        // 缓存 PropertyInfo 避免每次反射查找
        var resultProperty = _resultCache.GetOrAdd(taskType, t => t.GetProperty("Result", BindingFlags.Public | BindingFlags.Instance));
        return resultProperty?.GetValue(task);
    }
}