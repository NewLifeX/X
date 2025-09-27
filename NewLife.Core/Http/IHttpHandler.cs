using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Http;

/// <summary>Http处理器</summary>
public interface IHttpHandler
{
    /// <summary>处理请求</summary>
    /// <param name="context"></param>
    void ProcessRequest(IHttpContext context);
}

/// <summary>Http请求处理委托</summary>
/// <param name="context"></param>
public delegate void HttpProcessDelegate(IHttpContext context);

/// <summary>委托Http处理器</summary>
public class DelegateHandler : IHttpHandler
{
    /// <summary>委托</summary>
    public Delegate? Callback { get; set; }

    /// <summary>处理请求</summary>
    /// <param name="context"></param>
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
            if (result != null) context.Response.SetResult(result);
        }
    }

    /// <summary>复杂调用</summary>
    /// <param name="handler"></param>
    /// <param name="context"></param>
    /// <returns></returns>
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