using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

// 将 Stub 文件（低版本）中直接定义的裁剪/AOT 注解类型，在高版本框架的程序集中转发到 BCL。
// 背景与类型标识冲突说明见 AsyncInterfacesForwards.cs。
// 各类型的 BCL 引入版本：DAM / RUC / USM 从 net5 起，RDC 从 net7 起（与 Stub 守卫一一对应）。
#if NET5_0_OR_GREATER
[assembly: TypeForwardedTo(typeof(DynamicallyAccessedMembersAttribute))]
[assembly: TypeForwardedTo(typeof(DynamicallyAccessedMemberTypes))]
[assembly: TypeForwardedTo(typeof(RequiresUnreferencedCodeAttribute))]
[assembly: TypeForwardedTo(typeof(UnconditionalSuppressMessageAttribute))]
[assembly: TypeForwardedTo(typeof(DynamicDependencyAttribute))]
#endif

#if NET7_0_OR_GREATER
[assembly: TypeForwardedTo(typeof(RequiresDynamicCodeAttribute))]
#endif
