using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Web;
using XCode;

// 有关程序集的常规信息通过下列属性集
// 控制。更改这些属性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("数据中间件")]
[assembly: AssemblyDescription("数据中间件，MySQL、SQLite、SqlServer、Oracle")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyProduct("XCode")]
[assembly: AssemblyCompany("新生命开发团队")]
[assembly: AssemblyCopyright("©2002-2018 新生命开发团队")]
[assembly: AssemblyTrademark("四叶草")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 使此程序集中的类型
// 对 COM 组件不可见。如果需要从 COM 访问此程序集中的类型，
// 则将该类型上的 ComVisible 属性设置为 true。
[assembly: ComVisible(false)]
//[assembly: CLSCompliant(true)]
[assembly: Dependency("NewLife.Core", LoadHint.Always)]

[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: PreApplicationStartMethod(typeof(PreApplicationStartCode), "Start")]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("fd577d2c-f8aa-4cc8-a697-d7990c264af3")]

// 程序集的版本信息由下面四个值组成:
//
//      主版本
//      次版本 
//      内部版本号
//      修订号
//
// 可以指定所有这些值，也可以使用“修订号”和“内部版本号”的默认值，
// 方法是按如下所示使用“*”:
[assembly: AssemblyVersion("9.9.*")]
[assembly: AssemblyFileVersion("9.9.2018.1103")]

/*
 * XCode的重大改进
 * 
 * v9.0 高性能，分布式
 * v8.0 标准化开放的接口，增强定制能力
 * v7.0 增强数据库架构的支持，支持更多数据库
 * v6.0 增强的缓存和扩展属性支持
 * v5.0 弱类型支持
 * v4.0 实体集合和缓存
 * v3.0 增加ORM的各种细节支持
 * v2.0 数据架构功能，实体和数据结构双向映射
 * v1.2 使用泛型基类
 * v1.0 创建XCode
 * /

/*
 * v9.9.2018.1103   重构数据层查询，DbTable替代DataSet，为将来数据备份和传输打基础
 * 
 * v9.9.2018.0907   恢复使用数据层缓存，默认10秒，任意写入清空
 * 
 * v9.9.2018.0813   支持批量插入和更新，MySql/Oracle
 * 
 * v9.8.2018.0605   由DataReader直接映射实体列表，以支持netstandard的MySql和SQLite，且提升性能
 * 
 * v9.7.2018.0421   支持运行时修改DAL连接字符串
 * 
 * v9.6.2018.0326   重构权限体系，支持多角色
 * 
 * v9.6.2017.0808   重构正向工程，基于映射表查找数据库字段类型到实体类型的映射
 * 
 * v9.5.2017.0607   全面支持参数化添删改查
 * 
 * v9.4.2017.0207   废弃单对象缓存的自动保存
 *                  异步保存SaveAsync支持指定延迟时间
 * 
 * v9.3.2017.0124   废弃一级缓存，因其用处越来越不明显
 * 
 * v9.2.2016.1201   去掉扩展属性的实体类依赖，降低复杂度以及弱引用内存泄漏风险
 * 
 * v9.1.2016.0921   增加统计字段缓存FieldCache，用于按字段统计日志历史表，加快查询速度
 * 
 * v9.1.2016.0920   日志表增加链接编号LinkID，用于关联实体表对应记录，加强数据修改审计功能
 * 
 * v9.0.2016.0413   去掉模型解析器自动修正表名字段名的功能
 * 
 * v9.0.2016.0410   IDataTable增加ConnName属性，允许模型文件中每个表单独指定连接名
 * 
 * v9.0.2016.0405   增加实体队列和SaveAsync，支持延迟持久化，提升历史表和在线表的性能
 * 
 * v8.21.2016.0131  Oracle数据驱动更换为托管驱动
 * 
 * v8.20.2015.0717  数据会话增加Truncate用于清空数据表，标识归零，方便测试
 * 
 * v8.19.2015.0716  增加实体处理模块IEntityModule，支持拦截Create/Valid操作
 * 
 * v8.18.2015.0522  增加用户时间实体基类UserTimeEntityBase，便于扩展创建人和更新人以及时间等信息
 * 
 * v8.18.2015.0425  实体类支持动态增加字段Meta.Table.Add，享受读写数据以及反向工程的完整支持
 * 
 * v8.17.2015.0425  升级实体类SearchWhereByKeys方法，支持指定对哪些字段进行模糊查询
 * 
 * v8.17.2015.0408  集成用户权限管理架构
 *                  改进连接字符串设置，当某连接字符串不存在时，不再抛出异常，默认采用SQLite数据库，并在日志中输出
 * 
 * v8.16.2015.0329  直接查数据库得到的实体对象，自动加入正在使用的单对象缓存
 * 
 * v8.15.2015.0327  SQLite增加Backup，支持数据热备
 * 
 * v8.14.2015.0305  字段名集合Meta.FieldNames和实体索引this[name]等不再区分大小写比较，要求数据表不得使用同名但不同大小写的字段名
 * 
 * v8.13.2015.0107  修正部分实体类因没有主键导致删除数据错误的BUG
 * 
 * v8.13.2014.0720  如果页面设定有XCode_SQLList列表，则往列表写入SQL语句
 * 
 * v8.13.2014.0707  支持使用锁来控制SQLite并发
 * 
 * v8.12.2014.0703  修正EntityList在高并发遍历数据时存在版本冲突的问题
 * 
 * v8.12.2014.0617  Entity增加FindMin/FindMax
 *                  FieldItem增加GroupBy/Count/Sum/Min/Max等
 *                  SQLite的Select Count非常慢，数据大于阀值时，使用最大ID作为表记录数
 * 
 * v8.12.2014.0616  缓存模块增加Alone独占数据库以及实体缓存过期时间等配置，独占数据库是加大缓存权重以及过期时间
 *                  EntitySession增加HoldCache，指示在更新数据库不许清空缓存（CURD可同步更新实体缓存），而只能让其过期
 * 
 * v8.11.2014.0616  优化MSSQL的QueryCountFast，每次查询所有表行数，并缓存短时间
 * 
 * v8.11.2014.0614  增加分表分库专属方法Meta.ProcessWithSplit/Meta.CreateSplit
 * 
 * v8.11.2014.0612  重新整理远程数据库访问时切换系统库的逻辑
 * 
 * v8.10.2014.0412  增加DAL.CacheExpiration全局设置一级缓存
 * 
 * v8.10.2014.0311  为IList<IEntity>接口增加Page分页方法
 * 
 * v8.10.2014.0310  SQLite连接字符串支持auto_vacuum，指定是否自动收缩数据库
 * 
 * v8.10.2014.0228  修正DAL中在关闭一级缓存的情况下仍然使用一级缓存的问题
 * 
 * v8.10.2014.0221  修正CreateSession中的_sessions[tid] = session报null异常的问题，修改DbBase.ConnectionString引发ReleaseSession从而导致_sessions = null
 * 
 * v8.10.2014.0215  修正EntitySession.WaitForInitData中没有阻止多线程进入的问题
 * 
 * v8.10.2014.0107  增加Meta.CreateTrans/IEntityOperate.CreateTrans，方便使用using的事务
 * 
 * v8.10.2013.1214  IDataTable增加GetAllColumns方法，用于获取继承链上的所有字段
 * 
 * v8.10.2013.1213  增强实体类扩展EntityOperate，支持继承扩展，逐步替代Meta
 *                  填充数据完成时调用实体类OnLoad方法。默认设定标记_IsFromDatabase
 *                  实体树EntityTree设置独立EntityTreeSettting
 *                  IDataTable增加BaseType，方便具有继承特性的代码生成以及增强对实体类继承的支持
 * 
 * v8.9.2013.1212   增加EntityList.Page用于实体列表分页，同时增加配套的构造函数
 * 
 * v8.9.2013.1005   增加字段扩展FieldExtension，把字段的时间扩展操作分离出来并优化
 * 
 * v8.9.2013.0909   IEntity增加IEntityEntry的枚举接口，支持遍历实体的字段和值
 * 
 * v8.9.2013.0901   IDataTable和IDataColumn的DisplayName独立，支持数据表和数据字段中显示名和描述的分离
 * 
 * v8.9.2013.0815   SelectBuilder.Parse有缺陷，不能分析带有圆括号的SQL语句，因此给DbBase.PageSplit带来风险，各数据库全部重载该方法以规避该风险
 * 
 * v8.9.2013.0804   模型保存IDataIndex时，不用保存默认的Name
 * 
 * v8.9.2013.0803   修正SQLite无法加载SQLite.Interop.dll的BUG，根据进程版本，设定x86或者x64为DLL目录
 * 
 * v8.9.2013.0708   修改数据库驱动下载逻辑，增加Fx40支持
 * 
 * v8.9.2013.0622   默认服务地址修改为NewLifeX.com
 * 
 * v8.9.2013.0326   FieldItem增加IsTrue和IsFalse，实现True/False/Null的分组构造条件
 * 
 * v8.9.2013.0321   SearchWhereByKey和SearchWhereByKeys直接返回WhereExpression
 * 
 * v8.9.2013.0320   增加TraceSQLTime，跟踪SQL执行时间，大于该阀值将输出日志，默认0毫秒不跟踪。
 * 
 * v8.9.2013.0310   修正动态代码中字段引用不正确的BUG
 *                  IDbSession.Rollback增加是否忽略异常的参数，默认忽略
 * 
 * v8.9.2013.0307   为IDataTable/IDataColumn增加扩展属性字典Properties，支持表和字段使用扩展属性
 * 
 * v8.9.2013.0306   为Oracle驱动增加配置项XCode.Oracle.IgnoreCase，是否忽略大小写，如果不忽略则在表名字段名外面加上双引号，默认true
 * 
 * v8.9.2012.1225   模型导入导出进行默认值精简，主要使用名称类型和长度
 * 
 * v8.9.2012.1220   实体树基类EntityTree增加父级节点名ParentNodeName
 *                  实体树基类EntityTree支持最大深度MaxDepth限制
 * 
 * v8.9.2012.1216   整理EntityAssembly代码，增加若干事件，允许控制动态实体类的基类等信息
 * 
 * v8.9.2012.1122   Oracle驱动增加对环境变量和注册表的检测
 * 
 * v8.9.2012.1114   实体树EntityTree增加BigSort开关，用于指定排序时是否较大数字排前面
 * 
 * v8.9.2012.1113   特殊处理实体列表查找中的整数类型，避免出现相同值不同整型而导致结果不同
 * 
 * v8.9.2012.1111   数据模型调整，Name=>TableName/ColumnName，Alias=>Name
 * 
 * v8.8.2012.0828   DAL中，把反向工程检查延迟到第一次数据库操作之前一刻
 *                  FieldItem的等于不等于大于小于等操作，支持两个操作数都是本表字段的情况
 * 
 * v8.8.2012.0821   无条件FindCount时，如果总记录数超过一万，为了提高性能，返回快速查找且带有缓存的总记录数
 * 
 * v8.8.2012.0803   下载数据库驱动时，增加本地缓存（系统盘X目录），有效期一个月
 * 
 * v8.8.2012.0727   如果实体来自数据库，在给数据属性赋相同值时，不改变脏数据，其它情况均改变脏数据
 * 
 * v8.8.2012.0722   增加模型字段排序特性ModelSortModeAttribute，默认指定基类数据字段优先，影响生成数据表字段顺序
 * 
 * v8.8.2012.0718   增加实体事务区域EntityTransaction
 * 
 * v8.8.2012.0715   修正反向工程中因为跨数据库处理默认值而导致字符串默认值出错的BUG
 *                  修正MSSQL中采用CONVERT([datetime],'1753-1-1',(0))作为时间最小值而无法实现跨数据库处理的BUG
 *                  如果索引全部就是主键，无需创建索引
 *                  SQLite中不同表的索引名也不能相同
 * 
 * v8.8.2012.0625   增加字段累加功能，支持点击数累加和货币累加等，如Update xxx Set Price=Price+100
 *                  累加字段用法：实体类静态构造函数中通过AdditionalFields指定需要累加的字段，其它操作不变
 *                  如果索引的唯一字段是主键，则反向工程时无需建立索引
 * 
 * v8.7.2012.0620   修正Entity<>.CopyFrom中复制扩展属性时，设置脏数据出错的BUG，而8.7的实体缓存OnUpdate刚好用到
 *                  XCode对于默认排序的规则：自增主键降序，其它情况默认
 * 
 * v8.7.2012.0614   IEntity接口增加CloneEntity和CopyFrom方法，增强对实体克隆的支持
 *                  在事务保护中，为了避免性能损耗，不会实时更新实体缓存，直到提交或回滚事务，插入或删除实体时直接操作实体缓存
 *                  DbSession.GetSchema缓存10秒，既提升了正向反向工程的性能，又避免了修改表结构后无法及时得到更新
 *                  Entity.Exist选择采用实体缓存进行验证，提高性能
 *                  修正EntityList.ToDataTable绑定DataGridView时无法更新数据的BUG，因为脏数据
 * 
 * v8.7.2012.0607   修正TableItem中连接名映射ConnMaps可能因为多线程冲突而导致小几率失败的问题
 *                  插入数据时，如果没有自增字段，则无视允许插入自增字段的设置
 *                  为了最大程度保证兼容性，反向时所有数据库的Decimal和DateTime类型不指定精度，均采用数据库默认值
 *                  修正EntityTransform中一个使用事务保护的错误
 *                  修正Entity.Meta.ClearCountCache处理记录数缓存的错误，增加支持切换连接或表名时清除记录数缓存
 * 
 * v8.7.2012.0605   实体类增加FindSQL方法，获取查询SQL，主要用于构造子查询
 * 
 * v8.6.2012.0604   修正XCode中会导致IEntityOperate.FindAllWithCache死循环的BUG
 *                  为SQL格式化数值时，如果字符串是Empty，将不再格式化为null
 * 
 * v8.6.2012.0529   重构反向工程设置项的设计，以参数的方式接受反向设置，摆脱受配置项的限制
 * 
 * v8.6.2012.0526   由 @Goon(12600112) 测试并完善SqlCe的正向反向工程
 * 
 * v8.6.2012.0525   实体类把四参数的FindAll和FindCount标为已过期，IEntityOperate中直接注释
 * 
 * v8.6.2012.0523   数据迁移EntityTransform引用局部迁移，允许某些表只迁移开头或结尾的指定数量的数据
 * 
 * v8.6.2012.0521   改进EntityList，增加ToList方法，返回List<T>形式的列表，方便使用Linq
 * 
 * v8.6.2012.0515   增加模型解析接口，用于格式化数据模型表名字段名等，形成更优的别名（类名和属性名）
 * 
 * v8.5.2012.0508   增加数据转换功能，支持在不同链接间互导数据
 * 
 * v8.5.2012.0507   修改IEntityPersistence接口，增加两个事件用于是否自动设置Guid和是否允许向自增列插入值
 *                  默认不再自动设置Guid
 *                  可通过设置允许向自增列插入值，可用于数据复制
 * 
 * v8.5.2012.0422   修正更改实体类的连接名和表名时没有正确处理架构检查，导致性能低下的BUG
 * 
 * v8.5.2012.0401   IList<IEntity>接口增加ToDataTable等方法
 * 
 * v8.5.2012.0323   Insert和Update时，大字段使用参数传递，至此，XCode完整支持所有数据类型。感谢@老徐（gregorius 279504479）
 *                  修正对默认值的处理的错误，该错误导致创建表时无法使用字符串默认值
 *                  
 * v8.4.2012.0322   Entity.Save中，对于非自增主键，如果唯一主键不为空，应该通过后面判断，而不是直接Update
 * 
 * v8.4.2012.0320   FieldItem增加IsNullOrEmpty和NotIsNullOrEmpty方法
 * 
 * v8.4.2012.0316   感谢@晴天（412684802）和@老徐（gregorius 279504479），这里的最小和开始必须是0，插入的时候有++i的效果，才会得到从1开始的编号
 * 
 * v8.4.2012.0309   IDatabase接口增加表示Guid获取函数的属性NewGuid
 *                  数据层增加对Guid默认值的支持，用于DDL操作
 *                  插入数据时，针对没有赋值的Guid字段或设置了Guid默认值的字符串字段，默认设置一个Guid，由EntityPersistence实现
 * 
 * v8.4.2012.0224   修正DbSession.InsertAndGetIdentity永远返回0的错误，该错误于20110224产生，影响Oracle和Firebird，感谢 @老徐（279504479）
 *                  针对性优化元数据架构，优化正向工程、反向工程的性能，获取单表模型信息时，仅获取该表架构信息，对Oracle反向工程性能提升较大
 * 
 * v8.4.2012.0221   增加排序表达式OrderExpression，字段FieldItem增加排序的Asc和Desc
 * 
 * v8.4.2012.0218   改进XCode.Code，增加单独的调试开关XCode.Code.Debug
 * 
 * v8.4.2012.0216   修正SelectBuilder中一个导致Limit式SQL出错的BUG
 * 
 * v8.4.2012.0215   修正EntityTree.FindByPath中的一个BUG，查找多级路径时，无法把keys参数传入内部，导致有些时候查找失败
 * 
 * v8.4.2012.0113   修正DbFactory中获取默认提供者时的BUG
 *                  DbMetaData中，无法识别字段类型时，输出日志
 *                  修正没有设置builder.IsInt导致分页没有选择最佳的MaxMin的BUG，感谢 @RICH(20371423)
 * 
 * v8.4.2011.1223   数据层支持存储过程及参数化查询
 * 
 * v8.3.2011.1222   修正SQLite会对内存数据库调用创建数据库的BUG
 *                  修改EntityAssembly所需要的参数，支持传入IDataTable集合来生成实体
 * 
 * v8.3.2011.1208   EntityList增加排序方法Up/Down，支持调整某个实体对象在列表中的排序
 *                  IEntity增加EqualTo，用于判断两个实体对象在主键上是否相等
 *                  IEntity增加SetNullKey，用于把实体对象的主键数据设置为空
 *                  IDatabase增加StringConcat方法，表示数据库中连接两个字符串
 *                  修正反向工程中ReBuildTable的BUG，SQLite中字符串连接使用||而不是+
 * 
 * v8.3.2011.1207   修正MSPageSplit.DoubleTop分页最后一页可能有错误的问题，每次计算分页都查询总记录数判断处理
 *                  修正反向工程中SQLite无法删除字段的BUG
 * 
 * v8.3.2011.1204   实体访问器接口增加OnError事件，允许外部控制异常，默认向外抛出。
 *                  Entity<>.Delete删除实体时，如果实体有且仅有主键有脏数据，则先查询一次再调用OnDelete删除，避免扩展删除失败
 * 
 * v8.3.2011.1201   不再支持异步初始化数据InitData，因为它实在容易出问题。如若需要异步初始化，可在InitData中自己实现。
 * 
 * v8.3.2011.1126   HttpEntityAccessor检查数据类型是否满足目标类型，如果不满足则跳过，以免内部赋值异常导致程序处理出错
 * 
 * v8.3.2011.1124   Entity增加SaveWithoutValid方法，不需要验证的保存，不执行Valid，一般用于快速导入数据
 * 
 * v8.3.2011.1123   修正反向工程检查实体类表架构时，有些实体类所在程序集尚未加载，导致未能检查的问题，改为首次使用时检查
 *                  针对缓存、数据初始化、检查表架构，添加各种调试日志，方便检查调试程序
 *                  修正ModelHelper.GetIndex的错误，该错误导致了反向工程频繁的删除并创建索引
 * 
 * v8.3.2011.1122   复审一级缓存、实体缓存、单对象缓存代码，增加关键点写日志功能，方便调试可能因缓存而引起的各种问题
 * 
 * v8.3.2011.1121   EntityTree增加EnableCaching属性，指定是否缓存Childs、AllChilds、Parent等，默认为true
 * 
 * v8.3.2011.1120   增加实体依赖类EntityDependency。用于HttpRuntime.Cache，一旦指定的实体类数据改变，马上让缓存过期。
 * 
 * v8.3.2011.1118   修正SelectBuilder.SelectCount方法中的BUG，当条件字句包含GroupBy时，处理不正确。该BUG由@行走江湖（534163320）发现
 *                  新增字段信息类Field，继承自FieldItem，仅仅为了重载等号运算符，用于实体数据类内置的字段信息类
 *                  WhereExpression增加左右小括号支持，And运算自动检测左右字句并加上小括号（保守做法，只要有Or就加）
 * 
 * v8.3.2011.1117   修改EntityTree，增加FullPath、FullParentPath、TreeNodeName等属性
 *                  重构IEntityTree接口
 * 
 * v8.3.2011.1115   IDataTable和IDataColumn增加只读属性DisplayName，优先返回Description，然后才是Name
 *                  修正MSPageSplit中关于RowNumber和DoubleTop分页的错误
 * 
 * v8.3.2011.1114   修改Entity<>.Meta.CheckModel，加上锁，让多个线程同时访问同一个实体表的操作，全部卡在检查模型之后，避免未创建实体表而报错
 * 
 * v8.3.2011.1111   如果启用了事务保护，GetSchema要新开一个连接，否则MSSQL里面报错，SQLite不报错，其它数据库未测试
 * 
 * v8.3.2011.1109   增加实体持久化接口IEntityPersistence，实体类中的Insert/Update/Delete由该接口实现，可通过该接口实现参数化DbCommand
 *                  IEntityOperate增加Execute等数据库操作，相比于DAL的数据库操作，这里的操作会触发实体类实体缓存更新
 *                  实体类中，验证数据是否已存在时，忽略自增
 * 
 * v8.2.2011.1108   IEntityOperate增加缓存查询
 * 
 * v8.2.2011.1107   给IEntityOperate.Create和Entity.CreateInstance加上默认参数forEdit，表示是否为了编辑(FindByKeyForEdit)而创建，默认为false
 *                  实体类可重写Entity.CreateInstance，根据参数forEdit，对为了在界面上新增而创建的实体进行初始化
 * 
 * v8.2.2011.1103   重构MS分页算法，分为MaxMin分页（数字主键优先选择）、NotIn分页（基本废弃）、双Top分页（替代NotIn）、RowNumber分页（高版本选择）
 * 
 * v8.2.2011.1101   SqlServer增加连接字符串设置DataPath，用于指定反向工程创建数据库时所使用的数据目录
 *                  Oracle增加连接字符串设置DllPath，用于指定OCI目录，同时基于该目录自动计算ORACLE_HOME目录
 *                  Oracle增加设置项XCode.Oracle.IsUseOwner，指定正向工程时是否使用Owner约束所查询的表
 *                  支持连接字符串编码加密，避免明文，明文=>UTF8字节=>Base64，可调用DAL.EncodeConnStr实现
 *                  如果需要高级加密，则不要在配置文件中设置连接字符串，而改为编码通过DAL.AddConnStr添加
 *                  **使用对象容器后，实际项目稳定运行半个月，版本可升级到8.2
 * 
 * v8.1.2011.1020   实体缓存EntityCache及接口增加一个使用委托进行查询的FindAll
 * 
 * v8.1.2011.1019   修正注释反向工程设置项后，单表使用的反向工程仍然检查的问题
 *                  FieldItem增加NotIn支持
 * 
 * v8.1.2011.1018   改善实体基类Entity，对于FindAll和FindCount，如果查询的条件是单一主键或者自增，并且为空，则取消查询
 * 
 * v8.1.2011.1017   改善Oracle支持上的一些问题
 *                  完善对象容器的使用
 * 
 * v8.1.2011.1016   丢失主键的问题经常发生，现在修改DefaultCondition，如果没有主键，直接抛出异常
 * 
 * v8.1.2011.1014   使用对象容器重构XCode中的各个接口使用
 * 
 * v8.1.2011.1013   修正Entity和EntityList中，因为批量查询不再返回null而带来的问题，特别是Exist
 *                  SQLite建表语句，对于字符串类型，创建忽略大小写的字段
 * 
 * v8.1.2011.1012   修正给非主键的自增字段建立唯一索引中的编码错误
 * 
 * v8.1.2011.1010   FieldItem增加对In操作符的支持
 * 
 * v8.1.2011.1008   IEntityOperate中返回IList<IEntity>改为返回IList<IEntity>，直接返回原始的实体类列表
 * 
 * v8.0.2011.0929   修正IDataTable.Fix中对一对一关系处理的不足
 *                  修改Entity，根据唯一索引查询单对象时不加分页和排序，这使得在SQLite上特别是MySql上有性能提升
 * 
 * v8.0.2011.0917   给索引和关系模型增加Computed属性，标识是计算出来还是数据库内置的
 * 
 * v8.0.2011.0912   重构反向工程，废除DatabaseSchema类（旧版本的反向工程核心），将其功能合理分配到各个地方，兼容各种数据库在反向时的差异
 *                  增加ModelCheckModeAttribute特性，可用于控制实体类是在初始化连接时还是第一次使用该实体类的数据表时检查模型（反向工程）
 * 
 * v8.0.2011.0911   完善模型的模型特性，便于代码生成器中的模型管理
 * 
 * v8.0.2011.0910   数据层使用全新的接口IDataTable、IDataColumn、IDataIndex、IDataRelation
 *                  尝试使用服务提供者，外部可替代内部各接口实现
 *                  数据层/实体层、正向/反向工程 增加索引支持，在部署到生成环境时同步创建索引，保证系统最佳性能
 *                  丰富实体类的添删改查，增加多种常见页面用法
 *                  重点：把数据库=>实体类+页面的用法，改为模型+模版=>数据库+实体类+页面的使用方式，扩大数据架构的表现能力，全力支持魔方平台
 * 
 * v7.16.2011.0803  修正MSSQL中创建数据库指定文件位置时出错的BUG
 *                  增加设置项SQLPath，允许把SQL日志写入到单独的SQL日志中
 * 
 * v7.15.2011.0725  修正EntityFactory中创建实体操作者可能出现的BUG，解决非泛型继承的问题，如Admin=>Administrator=>Administrator<Administrator>
 * 
 * v7.14.2011.0723  增加实体缓存接口IEntityCache和单对象缓存接口ISingleEntityCache
 *                  IEntityOperate增加缓存等操作支持
 * 
 * v7.13.2011.0622  优化SQLite，如果外部不指定缓存大小等参数，则自动使用最高性能的参数
 * 
 * v7.12.2011.0614  SQLite增加对内存数据库的支持，数据源设置为:memory:或者空，即表示使用内存数据库
 * 
 * v7.11.2011.0613  修正v7.8.2011.0510中修改时遗留下来的问题，一个是SQLite.DropColumnSQL中把两个参数写反了，一个是DatabaseSchema中，如果先增加字段后删除字段会出错
 * 
 * v7.11.2011.0612  修正v7.10.2011.0608中修改时遗留下的问题，完整实现最大最小值分页，同时发现TopNotIn分页和MaxMin分页无法完美的支持GroupBy查询分页，只能查到第一页
 * 
 * v7.11.2011.0611  修正v7.10.2011.0608中修改时遗留下的问题，获取列表时默认使用自增字段降序，根据主键获取单记录的方法绕过此处，免受影响
 *                  非常重要：修改EntityBase，给实体类数据属性赋值时，如果新旧值相等，不影响脏数据，剐需要影响脏数据，请使用SetItem
 * 
 * v7.11.2011.0610  修改DbBase，对于需要外部提供者的数据库类型，在没有提供者程序集时，自动从网络上下载，等待3秒
 * 
 * v7.10.2011.0608  Entity中自增或者主键查询，记录集肯定是唯一的，不需要指定记录数和排序
 * 
 * v7.9.2011.0603   实体类增加三个根据实体缓存查找数据的方法，方便ObjectDataSource绑定
 * 
 * v7.9.2011.0602   修正EntityTree中的排序错误，增加升降排序方法，支持同级升降排序
 * 
 * v7.9.2011.0529   实体类添删改拆分两部分，OnInsert/OnUpdate/Delete作为操作数据的真实操作，Insert/Update/Delete作为调用者，配合以数据验证和事务保护
 *                  增加数据验证Valid，实体类可以重载，以实现Insert/Update前验证数据，将来可能根据数据字段特性进行自动化验证。
 *                  增加数据存在判断Exist，开发者可根据需要调用，建议用于业务主键的存在性判断。CheckExist可以在判断后抛出异常。
 * 
 * v7.9.2011.0526   重构XCode实体层元数据部分，使用公开的TableItem替代保护的XCodeConfig，配合FieldItem形成完成的实体元数据结构
 *                  使用专用类实现IEntityOperator接口，避免原来臃肿的结构
 * 
 * v7.8.2011.0512   更新SelectBuilder，更新QueryCount相关代码，保证生成最精简的QueryCount查询语句，对于MySql而言，避开子查询，有巨大的性能优势
 * 
 * v7.8.2011.0510   增强SQLite的反向工程能力，SQLite不支持修改字段和删除字段，但是可以通过新建表然后复制数据的方式替代，并且解决了新增不允许空且又没有默认值的字段时出错的问题
 * 
 * v7.7.2011.0429   修正Access创建表时不应该同时操作默认值的错误
 * 
 * v7.6.2011.0420   修正XCode中反向工程模块判断是否普通实体类的错误
 * 
 * v7.6.2011.0409   重新启用实体类的ToData，允许实体数据转化为DataRow
 *                  EntityList增加ToDataTable和ToDataSet等方法，允许实体集合转为数据集，并通过事件把数据集的添删改操作委托到实体操作
 *                  一级缓存默认设置改为请求级缓存，避免在Web项目中因不正当使用查询而带来的性能损耗
 * 
 * v7.5.2011.0403   修正XField中Xml序列化的问题
 * 
 * v7.5.2011.0401   增加Json支持
 * 
 * v7.4.2011.0331   实体类Insert后清空脏数据，避免连续两次Save变成一次Insert和一次Update
 *                  修正实现组件模型接口中的一些问题，测试通过，基本上满足WinForm开发要求
 * 
 * v7.4.2011.0330   修正3月23号更新SqlServer时带来的另一个错误——无法创建自增字段；同时增加了把原字段设置为自增字段的功能，先删后加！
 * 
 * v7.4.2011.0329   修正动态生成代码时属性名与类型重名导致编译出错的问题
 *                  XTable和XField实现克隆接口
 *                  EntityBase实现INotifyPropertyChanging, INotifyPropertyChanged, ICustomTypeDescriptor, IEditableObject接口
 *                  EntityList实现ITypedList, IBindingList, IBindingListView, ICancelAddNew接口
 * 
 * v7.4.2011.0325   Entity所有基类标识为可序列化
 * 
 * v7.4.2011.0323   修改反向工程，当多个实体类使用同一数据表时，优先使用非通用实体类
 *                  修改SqlServer提供者，支持修改字段的主键属性
 * 
 * v7.4.2011.0321   EntityList增加Join方法，串联指定成员，方便由实体集合构造用于查询的子字符串
 *                  进行异步数据初始化时，如果内部遇到其它数据初始化，则在当前线程进行处理，保证数据初始化的同步进行，保证某些业务数据的初始化安装预定顺序进行。
 * 
 * v7.4.2011.0318   实体缓存增加是否允许空的设置，如果不允许空则即使缓存未过期也进行数据刷新
 *                  稍微优化实体缓存和单对象缓存，提升性能
 *                  计划加强各个缓存，特别是单对象缓存，利用维护线程删除过期缓存项，也可能借助System.Web.Caching.Cache
 * 
 * v7.3.2011.0314   修正实体基类静态构造函数的死锁问题，感谢邱鹏发现该问题！
 * 
 * v7.3.2011.0313   扩展EntityTree，增加Contains、ChildKeys、ChildKeyString、AllChildKeys、AllChildKeyString
 *                  修改EntityBase，GetExtend方法增加是否缓存默认值的选项，使用者可以选择在取不到数据时是否缓存代表空的默认值
 *                  修改EntityTree的Root属性，不缓存空值，大多数情况下，树形结构的数据都不应该为空
 * 
 * v7.3.2011.0311   修正非MS数据库的分页错误
 * 
 * v7.3.2011.0310   修正判断保留字时使用泛型List导致性能低下的BUG，改为Dictionary
 * 
 * v7.3.2011.0307   增加对Firebird和PostgreSQL的支持，未完全测试通过
 * 
 * v7.2.2011.0303   实体操作接口增加InitData方法，实体类可以重载，用于在第一次使用数据库时初始化数据
 * 
 * v7.1.2011.0228   MSSQL中使用架构信息判断数据库和数据表是否存在，避免某些情况下没有权限使用系统视图而出错
 *                  IDatabase接口增加保留字和FormatName方法，只有关键字才进行格式化
 * 
 * v7.1.2011.0224   调整方法InsertAndGetIdentity
 *                  SQLite中去掉读写锁，改为写入时判断数据库是否锁定，如果已锁定则重试
 * 
 * v7.1.2011.0223   调整Oracle的数据架构功能
 *                  Oracle增加快速查找表记录数方法
 *                  XField调整，规范化长度、字节数、精度和位数
 * 
 * v7.1.2011.0222   SQLite使用完整读写锁，避免读取时有写入操作然后报文件锁定
 *                  SQLite写入操作允许重试两次，以解决高并发时文件锁定的小概率事件
 *                  修改数据库架构，在获取数据库是否存在出现异常时，默认数据库已存在，因为一般来说都是没有管理员权限造成的错误，并且大多数时候数据库都是存在的
 *                  修改DAL的构造函数，检查数据库架构的异常不应该影响DAL的正常使用
 * 
 * v7.1.2011.0215   热心网友 Hannibal 在处理日文网站时发现插入的日文为乱码（MSSQL），这里加上N前缀，同时给时间日期加上ts前缀
 *                  SQLite数据库处理字节数组时，加上X前缀
 *                  把实体类中的SQL方法，设为共有，便于外部获取SQL语句
 * 
 * v7.1.2011.0212   增加网络数据库提供者Network，把各种操作直接路由给远端
 *                  增加分布式数据库提供者Distributed，同时读写多个数据库
 *                  设计方案最佳实践：
 *                  1，使用MySql自身的集群，一主多从，XCode配置使用分布式提供者，更新写入主库，从各从库读取数据，实现负载均衡
 *                  2，使用网络数据库提供者实现路由中转，实现故障转移
 * 
 * v7.0.2011.0201   重写数据访问层，便于功能扩展
 *                  重写数据架构（反向工程），完善SQLite和MySql的反向工程支持
 * 
 * v6.6.2010.1230   修改XCode类型映射模型，统一使用Schema信息，不再人为指定类型映射，全部交由数据库提供者处理
 *                  由C#类型反向到数据类型的映射尚未完成
 * 
 * v6.5.2010.1223   修正SQLite已知的一些问题，查找dll文件路径不正确，执行插入语句不正确
 *                  IEntity增加CopyFrom方法，用于从指定实体对象复制成员数据
 *                  增加对二进制字段的支持，表现为Byte[]
 * 
 * v6.4.2010.1217   修正Entity中CheckColumn无法正确计算选择字段的错误
 *                  优化SelectBuilder，允许Where中使用GroupBy字句，ToString时自动分割到正确位置
 *                  实体类增加静态方法FindByKeyForEdit，用于替代模版生成中的FindByKeyForEdit，为将要实现的表单基类（自定义表单）做准备
 *                  ********************************
 *                  实体基类继承自BinaryAccessor，IEntity增加IIndexAccessor接口和IBinaryAccessor接口，增加对快速索引访问和二进制访问的支持
 *                  快速索引访问：实体类可以不必生成索引器代码，IIndexAccessor直接提供按名称访问属性
 *                  二进制访问：支持把实体对象序列化成二进制或者反向操作
 *                  这两个功能尚未经过严格测试，请不要用于正式系统使用！
 * 
 * v6.3.2010.1209   修正实体工厂EntityFactory缓存实体导致无法识别后加载实体程序集的错误
 * 
 * v6.2.2010.1202   SQLite增加读写锁，限制同时只能指定一个Excute操作
 *                  Entity的PageSplitSQL方法修正表名没有进行格式化的BUG
 * 
 * v6.1.2010.1119   取消依赖XLog，升级为依赖NewLife.Core，部分公共类库移植到NewLife.Core
 *                  修正EntityTree中FindChilds错误，增加排序字段的支持，如果指定排序字段，查询子级的时候将按排序字段降序排序
 *                  取消授权限制，但仍然混淆代码
 * 
 * v6.0.2010.1021   增加字典缓存DictionaryCache
 *                  增加弱引用泛型WeakReference<T>
 *                  单对象实体缓存改为弱引用，使得缓存对象在没有引用时得到回收
 *                  单对象实体缓存默认填充方法改为实体基类的FindByKey（前面某个版本增加，参数为Object），据说Delegate.CreateDelegate出来的委托会很慢
 *                  实体元数据类Meta增加单对象实体缓存SingleCache，给SingleCacheInt和SingleCacheStr加上过期标识，到v7.0将不再支持
 *                  实体元数据类Meta增加OnDataChange的数据改变事件，并使用弱引用，当该实体有数据改变后，触发事件，可用于在外部删除该对象的缓存
 *                  （重要更新）实体基类增加字典缓存Extends，用于存储扩展属性，并增加专属的GetExtend方法用于获取扩展属性，向依赖实体类注册数据更改事件
 *                  （重要更新）实体树类升级为实体树基类，所有具有树形结构数据的实体类，继承自该类，享受树形实体的各种功能
 *                  实体基类增加虚拟的CreateXmlSerializer，允许实体类重载以改变Xml序列化行为，默认序列化行为改为序列化为特性
 *                  EntityList改变序列化行为，默认序列化为特性
 *                  EntityList判断元素是否存在Contains方法改为Exists
 *                  EntityList增加多字段排序方法Sort，可用于多个字段排序
 *                  修复快速访问方法、属性和字段所存在的问题，在实体基类索引器使用
 * 
 * v5.9.2010.1020   修正Database中QueryCountFast的严重错误
 * 
 * v5.8.2010.1018   增加实体树接口IEntityTree，用于解决实体树操作的一些共性问题，避免死循环
 * 
 * v5.7.2010.0930   XField中增加一个Table属性指向自己的XTable，创建XField时必须指定所属XTable
 *                  增加只读列表，各配置项使用只读列表返回，配置项自身检测列表是否被修改
 *                  实体操作接口增加Fields和FieldNames属性
 * 
 * v5.6.2010.0909   修改DAL，把QueryTimes和ExecuteTimes改为本线程的查询次数和执行次数
 *                  修改Entity，Meta.Count返回表的总记录数（快速），FindCount()使用普通方法查询真实记录数
 * 
 * v5.5.2010.0903   实体操作接口IEntityOperate返回的实体集合改为IList<IEntity>，因为使用操作接口时一般不知道具体类型，如果知道就没必要使用操作接口
 *                  增加数据连接名映射的配置，允许通过配置修改某一个实体或者某一个连接名实际对应的连接名
 *                  修改实体缓存和单对象缓存，使得缓存的数据因连接名或表名不同而不同，避免不同连接名或表名时缓存串号的问题
 *                  修改实体类结构模型，比如Area:Area<Area>:Entity<Area>，使得实体类可以通过继承实现二次扩展
 * 
 * v5.4.2010.0830   数据架构中的异步检查BeginCheck当启用检查时改为同步检查Check，保证数据库操作前先完成一次数据架构检查
 *                  唯一键为自增且参数小于等于0时，返回空
 *                  实体操作接口IEntityOperate增加ToList方法，实现把ICollection转为List<IEntity>
 *                  优化Entity的FindAll方法，处理海量数据尾页查询时使用优化算法
 * 
 * v5.3.2010.0826   DAL增加CreateOperate方法，为数据表动态创建实体类操作接口，支持在没有实体类的情况下操作数据库
 *                  该版本为不稳定版本
 * 
 * v5.2.2010.0726   IEntity接口增加SetItem方法，提供影响脏数据的弱类型数据设置
 *                  IEntityOperate接口增加Create方法，提供创建被类型实体对象的功能
 * 
 * v5.1.2010.0709   增加实体接口、实体操作接口、实体基类的基类，提供弱类型的Orm支持
 * 
 * v5.0.2010.0625   DAL优化
 *                  重新启用授权管理
 *                  EntityList增加排序方法Sort
 * 
 * v4.9.2010.0430   使用SelectBuilder来构造SQL语句，用于各层之间传递，准备将所有方法往SelectBuilder过度。该更新可能造成使用GroupBy的地方计算出错
 * 
 * v4.8.2010.0325   修改Entity索引器，新的快速调用方法在set的时候有问题
 *                  增加常用查询方法为Web方法
 * 
 * v4.8.2010.0301   增加实体类多表支持和多数据库支持
 *                  暴露几个常用的实体类静态方法供WebService引用
 * 
 * v4.7.2010.0130   数据架构中识别表名时不应该区分大小写
 *                  Entity中增加MakeCondition方法，以便于构造where语句
 * 
 * v4.6.2009.1226   改善分页算法，产生更简单的分页语句
 * 
 * v4.5.2009.1127   增加单实体缓存
 * 
 * v4.4.2009.1125   修改二级缓存，Entities改为EntityList类型，非空，支持FindAll操作
 * 
 * v4.3.2009.1121   修正Entity中Save方法判断自增字段不准确的BUG
 * 
 * v4.2.2009.1114   优化SqlServer取架构信息的性能，以及输出的SQL的可读性
 *                  支持Sql2008，通过Sql2005类
 *                  优化QueryCount方法，产生更简短的SQL
 * 
 * v4.1.2009.1028   增加快速获取单表总记录数方法QueryCountFast，修改Entity，在记录数大于1000时自动使用快速取总记录数
 * 
 * v4.0.2009.1011   增加实体类集合EntityList，Entity的所有FindAll返回EntityList
 *                  增强数据架构功能，支持Access、SQL2000、SQL2005
 * 
 * v3.7.2009.0907   修正DatabaseSchema中的一个小错误
 * 
 * v3.6.2009.0819   修正FindCount方法的错误
 * 
 * v3.5.2009.0714   Config类输出的FieldItem集合改为数组，防止被外部修改。
 *                  所有Select语句，使用*表示所有列，而不是列出所有列名。
 * 
 * v3.4.2009.0701   修正SqlServer 2000取主键的错误
 * 
 * v3.3.2009.0628   修改DAL，屏蔽Web请求级缓存DB的方法，似乎Web下多线程很不稳定，从而导致事务无法正常使用。
 * 
 * v3.2.2009.0623   修改Oracle，重载GetTables方法，修正无法从Oracle数据库取得构架信息的错误
 * 
 * v3.1.2009.0611   修改SqlServer类，使得每次返回构架信息时，都是从数据库取值。
 * 
 * v3.0.2009.0608   元数据类Meta增加一个字段名列表属性FieldNames
 *                  调整DatabaseSchema类，新增字段时，直接设置默认值，否则对于非空字段，创建字段将会失败
 *                  数据构架增加DatabaseSchema_Exclude配置项，用于指定要排除检查的链接名。
 *                  Entity中，增加ToXml输出的Xml的编码为UTF8，增加FromXml反序列化，增加Clone方法和CloneEntity方法
 *                  Database中，增加事务计数字段，支持多级事务。
 *                  Entity中，集合运算返回值改为List<T>，而不是IList<T>，更方便调用
 *                  在Database的QueryCount方法增加自动去除排序子句的功能
 *                  Entity中，增加ToString重载，默认显示Name属性
 *                  Entity中，Update时，增加了脏数据的判断，非脏数据的字段不更新，由于该功能的增加将导致以前所有的实体都无法Update到数据库，故版本改为3.0
 * 
 * v2.3.2009.0530   修正非自增字段做主键时也调用InsertAndGetIdentity的错误。
 * 
 * v2.2.2009.0527   数据表结构中，增加Int16和Int64两种类型
 * 
 * v2.1.2009.0408   修正DAL中_DBs空引用的问题，可能是因为该成员是线程静态，并没有在每一个线程上new一个对象。
 * 
 * v2.0.2009.0408   增加数据架构的功能。数据架构可以实现通过实体类反向更新数据库结构，不启用时，仅把更新SQL写入日志
 *                  修正Access类使用当前目录时拼接路径的错误。
 *                  
 * v1.2.2008.01.01  使用泛型基类重构
 * 
 * v1.1.2007.03.08  大量扩展功能，支持自定义表单、广义单点登录等项目
 *                  完善对Oracle的支持，支持电力生产管理系统等项目
 *                  完善对Sybase的支持，支持电力SCADA数据分析等项目
 * 
 * v1.0.2005.10.01  创建项目
 *                  支持C++客户端网络验证系统等项目
 *                  支持图片验证码识别等项目
*/
