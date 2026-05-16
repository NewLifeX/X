#if NETFRAMEWORK || NETSTANDARD2_0
namespace System;

/// <summary>提供异步释放非托管资源的机制</summary>
public interface IAsyncDisposable
{
    /// <summary>异步释放当前对象使用的非托管资源</summary>
    /// <returns>表示异步释放操作的 ValueTask</returns>
    ValueTask DisposeAsync();
}
#endif
