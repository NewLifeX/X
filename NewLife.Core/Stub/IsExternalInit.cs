#if NETFRAMEWORK || NETSTANDARD || NETCOREAPP3_1
using System.ComponentModel;

namespace System.Runtime.CompilerServices;

/// <summary>保留供编译器用于跟踪元数据。 开发人员不应在源代码中使用此类。</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IsExternalInit
{
}
#endif