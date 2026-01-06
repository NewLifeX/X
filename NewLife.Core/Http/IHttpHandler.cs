using System.Reflection;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Http;

/// <summary>Http处理器接口</summary>
/// <remarks>实现该接口以处理匹配到的Http请求</remarks>
public interface IHttpHandler
{
    /// <summary>处理请求</summary>
    /// <param name="context">Http上下文，包含请求、响应和参数等信息</param>
    void ProcessRequest(IHttpContext context);
}

/// <summary>Http请求处理委托</summary>
/// <param name="context">Http上下文</param>
public delegate void HttpProcessDelegate(IHttpContext context);

/// <summary>委托Http处理器</summary>
/// <remarks>将委托包装为 IHttpHandler，支持多种委托签名</remarks>
public class DelegateHandler : IHttpHandler
{
    /// <summary>委托回调</summary>
    public Delegate? Callback { get; set; }

    /// <summary>处理请求</summary>
    /// <param name="context">Http上下文</param>
    public virtual void ProcessRequest(IHttpContext context)
    {
        var handler = Callback;
        if (handler is HttpProcessDelegate httpHandler)
        {
            httpHandler(context);
        }
        else if (handler != null)
        {
            var result = OnInvoke(handler, context);
            if (result is Task task) result = TaskHelper.GetTaskResult(task);
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
        if (pis.Length == 0) return handler.DynamicInvoke();

        var parameters = context.Parameters;

        var args = new Object?[pis.Length];
        for (var i = 0; i < pis.Length; i++)
        {
            if (parameters.TryGetValue(pis[i].Name + "", out var v))
                args[i] = v.ChangeType(pis[i].ParameterType);
        }

        // 如果只有一个参数，且参数为空，则需要尝试从字典来反序列化
        if (args.Length == 1 && args[0] == null)
        {
            args[0] = JsonHelper.Default.Convert(parameters, pis[0].ParameterType);
        }

        return handler.DynamicInvoke(args);
    }
}

/// <summary>Task结果提取辅助类</summary>
internal static class TaskHelper
{
    /// <summary>同步获取Task的结果。仅用于简单Http处理场景</summary>
    /// <param name="task">要获取结果的Task</param>
    /// <returns>Task的Result属性值；若Task无返回值则返回null</returns>
    public static Object? GetTaskResult(Task task)
    {
        task.GetAwaiter().GetResult();

        var taskType = task.GetType();
        if (!taskType.IsGenericType) return null;

        var resultProperty = taskType.GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
        return resultProperty?.GetValue(task);
    }
}