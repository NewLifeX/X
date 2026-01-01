using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Remoting;

namespace NewLife.Http;

/// <summary>控制器处理器</summary>
/// <remarks>
/// 将请求路由到控制器类型的指定方法，支持依赖注入创建控制器实例。
/// 路径格式：/{ControllerName}/{MethodName}
/// </remarks>
public class ControllerHandler : IHttpHandler
{
    #region 属性
    /// <summary>控制器类型</summary>
    public Type? ControllerType { get; set; }

    private readonly ConcurrentDictionary<String, MethodInfo?> _methods = new();
    #endregion

    /// <summary>处理请求</summary>
    /// <param name="context">Http上下文</param>
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
        if (methodName != null && !_methods.TryGetValue(methodName, out method))
        {
            method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.IgnoreCase);
            _methods[methodName] = method;
        }
        if (method == null) throw new ApiException(ApiCode.NotFound, $"Cannot find operation [{methodName}] within controller [{type.FullName}]");

        var result = controller.InvokeWithParams(method, context.Parameters as IDictionary);
        if (result is Task task) result = TaskHelper.GetTaskResult(task);
        if (result != null)
            context.Response.SetResult(result);
    }
}