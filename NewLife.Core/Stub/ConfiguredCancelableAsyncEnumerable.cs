#if NETFRAMEWORK || NETSTANDARD2_0
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices;

/// <summary>提供对 <see cref="System.IAsyncDisposable"/> 上等待行为的配置支持</summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct ConfiguredAsyncDisposable
{
    private readonly IAsyncDisposable _source;
    private readonly Boolean _continueOnCapturedContext;

    internal ConfiguredAsyncDisposable(IAsyncDisposable source, Boolean continueOnCapturedContext)
    {
        _source = source;
        _continueOnCapturedContext = continueOnCapturedContext;
    }

    /// <summary>异步释放当前对象使用的非托管资源</summary>
    /// <returns>表示异步释放操作的可等待对象</returns>
    public ConfiguredValueTaskAwaitable DisposeAsync() =>
        _source.DisposeAsync().ConfigureAwait(_continueOnCapturedContext);
}

/// <summary>提供可等待的异步枚举，支持可取消迭代和自定义等待上下文</summary>
/// <typeparam name="T">被迭代的对象类型</typeparam>
[StructLayout(LayoutKind.Auto)]
public readonly struct ConfiguredCancelableAsyncEnumerable<T>
{
    private readonly IAsyncEnumerable<T> _enumerable;
    private readonly CancellationToken _cancellationToken;
    private readonly Boolean _continueOnCapturedContext;

    internal ConfiguredCancelableAsyncEnumerable(IAsyncEnumerable<T> enumerable, Boolean continueOnCapturedContext, CancellationToken cancellationToken)
    {
        _enumerable = enumerable;
        _continueOnCapturedContext = continueOnCapturedContext;
        _cancellationToken = cancellationToken;
    }

    /// <summary>配置异步迭代中等待任务的方式</summary>
    /// <param name="continueOnCapturedContext">true 时捕获并切回当前上下文；false 则不捕获</param>
    /// <returns>已配置的可枚举对象</returns>
    public ConfiguredCancelableAsyncEnumerable<T> ConfigureAwait(Boolean continueOnCapturedContext) =>
        new ConfiguredCancelableAsyncEnumerable<T>(_enumerable, continueOnCapturedContext, _cancellationToken);

    /// <summary>设置传递给 <see cref="System.Collections.Generic.IAsyncEnumerable{T}.GetAsyncEnumerator"/> 的取消令牌</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已配置的可枚举对象</returns>
    public ConfiguredCancelableAsyncEnumerable<T> WithCancellation(CancellationToken cancellationToken) =>
        new ConfiguredCancelableAsyncEnumerable<T>(_enumerable, _continueOnCapturedContext, cancellationToken);

    /// <summary>返回可对集合进行异步迭代的枚举器</summary>
    /// <returns>可枚举对象的枚举器</returns>
    public Enumerator GetAsyncEnumerator() =>
        new Enumerator(_enumerable.GetAsyncEnumerator(_cancellationToken), _continueOnCapturedContext);

    /// <summary>提供可等待的异步枚举器，支持可取消迭代和自定义等待上下文</summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct Enumerator
    {
        private readonly IAsyncEnumerator<T> _enumerator;
        private readonly Boolean _continueOnCapturedContext;

        internal Enumerator(IAsyncEnumerator<T> enumerator, Boolean continueOnCapturedContext)
        {
            _enumerator = enumerator;
            _continueOnCapturedContext = continueOnCapturedContext;
        }

        /// <summary>将枚举器异步推进到集合的下一个元素</summary>
        /// <returns>若成功推进到下一个元素则为 true；若已越过集合末尾则为 false</returns>
        public ConfiguredValueTaskAwaitable<Boolean> MoveNextAsync() =>
            _enumerator.MoveNextAsync().ConfigureAwait(_continueOnCapturedContext);

        /// <summary>获取集合中当前位置的元素</summary>
        public T Current => _enumerator.Current;

        /// <summary>异步释放枚举器使用的资源</summary>
        /// <returns>表示异步释放操作的可等待对象</returns>
        public ConfiguredValueTaskAwaitable DisposeAsync() =>
            _enumerator.DisposeAsync().ConfigureAwait(_continueOnCapturedContext);
    }
}
#endif
