#if NETFRAMEWORK || NETSTANDARD2_0
namespace System.Diagnostics.CodeAnalysis;

/// <summary>当返回指定值时可能为空</summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
public sealed class MaybeNullWhenAttribute : Attribute
{
    /// <summary>返回值</summary>
    public Boolean ReturnValue { get; }

    /// <summary>实例化</summary>
    /// <param name="returnValue"></param>
    public MaybeNullWhenAttribute(Boolean returnValue) => ReturnValue = returnValue;
}
#endif