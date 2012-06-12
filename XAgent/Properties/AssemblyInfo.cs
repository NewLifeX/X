using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// 有关程序集的常规信息通过下列属性集
// 控制。更改这些属性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("新生命服务组件")]
[assembly: AssemblyDescription("支撑Windows服务应用的基础组件")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("新生命开发团队")]
[assembly: AssemblyProduct("XAgent")]
[assembly: AssemblyCopyright("\x00a92002-2012 新生命开发团队")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 使此程序集中的类型
// 对 COM 组件不可见。如果需要从 COM 访问此程序集中的类型，
// 则将该类型上的 ComVisible 属性设置为 true。
[assembly: ComVisible(false)]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("edcf20f9-b8cb-442d-863f-7534d97e9e98")]

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
[assembly: AssemblyVersion("2.3.*")]
[assembly: AssemblyFileVersion("2.3.2012.0419")]

/*
 * v2.3.2012.0419   增加看门狗功能，守护其它服务。
 * 
 * v2.2.2012.0209   完善对附加代理服务的支持，支持通过配置来设置附加代理服务
 * 
 * v2.1.2011.1019   项目类型由类库修改为控制台项目，不仅可供其它程序集引用，也可直接作为服务代理，承载各种实现IServer接口的服务
 * 
 * v2.0.2011.0720   可以通过设置任务的时间间隔小于0来关闭指定任务
 *                  可以通过配置文件指定服务名、显示名、描述等信息
 *                  可以通过XAgent.AttachServers设置附加代理服务，任何XAgent服务程序都可以附加代理实现了IServer接口的服务
 * 
 * v1.5.2010.0127   允许执行线程名
 * 
 * v1.4.2010.0114   生成批处理的方法，允许子类重载
 * 
 * v1.3.2009.1123   修改获取配置信息的代码，允许手工指定配置参数
 * 
 * v1.2.2009.1019   修改了取服务的方法，尽量避免异常
 *                  把StartWork等几个方法改为虚拟，允许重载
 * 
 * v1.1.2009.0731   启动时，生成批处理文件
 * 
 * v1.0.2009.0720   建立组件
*/