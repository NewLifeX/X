using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// 有关程序集的常规信息通过以下
// 特性集控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("序列化")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("新生命开发团队")]
[assembly: AssemblyProduct("NewLife.Serialization")]
[assembly: AssemblyCopyright("Copyright © 新生命开发团队 2010")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 使此程序集中的类型
// 对 COM 组件不可见。如果需要从 COM 访问此程序集中的类型，
// 则将该类型上的 ComVisible 特性设置为 true。
[assembly: ComVisible(false)]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("94e7a37b-091b-4ddd-bbf2-fdceb1f4b12c")]

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
[assembly: AssemblyFileVersion("1.2.2010.0813")]

/*
 * v1.2.2010.0813   修改自定义序列化支持，把特性支持改为接口支持，性能更佳
 * 
 * v1.1.2010.0812   增加序列化事件支持
 *                  增加循环引用的检测（目前为简单起见，暂时仅抛出异常）
 * 
 * v1.0.2010.0809   完成基本功能，暂时告一段落。后续增加：类型表、循环引用、序列化前后事件等功能！
 * 
 * v1.0.2010.0803   创建序列化库
 *
**/