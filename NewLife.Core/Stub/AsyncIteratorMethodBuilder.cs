#if NETFRAMEWORK || NETSTANDARD2_0
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices;

/// <summary>标识方法为异步迭代器</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class AsyncIteratorStateMachineAttribute : StateMachineAttribute
{
    /// <summary>初始化 <see cref="AsyncIteratorStateMachineAttribute"/> 实例</summary>
    /// <param name="stateMachineType">底层状态机类型</param>
    public AsyncIteratorStateMachineAttribute(Type stateMachineType) : base(stateMachineType) { }
}

/// <summary>标识参数应接收来自 <see cref="System.Collections.Generic.IAsyncEnumerable{T}.GetAsyncEnumerator"/> 的取消令牌</summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
public sealed class EnumeratorCancellationAttribute : Attribute { }

/// <summary>表示异步迭代器的构建器</summary>
/// <remarks>
/// 编译器生成的异步迭代器状态机使用此类型协调 async/await 与 yield return 逻辑。
/// 此为 .NET Standard 2.0 / .NET Framework 降级实现，通过包装 <see cref="AsyncTaskMethodBuilder"/> 提供等效能力。
/// </remarks>
[StructLayout(LayoutKind.Auto)]
public struct AsyncIteratorMethodBuilder
{
    private AsyncTaskMethodBuilder _methodBuilder; // 可变结构体，请勿设为 readonly
    private Object? _id;

    /// <summary>创建 <see cref="AsyncIteratorMethodBuilder"/> 实例</summary>
    /// <returns>已初始化的实例</returns>
    public static AsyncIteratorMethodBuilder Create() =>
        new AsyncIteratorMethodBuilder() { _methodBuilder = AsyncTaskMethodBuilder.Create() };

    /// <summary>在保护 <see cref="ExecutionContext"/> 的同时，调用状态机的 MoveNext</summary>
    /// <typeparam name="TStateMachine">状态机类型</typeparam>
    /// <param name="stateMachine">状态机实例（按引用传递）</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MoveNext<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine =>
        _methodBuilder.Start(ref stateMachine);

    /// <summary>在指定的等待者完成时调度状态机继续执行</summary>
    /// <typeparam name="TAwaiter">等待者类型</typeparam>
    /// <typeparam name="TStateMachine">状态机类型</typeparam>
    /// <param name="awaiter">等待者</param>
    /// <param name="stateMachine">状态机</param>
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine =>
        _methodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);

    /// <summary>在指定的等待者完成时调度状态机继续执行（不安全版本）</summary>
    /// <typeparam name="TAwaiter">等待者类型</typeparam>
    /// <typeparam name="TStateMachine">状态机类型</typeparam>
    /// <param name="awaiter">等待者</param>
    /// <param name="stateMachine">状态机</param>
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine =>
        _methodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);

    /// <summary>标记迭代已完成（无论成功还是失败）</summary>
    public void Complete() => _methodBuilder.SetResult();

    /// <summary>获取可用于调试器唯一标识此构建器的对象</summary>
    internal Object ObjectIdForDebugger => _id ?? Interlocked.CompareExchange(ref _id, new Object(), null) ?? _id!;
}
#endif
