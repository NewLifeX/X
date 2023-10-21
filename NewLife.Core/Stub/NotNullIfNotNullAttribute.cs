#if NETFRAMEWORK || NETSTANDARD2_0
namespace System.Diagnostics.CodeAnalysis;

/// <summary>指定参数不为空时返回也不为空</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
public sealed class NotNullIfNotNullAttribute : Attribute
{
    /// <summary>指定参数</summary>
    public String ParameterName { get; }

    /// <summary>实例化</summary>
    /// <param name="parameterName"></param>
    public NotNullIfNotNullAttribute(String parameterName) => ParameterName = parameterName;
}
#endif