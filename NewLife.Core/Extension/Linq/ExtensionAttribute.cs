#if !NET4
namespace System.Runtime.CompilerServices
{
    /// <summary>支持使用扩展方法的特性</summary>
    /// <remarks>
    /// 为了能在vs2010+.Net 2.0中使用扩展方法，添加该特性。
    /// </remarks>
    public sealed class ExtensionAttribute : Attribute { }
}
#endif