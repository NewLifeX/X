using System;
using System.Reflection;
using System.Runtime.InteropServices;

// 有关程序集的常规信息通过以下
// 特性集控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("新生命核心库")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("新生命开发团队")]
[assembly: AssemblyProduct("NewLife.Core")]
[assembly: AssemblyCopyright("\x00a92002-2011 新生命开发团队")]
[assembly: AssemblyTrademark("四叶草")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 使此程序集中的类型
// 对 COM 组件不可见。如果需要从 COM 访问此程序集中的类型，
// 则将该类型上的 ComVisible 特性设置为 true。
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]

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
[assembly: AssemblyVersion("3.4.*")]
[assembly: AssemblyFileVersion("3.4.2011.1123")]

/*
 * v3.4.2011.1123   修正Config.GetMutilConfig中没有正确返回默认值的错误
 *                  修改XTrace，输出日志时，是否线程池除了Y和N外，增加W表示是否Web线程
 * 
 * v3.3.2011.1110   增加网页下载类WebDownload
 * 
 * v3.3.2011.1107   字符串扩展和枚举扩展命名空间改为System
 *                  增加快速反射的扩展方法类ReflectionExtensions
 * 
 * v3.2.2011.1020   修正AssemblyX中一个可能导致无法从只加载程序集中获取类型的BUG
 *                  修正DisposeBase析构中调用XTrace.Debug可能配置系统已经释放的错误
 * 
 * v3.2.2011.1018   增加对象容器IObjectContainer，实现IoC的容器功能
 *                  增加服务容器ServiceContainer，作为对象容器的封装
 * 
 * v3.1.2011.1013   增加运行时类Runtime，支持识别是否控制台、是否64位操作系统
 *                  Runtime支持获取方法的JIT Native地址，支持同签名方法替换
 *                  增加方法体读取器MethodBodyReader，支持获取方法体的IL代码
 *                  增加模块构造函数的支持，默认调用Cctor类的Init和Finish方法
 *                  Runtime增加设置进程程序集大小，支持释放物理内存
 * 
 * v3.0.2011.0922   增加扩展方法特性，支持在vs2008和vs2010上编写.Net2.0时使用扩展方法
 *                  增加Enumerable，利用扩展方法扩展IEnumerable
 * 
 * v2.9.2011.0915   XTrace增加写当前线程MiniDump方法WriteMiniDump
 *                  XTrace增加写异常信息的方法WriteException和WriteExceptionWhenDebug
 *                  ThreadPoolX增加多个QueueUserWorkItem方法，作为系统ThreadPool.QueueUserWorkItem的封装，省去每次使用线程池都要做异常处理的麻烦，同时支持无参数委托
 * 
 * v2.8.2011.0901   修正TypeX.GetType中识别一维数组时的一个错误，如TypeX.GetType("Byte[]")会被错误识别为Byte[*]
 * 
 * v2.7.2011.0815   增加鸭子类型DuckTyping，但不对外公开，通过TypeX.ChangeType来使用！
 *                  增加ServiceProvider等服务模型
 * 
 * v2.6.2011.0725   修正TypeX中计算内嵌类型会重复计算的BUG
 * 
 * v2.5.2011.0701   增加可重入计时器TimerX
 * 
 * v2.4.2011.0625   重写日志模块，拆分出来TextFileLog，以便于多种日志用途
 * 
 * v2.3.2011.0623   增加证书类Certificate，用于创建自签名X509证书
 * 
 * v2.2.2011.0610   增加IO操作工具类IOHelper，支持数据流复制CopyTo，支持数据流压缩（Deflate压缩更小一点），支持单文件GZip压缩（WinRar可解压），支持多文件GZip压缩（自定义格式）
 *                  增加增强版Web客户端WebClientX，支持Cookie，默认增加若干请求头
 * 
 * v2.1.2011.0607   实现Http压缩模块CompressionModule，减少网络传输大小
 * 
 * v2.0.2011.0507   反序列化框架NewLife.Serialization命名空间，默认实现二进制、Xml和Json
 *                  轻量级IoC，实现类型解析器TypeResolver，配合接口变成来解决泛型基类所带来的不足
 *                  快速反射，AssemblyX增加一个FindAllPlugins(Type type, Boolean isLoadAssembly)方法
 *                  快速反射，PropertyInfoX的Create，在无法找到属性时递归处理基类，类似字段的处理方式
 *                  快速反射，修正TypeX的GetType方法没有使用isLoadAssembly参数的BUG
 * 
 * v1.9.2011.0423   增加跟踪数据流TraceStream，用于跟踪各种数据流操作
 * 
 * v1.8.2011.0412   修改获取硬件信息时如果获取某项发生异常时,只有在NewLife.Debug开关打开时才输出异常信息
 * 
 * v1.8.2011.0401   增加Json类
 * 
 * v1.7.2011.0330   增加泛型列表基类ListBase
 * 
 * v1.6.2011.0313   扩展字段缓存DictionaryCache，增加几个支持更多参数的GetItem重载
 * 
 * v1.6.2011.0311   优化TypeX.GetType，增加缓存功能
 * 
 * v1.5.2011.0303   修改二进制读写器和二进制访问器，在读取数据时允许指定目标数据类型
 * 
 * v1.5.2011.0222   修正读写锁的BUG，简化处理，任意读操作阻塞写操作，任意写操作阻塞所有其它读写操作
 * 
 * v1.5.2011.0212   TypeX增加GetType方法，用于根据类型名获取类型，可自动加载未加载程序集
 *                  二进制读写器BinaryReaderX和BinaryWriterX支持对Type的读写，以FullName的方式存在以节省空间
 *                  调整数据流总线模型，增加数据流客户端，用于向远端数据流处理器发送数据
 *                  增加远程调用框架Remoting（未完成），基于消息模型和快速反射模型设计
 * 
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