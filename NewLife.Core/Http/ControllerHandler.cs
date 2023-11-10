using System.Collections;
using System.Reflection;
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
        if (ControllerType == null) return;

        var ss = context.Path.Split('/');
        var methodName = ss.Length >= 3 ? ss[2] : null;

        var controller = ControllerType.CreateInstance();

        var method = methodName == null ? null : ControllerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (method == null) throw new ApiException(ApiCode.NotFound, $"Cannot find operation [{methodName}] within controller [{ControllerType.FullName}]");

        var result = controller.InvokeWithParams(method, context.Parameters as IDictionary);
        if (result != null)
            context.Response.SetResult(result);
    }
}