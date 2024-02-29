using System.Collections;
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
        var controller = context.ServiceProvider?.CreateInstance(type) ?? type.CreateInstance();

        var method = methodName == null ? null : type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (method == null) throw new ApiException(ApiCode.NotFound, $"Cannot find operation [{methodName}] within controller [{type.FullName}]");

        var result = controller.InvokeWithParams(method, context.Parameters as IDictionary);
        if (result != null)
            context.Response.SetResult(result);
    }
}