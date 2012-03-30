using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// 有关程序集的常规信息通过以下
// 特性集控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("XUrlRewrite")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("新生命开发团队")]
[assembly: AssemblyProduct("XUrlRewrite")]
[assembly: AssemblyCopyright("版权所有 (C) 新生命开发团队 2009")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 使此程序集中的类型
// 对 COM 组件不可见。如果需要从 COM 访问此程序集中的类型，
// 则将该类型上的 ComVisible 特性设置为 true。
[assembly: ComVisible(false)]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("06604c95-4158-4aed-b651-2457d4cc8483")]

// 程序集的版本信息由下面四个值组成:
//
//      主版本
//      次版本 
//      内部版本号
//      修订号
//
// 可以指定所有这些值，也可以使用“内部版本号”和“修订号”的默认值，
// 方法是按如下所示使用“*”:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.1.*")]
[assembly: AssemblyFileVersion("1.1.2012.0330")]


/**
 * 
 * v1.1.2012.0330    增加重写跟踪日志,打开XUrlRewrite.Debug开关就可以在日志中看到
 * 
 * v1.1.2012.0215    配置文件增加可选的过滤参数,可以控制特定的请求不使用Url重写
 * 
 * v1.1.2011.1212    修正一处可能读取到错误的配置文件路径的问题
 * 
 * 
 */