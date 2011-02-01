using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// 有关程序集的常规信息通过以下
// 特性集控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("新生命核心库")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("新生命开发团队")]
[assembly: AssemblyProduct("NewLife.Core")]
[assembly: AssemblyCopyright("Copyright © 新生命开发团队 2002~2011")]
[assembly: AssemblyTrademark("四叶草")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 使此程序集中的类型
// 对 COM 组件不可见。如果需要从 COM 访问此程序集中的类型，
// 则将该类型上的 ComVisible 特性设置为 true。
[assembly: ComVisible(false)]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("5536479f-1b04-410a-adf2-49df6e629060")]

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
[assembly: AssemblyFileVersion("1.4.2011.0113")]

/*
 * v1.4.2011.0113   快速反射中增加静态的（指定目标对象和成员名称即可）快速赋值取值和快速调用等方法
 *                  增加控件助手类ControlHelper
 * 
 * v1.3.2010.1215   修正FieldInfoX处理值类型时没有考虑拆箱的问题
 * 
 * v1.2.2010.1209   增强快速反射功能
 * 
 * v1.1.2010.1201   增加数据流总线模型和消息总线模型
 *                  增加原子读写锁ReadWriteLock
 * 
 * v1.0.2010.1115   创建核心库
 *                  合并日志组件XLog
 *                  合并多线程组件XThread
 *                  合并序列化组件NewLife.Serialization
*/