using System.Reflection;
using NewLife.Data;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Remoting;

/// <summary>Api动作</summary>
public class ApiAction
{
    /// <summary>动作名称</summary>
    public String Name { get; }

    /// <summary>动作所在类型</summary>
    public Type Type { get; }

    /// <summary>方法</summary>
    public MethodInfo Method { get; }

    /// <summary>控制器对象</summary>
    /// <remarks>如果指定控制器对象，则每次调用前不再实例化对象</remarks>
    public Object? Controller { get; set; }

    /// <summary>是否二进制参数</summary>
    public Boolean IsPacketParameter { get; }

    /// <summary>是否二进制返回</summary>
    public Boolean IsPacketReturn { get; }

    /// <summary>处理统计</summary>
    public ICounter StatProcess { get; set; } = new PerfCounter();

    /// <summary>最后会话</summary>
    public String? LastSession { get; set; }

    /// <summary>实例化</summary>
    public ApiAction(MethodInfo method, Type type)
    {
        if (type == null) type = method.DeclaringType;
        Name = GetName(type, method);

        // 必须同时记录类型和方法，因为有些方法位于继承的不同层次，那样会导致实例化的对象不一致
        Type = type;
        Method = method;

        var ps = method.GetParameters();
        if (ps != null && ps.Length == 1 && ps[0].ParameterType == typeof(Packet)) IsPacketParameter = true;

        if (method.ReturnType == typeof(Packet)) IsPacketReturn = true;
    }

    /// <summary>获取名称</summary>
    /// <param name="type"></param>
    /// <param name="method"></param>
    /// <returns></returns>
    public static String GetName(Type? type, MethodInfo method)
    {
        if (type == null) type = method.DeclaringType;
        //if (type == null) return null;

        var typeName = type.Name.TrimEnd("Controller", "Service");
        var att = type.GetCustomAttribute<ApiAttribute>(true);
        if (att != null) typeName = att.Name;

        var miName = method.Name;
        att = method.GetCustomAttribute<ApiAttribute>();
        if (att != null) miName = att.Name;

        if (typeName.IsNullOrEmpty() || miName.Contains('/'))
            return miName;
        else
            return $"{typeName}/{miName}";
    }

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString()
    {
        var mi = Method;

        var type = mi.ReturnType;
        var rtype = type.Name;
        if (type.As<Task>())
        {
            if (!type.IsGenericType)
                rtype = "void";
            else
            {
                type = type.GetGenericArguments()[0];
                rtype = type.Name;
            }
        }

        var ps = mi.GetParameters().Select(pi => $"{pi.ParameterType.Name} {pi.Name}").Join(", ");
        return $"{rtype} {mi.Name}({ps})";
    }
}