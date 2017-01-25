using System;
using System.ComponentModel;
using NewLife;
using NewLife.Configuration;
using NewLife.Xml;
using XCode.Cache;
using XCode.DataAccessLayer;

namespace XCode
{
    /// <summary>XCode设置</summary>
    [DisplayName("XCode设置")]
    [XmlConfigFile(@"Config\XCode.config", 15000)]
    public class Setting : XmlConfig<Setting>
    {
        #region 属性
        /// <summary>是否启用调试。默认启用</summary>
        [Description("调试")]
        public Boolean Debug { get; set; } = true;

        /// <summary>是否输出SQL语句，默认启用</summary>
        [Description("输出SQL。是否输出SQL语句，默认启用")]
        public Boolean ShowSQL { get; set; } = true;

        /// <summary>设置SQL输出的单独目录，默认为空，SQL输出到当前日志中。生产环境建议输出到站点外单独的SqlLog目录</summary>
        [Description("SQL目录。设置SQL输出的单独目录，默认为空，SQL输出到当前日志中。生产环境建议输出到站点外单独的SqlLog目录")]
        public String SQLPath { get; set; } = "";

        /// <summary>跟踪SQL执行时间，大于该阀值将输出日志，默认500毫秒</summary>
        [Description("SQL执行时间。跟踪SQL执行时间，大于该阀值将输出日志，默认500毫秒")]
        public Int32 TraceSQLTime { get; set; } = 500;

        /// <summary>连接名映射#，表名映射@，把实体类中的Test2和Test3连接名映射到Test去</summary>
        [Description("连接映射。连接名映射#，表名映射@，把实体类中的Test2和Test3连接名映射到Test去")]
        public String ConnMaps { get; set; }

        /// <summary>是否启用动态代码调试，把动态生成的实体类代码和程序集输出到临时目录，默认不启用</summary>
        [Description("代码调试。是否启用动态代码调试，把动态生成的实体类代码和程序集输出到临时目录，默认不启用")]
        public Boolean CodeDebug { get; set; }

        /// <summary>实体类首次访问数据库时，是否执行数据初始化，默认true执行，导数据时建议关闭</summary>
        [Description("数据初始化。实体类首次访问数据库时，是否执行数据初始化，默认true执行，导数据时建议关闭")]
        public Boolean InitData { get; set; } = true;

        /// <summary>事务调试。打开时输出事务回滚日志，默认关闭</summary>
        [Description("事务调试。打开时输出事务回滚日志，默认关闭")]
        public Boolean TransactionDebug { get; set; }

        /// <summary>SQLite数据库默认目录。没有设置连接字符串的连接默认创建SQLite连接，数据库放在该目录</summary>
        [Description("SQLite默认目录。没有设置连接字符串的连接默认创建SQLite连接，数据库放在该目录")]
        public String SQLiteDbPath { get; set; }

        /// <summary>缓存调试</summary>
        [Description("缓存调试")]
        public Boolean CacheDebug { get; set; }

        /// <summary>独占数据库。独占时将大大加大缓存权重，默认true</summary>
        [Description("独占数据库。独占时将大大加大缓存权重，默认true")]
        public Boolean Alone { get; set; } = true;

        /// <summary>实体缓存过期。默认60秒</summary>
        [Description("实体缓存过期。默认60秒")]
        public Int32 EntityCacheExpire { get; set; } = 60;

        /// <summary>单对象缓存过期。默认60秒</summary>
        [Description("单对象缓存过期。默认60秒")]
        public Int32 SingleCacheExpire { get; set; } = 60;

        /// <summary>反向工程</summary>
        [Description("反向工程")]
        public NegativeSetting Negative { get; set; }

        /// <summary>Oracle设置</summary>
        [Description("Oracle设置")]
        public OracleSetting Oracle { get; set; }
        #endregion

        #region 方法
        /// <summary>实例化设置</summary>
        public Setting()
        {
            ConnMaps = "Conn2#Conn,Table3@Table";

            Negative = new NegativeSetting();
            Oracle = new OracleSetting();
        }

        /// <summary>新建时调用</summary>
        protected override void OnNew()
        {
            Negative.Init();
        }

        /// <summary>加载后检查默认值</summary>
        protected override void OnLoaded()
        {
            var dbpath = SQLiteDbPath;
            if (dbpath.IsNullOrEmpty())
            {
                dbpath = ".";
                if (Runtime.IsWeb)
                {
                    if (!Environment.CurrentDirectory.Contains("iisexpress") ||
                        !Environment.CurrentDirectory.Contains("Web"))
                        dbpath = "..\\Data";
                    else
                        dbpath = "~\\App_Data";
                }
                SQLiteDbPath = dbpath;
            }

            base.OnLoaded();
        }
        #endregion
    }

    /// <summary>Oracle设置</summary>
    public class OracleSetting
    {
        #region 属性
        /// <summary>是否限制只能访问拥有者的信息，默认true</summary>
        [Description("是否限制只能访问拥有者的信息，默认true")]
        public Boolean UseOwner { get; set; } = true;

        /// <summary>是否忽略大小写，如果不忽略则在表名字段名外面加上双引号，默认true</summary>
        [Description("是否忽略大小写，如果不忽略则在表名字段名外面加上双引号，默认true")]
        public Boolean IgnoreCase { get; set; } = true;
        #endregion

        /// <summary>初始化</summary>
        public OracleSetting()
        {
        }
    }
}