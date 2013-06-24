using System;
using System.Reflection;
using System.Runtime.InteropServices;

// 有关程序集的常规信息通过以下
// 特性集控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("新生命核心库")]
[assembly: AssemblyDescription("各种基础功能")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("新生命开发团队")]
[assembly: AssemblyProduct("NewLife.Core")]
[assembly: AssemblyCopyright("\x00a92002-2013 新生命开发团队")]
[assembly: AssemblyTrademark("四叶草")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 使此程序集中的类型
// 对 COM 组件不可见。如果需要从 COM 访问此程序集中的类型，
// 则将该类型上的 ComVisible 特性设置为 true。
[assembly: ComVisible(false)]
//[assembly: CLSCompliant(true)]

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
[assembly: AssemblyVersion("3.9.*")]
[assembly: AssemblyFileVersion("3.9.2013.0624")]

/*
 * v3.9.2013.0624   增加Left/Right/Cut三组字符串截取扩展，包括二进制截取
 * 
 * v3.9.2013.0412   增加JavaScript脚本。提供Js的基本操作，同时也支持继承扩展
 * 
 * v3.8.2013.0308   XmlHelper增加Xml到字符串字典的互相转换
 * 
 * v3.8.2012.1205   新增系统配置类SysConfig
 * 
 * v3.8.2012.1204   XmlHelper增加注入注释的扩展AttachCommit
 *                  XmlConfig默认写入Description和DisplayName作为注释
 * 
 * v3.8.2012.1121   路径扩展助手增加路径绑定CombinePath
 *                  压缩类ZipFile增加读取延迟机制，在构造对象后并不会马上读取压缩包，以便于在读取前设置各种参数
 * 
 * v3.8.2012.1120   运行时Runtime增加支持OSName获取系统名称
 *                  日志文件头增加当前目录、CLR版本和系统名称等信息
 * 
 * v3.8.2012.1102   增加Xml助手类XmlHelper，支持Xml序列化，全部扩展方法
 * 
 * v3.8.2012.1023   增加Xml配置文件基类XmlConfig
 * 
 * v3.8.2012.0803   增加路径扩展助手PathHelper
 * 
 * v3.8.2012.0802   跟踪日志增加不换行的写日志方法Write
 *                  序列化跟踪日志支持输出到文本日志
 * 
 * v3.8.2012.0720   增加拼音获取类PinYin，用于从中文获取对应的拼音
 * 
 * v3.8.2012.0612   修正对象容器中注册时没有记录优先级的BUG
 * 
 * v3.8.2012.0525   XTrace增加UseWinForm方法，用于挂载处理WinForm未处理异常
 * 
 * v3.8.2012.0514   对象容器增加ResolveInstance，用于指定获取实例，而Resolve每次返回新实例
 * 
 * v3.8.2012.0505   提供一种方法，允许在日志输出被重定向后仍然向文件输出日志
 * 
 * v3.8.2012.0423   消息提供者内部支持消息的分片和组装
 *                  DictionaryCache增加清理过期缓存项功能，在缓存项过期后，如果再超过清理过期时间则被扫描任务清理
 * 
 * v3.8.2012.0410   增加ApiHook，用于挂钩托管函数
 * 
 * v3.8.2012.0401   增加TypeX.GetElementType方法，用于获取枚举类型的元素类型
 * 
 * v3.8.2012.0331   增加TypeX.GetMethod方法，用于反射获取类中的方法，适用于多态场合
 * 
 * v3.8.2012.0328   所有扩展方法辅助类，全部使用System命名空间
 * 
 * v3.7.2012.0307   简化消息提供者接口，消息模型相当不成熟
 *                  强化序列化框架，增加备份和恢复环境的机制
 *                  修正字典缓存中会导致带过期缓存永远过期的BUG
 * 
 * v3.7.2012.0227   增加通用插件接口IPlugin，插件管理类PluginManager
 *                  增加消息提供者接口IMessageProvider和消息消费者接口IMessageConsumer
 * 
 * v3.7.2012.0220   增加基于lock的安全栈
 * 
 * v3.7.2012.0209   完善消息模型
 * 
 * v3.7.2012.0118   重构消息模型，取消消息总线
 * 
 * v3.6.2012.0107   增加数组实现的安全栈SafeStack，改进对象池，性能有40%左右的提升，没有GC压力。
 * 
 * v3.6.2012.0102   修改对象容器，把名称定位对象改为Object标识定位对象，方便使用各种类型（特别是枚举）来进行注册和解析
 * 
 * v3.5.2011.1230   修正DictionaryCache中GetItem方法cacheDefault参数的严重错误
 * 
 * v3.5.2011.1220   序列化框架，FieldSizeAttribute支持样式的多层次引用字段
 *                  CurrentObject、CurrentMember移到接口中公开
 *                  读取对象时，如果目标实现了IAccessor接口而对象为空，则提前实例化对象
 *                  DisposeBase增加OnDisposed事件，在基类OnDispose之后触发
 * 
 * v3.5.2011.1218   增加压缩命名空间Compression，支持Zip格式
 *                  调整序列化框架（特别是二进制序列化）的多项功能，更方便使用
 *                  改进TraceStream，重载所有方法，方便拦截大部分操作
 * 
 * v3.4.2011.1209   ControlHelper增加FindEventHandler方法，用于查找Web控件的事件
 *                  EnumHelper增加GetDescriptions方法，用于构建枚举的可绑定字典
 * 
 * v3.4.2011.1207   修改XTrace，增加临时目录TempPath
 * 
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