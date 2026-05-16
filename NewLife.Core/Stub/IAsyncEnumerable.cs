#if NETFRAMEWORK || NETSTANDARD2_0
namespace System.Collections.Generic;

/// <summary>公开一个枚举器，该枚举器提供对指定类型集合的异步迭代</summary>
/// <typeparam name="T">要枚举的对象类型</typeparam>
public interface IAsyncEnumerable<out T>
{
    /// <summary>返回一个在集合上异步迭代的枚举器</summary>
    /// <param name="cancellationToken">可用于取消异步迭代的取消令牌</param>
    /// <returns>可用于异步迭代集合的枚举器</returns>
    IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default);
}

/// <summary>支持对泛型集合进行简单的异步迭代</summary>
/// <typeparam name="T">要枚举的对象类型</typeparam>
public interface IAsyncEnumerator<out T> : IAsyncDisposable
{
    /// <summary>将枚举器异步推进到集合的下一个元素</summary>
    /// <returns>若成功推进到下一个元素则为 true；若已越过集合末尾则为 false</returns>
    ValueTask<Boolean> MoveNextAsync();

    /// <summary>获取集合中当前位置的元素</summary>
    T Current { get; }
}
#endif
