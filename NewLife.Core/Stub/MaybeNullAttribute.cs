#if NETFRAMEWORK || NETSTANDARD2_0
namespace System.Diagnostics.CodeAnalysis;

/// <summary>返回可能为空</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
public sealed class MaybeNullAttribute : Attribute
{
}
#endif