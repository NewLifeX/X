#if !NETFRAMEWORK && !NETSTANDARD2_0
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

// 将 Stub 文件（NETFRAMEWORK / NETSTANDARD2_0）中直接定义的类型，
// 在高版本框架（netstandard2.1 / netcoreapp3.1 / net5+）的程序集中转发到 BCL。
//
// 背景：
//   当一个 netstandard2.0 类库（AppLib）引用 NewLife.Core，
//   编译后 AppLib.dll 的元数据里，这些类型的程序集引用指向 "NewLife.Core"。
//   若该 AppLib 在 net10 应用（AppWeb）中运行，CLR 会加载 NewLife.Core (net10) 版本；
//   若 net10 版本中找不到这些类型，就会抛出 TypeLoadException。
//   此文件通过 TypeForwardedTo 将查找请求重定向到 BCL，彻底消除类型标识冲突。

// 异步接口（netstandard2.1 / netcoreapp3.0+ / net5+）
[assembly: TypeForwardedTo(typeof(IAsyncDisposable))]
[assembly: TypeForwardedTo(typeof(IAsyncEnumerable<>))]
[assembly: TypeForwardedTo(typeof(IAsyncEnumerator<>))]
[assembly: TypeForwardedTo(typeof(AsyncIteratorMethodBuilder))]
[assembly: TypeForwardedTo(typeof(AsyncIteratorStateMachineAttribute))]
[assembly: TypeForwardedTo(typeof(ConfiguredAsyncDisposable))]
[assembly: TypeForwardedTo(typeof(ConfiguredCancelableAsyncEnumerable<>))]
[assembly: TypeForwardedTo(typeof(EnumeratorCancellationAttribute))]
[assembly: TypeForwardedTo(typeof(TaskAsyncEnumerableExtensions))]
[assembly: TypeForwardedTo(typeof(ManualResetValueTaskSourceCore<>))]

// Nullable 注解属性（netstandard2.1 / netcoreapp3.0+）
[assembly: TypeForwardedTo(typeof(MaybeNullAttribute))]
[assembly: TypeForwardedTo(typeof(MaybeNullWhenAttribute))]
[assembly: TypeForwardedTo(typeof(NotNullAttribute))]
[assembly: TypeForwardedTo(typeof(NotNullIfNotNullAttribute))]
[assembly: TypeForwardedTo(typeof(NotNullWhenAttribute))]
#endif

#if NET5_0_OR_GREATER
// MemberNotNull / MemberNotNullWhen / IsExternalInit 从 .NET 5 起才进入 BCL，
// 对应 Stub 守卫为 NETFRAMEWORK || NETSTANDARD || NETCOREAPP3_1
[assembly: TypeForwardedTo(typeof(System.Diagnostics.CodeAnalysis.MemberNotNullAttribute))]
[assembly: TypeForwardedTo(typeof(System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute))]
[assembly: TypeForwardedTo(typeof(System.Runtime.CompilerServices.IsExternalInit))]
#endif
