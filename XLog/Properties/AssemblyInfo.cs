using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// 有关程序集的常规信息通过下列属性集
// 控制。更改这些属性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("日志组件")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("新生命开发团队")]
[assembly: AssemblyProduct("XLog")]
[assembly: AssemblyCopyright("版权所有 (C) NewLife 2009")]
[assembly: AssemblyTrademark("四叶草")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 使此程序集中的类型
// 对 COM 组件不可见。如果需要从 COM 访问此程序集中的类型，
// 则将该类型上的 ComVisible 属性设置为 true。
[assembly: ComVisible(false)]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("da0b1e2f-8b2e-46a1-b891-44b2da513612")]

// 程序集的版本信息由下面四个值组成:
//
//      主版本
//      次版本 
//      内部版本号
//      修订号
//
// 可以指定所有这些值，也可以使用“修订号”和“内部版本号”的默认值，
// 方法是按如下所示使用“*”:
[assembly: AssemblyVersion("2.4.*")]
[assembly: AssemblyFileVersion("2.4.2010.1103")]

/*
 * v2.4.2010.1103   增强日志事件参数类，使得输出日志时可以输出更多信息！
 * 
 * v2.3.2009.1201   修改日志格式为标准日志格式，逗号分隔
 * 
 * v2.2.2009.1022   修正调试开关Debug中的错误
 * 
 * v2.1.2009.0714   XTrace中增加WriteLine(String format, params Object[] args)方法，用于输出格式化的日志
*/