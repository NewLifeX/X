#if !NETFRAMEWORK && !NETSTANDARD2_0
using System.Runtime.CompilerServices;

// 将 Stub 文件（NETFRAMEWORK / NETSTANDARD2_0）中直接定义的 Index / Range 类型，
// 在高版本框架（netstandard2.1 / netcoreapp3.1 / net5+）的程序集中转发到 BCL。
//
// 背景与 AsyncInterfacesForwards.cs 相同：
//   当一个 netstandard2.0 类库（AppLib）引用 NewLife.Core 并使用 System.Index / System.Range，
//   编译后 AppLib.dll 的类型引用指向 "NewLife.Core"；
//   若该 AppLib 在 net10 应用中运行，CLR 会加载 NewLife.Core (net10) 版本，
//   若 net10 版本中找不到这些类型，就会抛出 TypeLoadException。
//   此文件通过 TypeForwardedTo 将查找请求重定向到 BCL，彻底消除类型标识冲突。
//
// 说明：BCL 自 netstandard2.1（netcoreapp3.0）起自带 Index / Range，
//   故仅 net45 / net461 / net462 / netstandard2.0 四个资产内直接定义（见 Common/Index.cs、Common/Range.cs）。
[assembly: TypeForwardedTo(typeof(Index))]
[assembly: TypeForwardedTo(typeof(Range))]
#endif

#if NET8_0_OR_GREATER
// 同背景：Common/TimeProvider.cs 与 Threading/ITimer.cs 的垫片守卫为
//   #if NETFRAMEWORK || NETSTANDARD || NETCOREAPP3_1 || NET5_0 || NET6_0 || NET7_0
// BCL 自 net8.0 起自带 TimeProvider / ITimer，在本项目 TFM 集合（net45~net10.0，含 *-windows）下，
// 该守卫的补集恰为 NET8_0_OR_GREATER（与垫片守卫逐一对应，不多不少）。
[assembly: TypeForwardedTo(typeof(System.TimeProvider))]
[assembly: TypeForwardedTo(typeof(System.Threading.ITimer))]
#endif
