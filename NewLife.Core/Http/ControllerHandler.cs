using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Remoting;

namespace NewLife.Http;

/// <summary>控制器处理器</summary>
public class ControllerHandler : IHttpHandler
{
    #region 属性
    /// <summary>控制器类型</summary>
    public Type? ControllerType { get; set; }

    private ConcurrentDictionary<String, MethodInfo?> _Methods = new();
    #endregion

    /// <summary>处理请求</summary>
    /// <param name="context"></param>
    public virtual void ProcessRequest(IHttpContext context)
    {
        var type = ControllerType;
        if (type == null) return;

        var ss = context.Path.Split('/');
        var methodName = ss.Length >= 3 ? ss[2] : null;

        // 优先使用服务提供者创建控制器对象，以便控制器构造函数注入
        var serviceProvider = context.ServiceProvider;
        var controller = serviceProvider?.GetService(type) ?? serviceProvider?.CreateInstance(type) ?? type.CreateInstance();

        // 查找方法，增加缓存
        MethodInfo? method = null;
        if (methodName != null && !_Methods.TryGetValue(methodName, out method))
        {
            method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.IgnoreCase);
            _Methods[methodName] = method;
        }
        if (method == null) throw new ApiException(ApiCode.NotFound, $"Cannot find operation [{methodName}] within controller [{type.FullName}]");

        var result = controller.InvokeWithParams(method, context.Parameters as IDictionary);
        if (result is Task task) result = GetTaskResult(task);
        if (result != null)
            context.Response.SetResult(result);
    }

    private static Object? GetTaskResult(Task task)
    {
        task.GetAwaiter().GetResult();

        var taskType = task.GetType();
        if (!taskType.IsGenericType) return null;

        var resultProperty = taskType.GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
        return resultProperty?.GetValue(task);
    }
}