using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// 有关程序集的常规信息通过以下
// 特性集控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("通用库")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("新生命开发团队")]
[assembly: AssemblyProduct("XCommon")]
[assembly: AssemblyCopyright("Copyright © 新生命开发团队 2010")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 使此程序集中的类型
// 对 COM 组件不可见。如果需要从 COM 访问此程序集中的类型，
// 则将该类型上的 ComVisible 特性设置为 true。
[assembly: ComVisible(false)]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("ca0ec708-68b3-407c-aaab-fe76d9e3a724")]

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
[assembly: AssemblyVersion("1.4.*")]
[assembly: AssemblyFileVersion("1.4.2010.0830")]

/*
 * v1.4.2010.0830   增加Http状态类HttpState，用于维护Http请求中的数据
 * 
 * v1.3.2010.0712   增加程序集版本类AssemblyVersion，用于获取程序集版本信息
 * 
 * v1.2.2010.0625   WebHelper中增加WriteScript、AlertAndClose和RequestInt
 * 
 * v1.1.2010.0621   WebHelper中增加CheckEmptyAndFocus
 * 
 * v1.0.2010.0604   创建通用库
 *
**/