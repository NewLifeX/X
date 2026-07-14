using System.Reflection;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Http;

/// <summary>方法参数绑定器，从IHttpContext.Parameters中提取并转换方法实参</summary>
/// <remarks>
/// 供 DelegateHandler 和 ControllerHandler 共用，消除重复的参数注入逻辑。
/// </remarks>
internal static class ParameterBinder
{
    /// <summary>为方法调用绑定参数</summary>
    /// <param name="method">目标方法</param>
    /// <param name="context">Http上下文</param>
    /// <returns>参数数组</returns>
    public static Object?[] Bind(MethodInfo method, IHttpContext context)
    {
        var pis = method.GetParameters();
        if (pis.Length == 0) return [];

        var parameters = context.Parameters;

        var args = new Object?[pis.Length];
        for (var i = 0; i < pis.Length; i++)
        {
            var pi = pis[i];
            var name = pi.Name;
            if (name.IsNullOrEmpty()) continue;

            if (parameters.TryGetValue(name, out var v))
                args[i] = v.ChangeType(pi.ParameterType);
            else if (pi.HasDefaultValue)
                args[i] = pi.DefaultValue;
            else if (pi.ParameterType == typeof(IHttpContext))
                args[i] = context;
            else if (pi.ParameterType == typeof(IServiceProvider))
                args[i] = context.ServiceProvider;
        }

        // 只有一个参数且值为null时，尝试从整个参数字典反序列化为目标类型
        if (args.Length == 1 && args[0] == null)
        {
            args[0] = JsonHelper.Default.Convert(parameters, pis[0].ParameterType);
        }

        return args;
    }
}
