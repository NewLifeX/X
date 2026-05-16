#if NETFRAMEWORK || NETSTANDARD2_0
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks;

/// <summary>为异步枚举和异步可销毁对象提供配置行为的静态扩展方法集</summary>
public static class TaskAsyncEnumerableExtensions
{
    /// <summary>配置异步可销毁对象上等待的行为</summary>
    /// <param name="source">源异步可销毁对象</param>
    /// <param name="continueOnCapturedContext">true 时捕获并切回当前上下文；false 则不捕获</param>
    /// <returns>已配置的异步可销毁对象</returns>
    public static ConfiguredAsyncDisposable ConfigureAwait(this IAsyncDisposable source, Boolean continueOnCapturedContext) =>
        new ConfiguredAsyncDisposable(source, continueOnCapturedContext);

    /// <summary>配置异步迭代中等待任务的行为</summary>
    /// <typeparam name="T">被迭代的对象类型</typeparam>
    /// <param name="source">被迭代的源枚举对象</param>
    /// <param name="continueOnCapturedContext">true 时捕获并切回当前上下文；false 则不捕获</param>
    /// <returns>已配置的可枚举对象</returns>
    public static ConfiguredCancelableAsyncEnumerable<T> ConfigureAwait<T>(this IAsyncEnumerable<T> source, Boolean continueOnCapturedContext) =>
        new ConfiguredCancelableAsyncEnumerable<T>(source, continueOnCapturedContext, cancellationToken: default);

    /// <summary>设置传递给 <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator"/> 的取消令牌</summary>
    /// <typeparam name="T">被迭代的对象类型</typeparam>
    /// <param name="source">被迭代的源枚举对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已配置的可枚举对象</returns>
    public static ConfiguredCancelableAsyncEnumerable<T> WithCancellation<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken) =>
        new ConfiguredCancelableAsyncEnumerable<T>(source, continueOnCapturedContext: true, cancellationToken);
}
#endif
