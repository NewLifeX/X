using System.Reflection;
using System.Runtime.InteropServices;

// 有关程序集的常规信息通过以下
// 特性集控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("NewLife.Mvc")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("新生命开发团队")]
[assembly: AssemblyProduct("NewLife.Mvc")]
[assembly: AssemblyCopyright("\x00a92002-2011 新生命开发团队")]
[assembly: AssemblyTrademark("四叶草")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 使此程序集中的类型
// 对 COM 组件不可见。如果需要从 COM 访问此程序集中的类型，
// 则将该类型上的 ComVisible 特性设置为 true。
[assembly: ComVisible(false)]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("8c61d66e-ef54-484d-93a5-27722b0c3fa8")]

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
[assembly: AssemblyVersion("1.2.*")]
[assembly: AssemblyFileVersion("1.2.2011.1109")]

/*
 * v1.2.2011.1109   实现Http请求路由到控制器
 *                  提供路由配置接口和模块路由配置接口
 *                  提供路由上下文对象以方便的访问当前路由的状态,以及从Url中获取信息
 *                  路由及执行控制器时将根据需要对浏览者隐藏详细异常信息
 *
 *
 * v1.0.2011.1101   创建组件。
 *                  支持Url路由到具体控制器，也支持Url路由到其它Url
 *                  提供控制器接口
 *                  提供模版引擎接口
 *                  提供默认简单控制器实现，通过模版引擎接口实现页面生成
 *
*/