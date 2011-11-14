using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// 有关程序集的常规信息通过以下
// 特性集控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("通用实体库")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("新生命开发团队")]
[assembly: AssemblyProduct("NewLife.CommonEntity")]
[assembly: AssemblyCopyright("\x00a92002-2011 新生命开发团队")]
[assembly: AssemblyTrademark("四叶草")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 使此程序集中的类型
// 对 COM 组件不可见。如果需要从 COM 访问此程序集中的类型，
// 则将该类型上的 ComVisible 特性设置为 true。
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: Dependency("XCode,", LoadHint.Always)]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("6affe219-9235-49dc-8032-a0dc9f10f887")]

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
[assembly: AssemblyVersion("2.6.*")]
[assembly: AssemblyFileVersion("2.6.2011.1114")]

/*
 * v2.6.2011.1114   精简ICommonManageProvider和IManagerPage，最大程度降低IManagerPage对IAdministator的依赖，目前可以做到不依赖
 *                  用户既可以不实现IAdministator，又可以享受IManagerPage的优势
 *                  Menu初始化扫描页面时，自动读取页面的Title
 * 
 * v2.6.2011.1111   增加接口IEntityForm，支持实体表单读写和验证保存
 * 
 * v2.5.2011.1018   增加管理提供者接口IManageProvider和通用实体类管理提供者接口ICommonManageProvider，用于支持管理平台解耦
 * 
 * v2.4.2011.0908   支持XCode v8.0，重新生成实体类
 * 
 * v2.3.2011.0628   管理员类和页面基类，增加根据权限名称来控制权限的方法
 *                  调整CommonEntity.EntityForm中对自定义控件的处理
 * 
 * v2.2.2011.0621   增加序列、设置、用户配置等三个实体类
 * 
 * v2.1.2011.0607   WebPageBase中增加ViewState压缩功能
 * 
 * v2.0.2011.0512   完善附件上传、附件下载、图片附件展示、图片缩略图等相关功能
 * 
 * v2.0.2011.0509   解决多个菜单具有相同权限名的问题，尝试返回当前页面所在菜单，如果无法确定，则返回第一个，并写日志。
 * 
 * v2.0.2011.0507   核心类库实现IoC，配合接口变成弥补泛型基类带来的不足，实体类升级支持
 * 
 * v1.6.2011.0313   更新EntityForm，屏蔽SetNotAllowNull中可能出现的异常
 * 
 * v1.6.2011.0303   调整实体类数据初始化架构，统一由XCode支持
 * 
 * v1.5.2011.0117   增加表单页基类EntityForm
 * 
 * v1.4.2011.0110   Edit:修改WebPageBase中的一些验证权限和输出执行时间的行为
 *                  Fixed:修改各个实体类中存在的BUG
 * 
 * v1.3.2010.1220   增加统计和附件实体类
 *                  修改角色和菜单实体，增加操作权限项，细分添加、修改、删除等权限
 *                  优化各个实体类，增加写日志的功能（通过接口和HttpState调用管理员的写日志功能）
 *                  Role中增加方法ClearRoleMenu，清理无效的权限项，由Role静态构造函数调用
 * 
 * v1.2.2010.1018   修改地区实体和菜单实体，增加实体树的操作
 * 
 * v1.1.2010.0909   再次抽象管理员实体和角色实体，各增加一层泛型基类
 *                  增加常用页面基类WebPageBase，同时增加一个指定了管理员类的泛型基类。支持输出页面执行时间和页面权限验证
 * 
 * v1.0.2010.0903   创建通用实体库
 *
**/