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
///
/// 控制器可通过以下方式获取当前 IHttpContext：
/// 
/// 1. 方法参数类型注入（推荐）：方法声明 IHttpContext 或 IServiceProvider 类型参数，自动注入
/// <code>
/// public String Info(IHttpContext ctx, String name) { var ua = ctx.Request.Headers["User-Agent"]; }
/// </code>
/// 
/// 2. 构造函数 DI 注入：控制器构造函数声明 IHttpContext 参数，由当前请求的服务提供者解析
/// <code>
/// public class MyController(IHttpContext ctx) { public String Info() => ctx.Path; }
/// </code>
/// 
/// 3. IHttpController 接口：实现该接口后 Context 属性自动填充
/// <code>
/// public class MyController : IHttpController { public IHttpContext? Context { get; set; } }
/// </code>
/// 
/// 4. 静态访问：DefaultHttpContext.Current（仅同步代码，async 场景不可靠）
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
        // （HttpSession 已通过 HttpServiceProvider 将 IHttpContext 加入 DI 解析链）
        var serviceProvider = context.ServiceProvider;
        var controller = serviceProvider?.GetService(type) ?? serviceProvider?.CreateInstance(type) ?? type.CreateInstance();

        // 注入 IHttpContext 到控制器（若实现了 IHttpController 接口）
        if (controller is IHttpController httpController)
            httpController.Context = context;

        // 查找方法，增加缓存
        MethodInfo? method = null;
        if (methodName != null && !_methods.TryGetValue(methodName, out method))
        {
            method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.IgnoreCase);
            _methods[methodName] = method;
        }
        if (method == null) throw new ApiException(ApiCode.NotFound, $"Cannot find operation [{methodName}] within controller [{type.FullName}]");

        var args = ParameterBinder.Bind(method, context);
        var result = controller.InvokeWithParams(method, args);
        if (result is Task task) result = TaskHelper.GetTaskResult(task);
        if (result != null)
            context.Response.SetResult(result);
    }
}