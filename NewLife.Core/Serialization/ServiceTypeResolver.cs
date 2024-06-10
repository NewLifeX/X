#if NET7_0_OR_GREATER
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
    public void Modifier(JsonTypeInfo typeInfo)
    {
        var type = typeInfo.Type;
        var provider = GetServiceProvider?.Invoke();
        if (provider != null && !type.IsBaseType())
        {
            if (provider.GetService(type) is not null)
            {
                typeInfo.CreateObject = () => provider.GetService(type) ?? provider.CreateInstance(type) ?? type.CreateInstance()!;
            }
        }
    }
}
#endif