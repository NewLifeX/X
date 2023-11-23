#if NETFRAMEWORK || NETSTANDARD || NETCOREAPP3_1
namespace System.Diagnostics.CodeAnalysis;

/// <summary>执行方法后指定成员不为空（带条件）</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
public sealed class MemberNotNullWhenAttribute : Attribute
{
    /// <summary>返回值</summary>
    public Boolean ReturnValue { get; }

    /// <summary>不为空的成员</summary>
    public String[] Members { get; }

    /// <summary>成员不为空</summary>
    /// <param name="returnValue"></param>
    /// <param name="member"></param>
    public MemberNotNullWhenAttribute(Boolean returnValue, String member)
    {
        ReturnValue = returnValue;
        Members = [member];
    }

    /// <summary>成员不为空</summary>
    /// <param name="returnValue"></param>
    /// <param name="members"></param>
    public MemberNotNullWhenAttribute(Boolean returnValue, params String[] members)
    {
        ReturnValue = returnValue;
        Members = members;
    }
}
#endif