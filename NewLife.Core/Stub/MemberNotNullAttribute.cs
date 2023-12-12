#if NETFRAMEWORK || NETSTANDARD || NETCOREAPP3_1
namespace System.Diagnostics.CodeAnalysis;

/// <summary>执行方法后指定成员不为空</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
public sealed class MemberNotNullAttribute : Attribute
{
    /// <summary>不为空的成员</summary>
    public String[] Members { get; }

    /// <summary>成员不为空</summary>
    /// <param name="member"></param>
    public MemberNotNullAttribute(String member) => Members = [member];

    /// <summary>成员不为空</summary>
    /// <param name="members"></param>
    public MemberNotNullAttribute(params String[] members) => Members = members;
}
#endif