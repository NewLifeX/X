#if !NETFRAMEWORK && !NETSTANDARD2_0
using System.Buffers;
using System.Runtime.CompilerServices;

// 将 Stub 文件（NETFRAMEWORK / NETSTANDARD2_0）中直接定义的 SequenceReader 相关类型，
// 在高版本框架（netstandard2.1 / netcoreapp3.1 / net5+）的程序集中转发到 BCL。
//
// 背景与 AsyncInterfacesForwards.cs 相同：
//   当一个 netstandard2.0 类库（AppLib）引用 NewLife.Core 并使用 SequenceReader<T>，
//   编译后 AppLib.dll 的类型引用指向 "NewLife.Core"；
//   若该 AppLib 在 net10 应用中运行，CLR 会加载 NewLife.Core (net10) 版本，
//   若 net10 版本中找不到这些类型，就会抛出 TypeLoadException。
//   此文件通过 TypeForwardedTo 将查找请求重定向到 BCL，彻底消除类型标识冲突。
//
// 说明：netstandard2.1 起 BCL 自带 SequenceReader<T> 与 SequenceReaderExtensions（有符号端序），
//   故仅 net45 / net461 / net462 / netstandard2.0 四个资产内直接定义（见 Stub/SequenceReader.cs）。
[assembly: TypeForwardedTo(typeof(SequenceReader<>))]
[assembly: TypeForwardedTo(typeof(SequenceReaderExtensions))]
#endif
