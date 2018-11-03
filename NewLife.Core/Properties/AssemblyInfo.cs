using System;
using System.Reflection;
using System.Runtime.InteropServices;

// 有关程序集的常规信息通过以下
// 特性集控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("组件核心库")]
[assembly: AssemblyDescription("日志、网络、RPC、序列化、缓存、Windows服务、多线程")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyProduct("NewLife.Core")]
[assembly: AssemblyCompany("新生命开发团队")]
[assembly: AssemblyCopyright("©2002-2018 新生命开发团队 https://github.com/NewLifeX/X")]
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
[assembly: AssemblyVersion("7.4.*")]
[assembly: AssemblyFileVersion("7.4.2018.1103")]

/*
 * v7.4.2018.1103   重构Redis，支持自动管道，提升吞吐率
 * 
 * v7.3.2018.0614   重构高性能资源池，减少GC压力，增加线程池，让异步任务得到平等竞争CPU的机会
 * 
 * v7.0.2018.0506   重构第四代网络库，改进RPC框架
 * 
 * v6.7.2018.0421   网络库废除发送队列SendQueue
 * 
 * v6.6.2018.0211   增加OAuth客户端服务端，支持QQ、百度、GitHub、淘宝等
 * 
 * v6.5.2017.1015   服务代理Agent增加任务调度器
 * 
 * v6.4.2017.0719   日志模块支持指定文件名格式，便于不同子系统输出日志文件到相同目录
 * 
 * v6.3.2017.0320   增加7z扩展压缩与解压缩
 * 
 * v6.2.2016.1230   增加定时调度器，支持多调度器来调度TimerX
 * 
 * v6.2.2016.1219   网络层增加数据包队列匹配功能，优先支持请求响应式协议
 * 
 * v6.1.2016.1208   网络层直接支持粘包处理和超大包收发，粘包接口IPacket默认实现HeaderLengthPacket支持基于长度的粘包拆分
 * 
 * v6.0.2016.0706   合并XAgent和NewLife.IP到核心库
 * 
 * v5.7.2016.0503   增加代码耗时统计TimeCost，用于统计关键点功能耗时情况，并输出日志
 *                  增加语音识别SpeechRecognition
 * 
 * v5.6.2016.0407   增加随机数生成器Rand
 * 
 * v5.5.2016.0205   网络库增加异步发送、收发统计
 * 
 * v5.4.2015.0511   增加网络日志提供者并作为Android版默认日志提供者，通过UDP广播把日志从网络发送出去
 * 
 * v5.3.2015.0327   网络库增加PacketStream，用于Tcp粘包拆包，测试通过
 *                  增加NetHelper.GetIPsWithCache，用于带缓存的获取本机IP，解决根据字符串IP获取物理IP时带来的BUG
 * 
 * v5.2.2015.0314   如果已经打开异步接收，还要使用同步接收，则同步Receive内部不再调用底层Socket，而是等待截走异步数据。
 * 
 * v5.2.2015.0307   增加Link，扩展WebClientX.GetLinks，用于从网页分析超链接以便于下载
 * 
 * v5.2.2015.0211   增加轻量级自动更新Upgrade，从网页自动查找更新包
 * 
 * v5.1.2015.0131   X组件核心库兼容MonoAndroid
 * 
 * v5.0.2014.1223   升级ITransport接口的事件实现方式，为上层应用开发提供强大有效的框架支持
 * 
 * v5.0.2014.1202   第三代网络库完成，回归APM模型，以简单为核心理念。网络基础测试通过，Tcp压力测试2w通过
 * 
 * v4.7.2014.1129   增加UdpClientX，基于APM模型，简单实用
 * 
 * v4.6.2014.1104   控件助手ControlHelper类增加文本控件扩展和文本控件着色
 * 
 * v4.6.2014.0928   IOHelper给Byte[]增加读写整数的扩展方法，特别支持大小端
 * 
 * v4.6.2014.0907   文本控件输出文本时支持退格字符，可实现时钟等固定位置刷新文本的效果
 *                  支持回车\r到行首，支持\7的Beep嘟嘟声
 * 
 * v4.6.2014.0731   二进制序列化增加List处理器
 * 
 * v4.6.2014.0722   DictionaryCache增加DelayLock，支持在锁外提前计算结果，避免每个key独占锁太长时间
 * 
 * v4.5.2014.0715   增加EncodingHelper，检测数据流字符编码
 * 
 * v4.4.2014.0704   增加窗口控件操作的ControlHelper，扩展若干常用Invoke方法
 * 
 * v4.3.2014.0703   WebHelper增加Cookie读写操作
 * 
 * v4.3.2014.0629   改进SerialTransport，支持串口断开检测，以及断开重连机制
 * 
 * v4.3.2014.0621   增加PE镜像解析类，识别x86/x64、FX2/FX4、托管/非托管
 * 
 * v4.2.2014.0504   增加Runtime.Mono，是否Mono环境
 * 
 * v4.2.2014.0401   增加并行字典、并行栈、并行队列
 * 
 * v4.1.2014.0307   修正XmlConfig中因逻辑错误导致频繁重新加载配置的错误
 * 
 * v4.1.2014.0219   增加UrlRewrite模块，允许集成扩展
 * 
 * v4.0.2014.0214   修正XmlHelper中因错误使用私有成员而导致编码为空的错误
 * 
 * v4.0.2014.0127   IOHelper为Stream增加Write和ToArray方法
 *                  IOHelper为Byte[]增加扩展方法ToHex，带有分隔符和分组功能
 * 
 * v4.0.2014.0111   IOHelper为Byte[]增加扩展方法Combine，用于合并两个数组
 *                  IOHelper为Byte[]增加扩展方法Reverse，用于字节数组倒序，主要是更换大小字节序
 * 
 * v4.0.2013.1214   简化对象容器IObjectContainer，不再支持构造函数依赖注入，上一次取消了属性依赖注入
 *                  增加扩展方法TryDispose，支持试图释放资源的各种场合
 * 
 * v4.0.2013.1211   文本日志TextFileLog增加指定日志文件路径的创建方法
 * 
 * v4.0.2013.1024   增加反射接口IReflect，统一快速反射
 *                  修改扩展方法命名空间NewLife.Linq=>System.Ling，以及HashSet的命名空间，保持FX2/FX4的兼容
 * 
 * v4.0.2013.1020   增加日志接口和日志等级，支持外部实现
 * 
 * v3.9.2013.1007   增加字节数组分割函数Split
 * 
 * v3.9.2013.1005   增加工具类Utility，采用对象容器架构，允许外部重载工具类的各种实现
 * 
 * v3.9.2013.0922   增加RSA/DSA/DES等加解密方法以及数字签名扩展
 * 
 * v3.9.2013.0907   StringHelper增加AppendExceptStart扩展，追加字符串，除了开头
 * 
 * v3.9.2013.0906   XmlHelper根据实体类模型给Xml树增加注释时，同时支持给实体类顶级加注释，支持数组和列表属性
 * 
 * v3.9.2013.0901   明确PathHelper.EnsureDirectory的用法，基础类库的用法应该有明确的用途，而不是通过某些小伎俩去让人猜测
 * 
 * v3.9.2013.0727   WinForm控件输出日志允许指定最大长度，超长清空
 * 
 * v3.9.2013.0712   使用WinForm控件输出日志
 * 
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
