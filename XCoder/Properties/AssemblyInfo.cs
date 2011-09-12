using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// 有关程序集的常规信息通过下列属性集
// 控制。更改这些属性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("代码生成器")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("新生命开发团队")]
[assembly: AssemblyProduct("代码生成器")]
[assembly: AssemblyCopyright("\x00a92002-2011 新生命开发团队")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 使此程序集中的类型
// 对 COM 组件不可见。如果需要从 COM 访问此程序集中的类型，
// 则将该类型上的 ComVisible 属性设置为 true。
[assembly: ComVisible(false)]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("bfff5d5b-dd0d-4464-b7b8-ec3786bc2d7b")]

// 程序集的版本信息由下面四个值组成:
//
//      主版本
//      次版本 
//      内部版本号
//      修订号
//
[assembly: AssemblyVersion("4.2.*")]
[assembly: AssemblyFileVersion("4.2.2011.0911")]

/*
 * v4.2.2011.0911   分离架构信息管理和模型管理，允许编辑模型
 * 
 * v4.2.2011.0909   支持内部模版，也支持外部文件模版，支持释放内部模版
 * 
 * v4.2.2011.0908   修正代码生成时更改输出目录后无效的BUG
 *                  完善各个模版
 *                  更改模版存放方式，直接从嵌入资源读取，不再释放
 *                  增加一个设置项Config.BaseClass，允许指定实体类的基类
 *                  增加数据架构管理功能
 * 
 * v4.1.2011.0906   修正生成时不能自动创建输出目录的错误
 *                  数据表选择支持显示中文描述
 * 
 * v4.1.2011.0902   修改自动更新逻辑，先下载，再询问是否更新
 *                  改善自动发现机制，自动发现本地SQLite和Access文件
 *                  增加自动在线翻译功能，调用Bing翻译
 * 
 * v4.0.2011.0829   升级XCode到v8
 *                  增加自动更新功能
 *                  删除代码视图
 *                  集成www.7765.com在线音乐
 * 
 * v3.4.2011.0329   修正自动尝试加载表时的错误
 *                  更新实体模版，以兼容最新版本的XCode v7.5
 * 
 * v3.4.2011.0316   升级模版引擎XTemplate，大大提高模版处理性能
 * 
 * v3.4.2011.0311   XCode升级到最新版本，增加数据库支持
 * 
 * v3.3.2011.0117   增加实体模版和页面模版，改变以前的生成模式（由一个表四次改为两次）
 *                  使用新的控件GridViewExtend和自定义表单EntityForm
 * 
 * v3.2.2010.1014   增加是否使用文件模版头的设置
 *                  更新模版引擎到v1.3，取消对模版的换行优化处理
 *                  增加第二套表单模版
 * 
 * v3.1.2010.1009   增加文件头
 *                  使用新的模版引擎
 * 
 * v3.0.2010.0928   更新模版引擎
 * 
 * v2.4.2010.0128   增加等号支持
 * 
 * v2.3.2009.1013   XCode数据架构升级
 * 
 * v2.2.2009.0714   修正待替换内容（如默认值）含有双引号而待替换标签前后又有双引号时，没有正确处理双引号的问题
 * 
 * v2.1.2009.0623   增加对Oracle的支持
 * 
 * v2.0.2009.0611   增加中文文件名配置，直接生成中文文件名的类
 *                  每次生成表时，重新从数据库取构架信息
 * 
 * v1.9.2009.0519   处理以类名开头的属性名后，再次纠正属性名
 * 
 * v1.8.2009.0330   属性名等于类名时，自动增加后缀0
 *                  属性名等于item时，自动增加后缀0
 *                  属性名以类名开头时，自动去除
 *                  代码查看框随着窗体最大化自动扩展
 *                  修正链接名错误使用当前链接名而不是填写的链接名的错误
 * 
 * v1.7.2009.0321   加载所有表时，按表名排序
 *
**/