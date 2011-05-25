using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Web.UI;

// 有关程序集的常规信息通过下列属性集
// 控制。更改这些属性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("控件库")]
[assembly: AssemblyDescription("常用控件")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("新生命开发团队")]
[assembly: AssemblyProduct("XControl")]
[assembly: AssemblyCopyright("\x00a92002-2011 新生命开发团队")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 使此程序集中的类型
// 对 COM 组件不可见。如果需要从 COM 访问此程序集中的类型，
// 则将该类型上的 ComVisible 属性设置为 true。
[assembly: ComVisible(false)]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("fa826606-eee8-4b30-a41f-5e37420328a6")]

// 设置控件前缀
[assembly: TagPrefix("XControl", "XCL")]

// 特别要注意，这里得加上默认命名空间和目录名，因为vs2005编译的时候会给js文件加上这些东东的
[assembly: WebResource("XControl.TextBox.Validator.js", "application/x-javascript")]

// 程序集的版本信息由下面四个值组成:
//
//      主版本
//      次版本 
//      内部版本号
//      修订号
//
// 可以指定所有这些值，也可以使用“修订号”和“内部版本号”的默认值，
// 方法是按如下所示使用“*”:
[assembly: AssemblyVersion("1.9.*")]
[assembly: AssemblyFileVersion("1.9.2011.0525")]

/*
 * v1.9.2011.0525   修正数字 浮点数输入控件的一些细节问题,使在firefox下可正常运行
 * 
 * v1.9.2011.0224   +扩展控件基类，修改GetPropertyValue方法，当没有设定的ViewState时，从全局Appconfig中读取
 * 
 * v1.8.2011.0116   Add：GridView扩展控件增加分页模版、多选
 * 
 * v1.7.2010.1015   增加对话框控件
 * 
 * v1.6.2010.0830   修正时间日期选择控件回发后失效的问题
 * 
 * v1.5.2010.0706   增加选择输入控件ChooseButton，用于处理复杂选择
 * 
 * v1.4.2010.0702   重写DropDownList处理异常项的逻辑
 * 
 * v1.3.2010.0625   修正DropDownList中没有过滤重复异常项的问题
 * 
 * v1.2.2010.0621   增加DataPager分页控件，剥离自GridView
 * 
 * v1.1.2010.0604   增加DropDownList和CheckBoxList，修正关联参数ODS时两次绑定的BUG
 *                  增加DateTimePicker，强大的日期时间控件
 *                  
 * v1.0.2008.1212   创建控件库
 *
**/