#if NET7_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using NewLife.Model;
using NewLife.Reflection;

namespace NewLife.Serialization;

/// <summary>支持服务提供者的类型解析器</summary>
public class ServiceTypeResolver
{
    /// <summary>服务提供者</summary>
    public Func<IServiceProvider>? GetServiceProvider { get; set; }

    /// <summary>匹配修改</summary>
    /// <param name="typeInfo"></param>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "IList/IDictionary 接口集合的类型解析需要运行时泛型实例化；AOT 下未预编译的实例化将由运行时明确失败")]
    public void Modifier(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object) return;

        var type = typeInfo.Type;
        if (!type.IsBaseType())
        {
            var provider = GetServiceProvider?.Invoke();
            if (provider != null && provider.GetService(type) is not null)
            {
                typeInfo.CreateObject = () => provider.GetService(type) ?? provider.CreateInstance(type) ?? type.CreateInstance()!;
            }
            else if (type.IsInterface && type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    var type2 = typeof(List<>).MakeGenericType(type.GetGenericArguments());
                    typeInfo.CreateObject = () => type2.CreateInstance()!;
                }
                else if (type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    var type2 = typeof(Dictionary<,>).MakeGenericType(type.GetGenericArguments());
                    typeInfo.CreateObject = () => type2.CreateInstance()!;
                }
            }
        }
    }
}
#endif