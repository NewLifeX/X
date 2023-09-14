#if NETFRAMEWORK || NETSTANDARD2_0
namespace System.Diagnostics.CodeAnalysis;

/// <summary>指定在方法返回 ReturnValue 时，即使相应的类型允许，参数也不会为 null。</summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
public sealed class NotNullWhenAttribute : Attribute
{
    /// <summary>获取返回值条件。</summary>
    public Boolean ReturnValue { get; }

    /// <summary>使用指定的返回值条件初始化属性。</summary>
    /// <param name="returnValue"></param>
    public NotNullWhenAttribute(Boolean returnValue) => ReturnValue = returnValue;
}
#endif