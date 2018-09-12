using System;
using System.ComponentModel;
using NewLife;
using NewLife.Xml;
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

        /// <summary>跟踪SQL执行时间，大于该阀值将输出日志，默认1000毫秒</summary>
        [Description("SQL执行时间。跟踪SQL执行时间，大于该阀值将输出日志，默认1000毫秒")]
        public Int32 TraceSQLTime { get; set; } = 1000;

        /// <summary>连接名映射#，表名映射@，表名映射@，把实体类中的Test2和Test3连接名映射到Test去</summary>
        [Description("连接映射。连接名映射#，表名映射@，把实体类中的Test2和Test3连接名映射到Test去")]
        public String ConnMaps { get; set; } = "";

        /// <summary>参数化添删改查。默认关闭</summary>
        [Description("参数化添删改查。默认关闭")]
        public Boolean UseParameter { get; set; }

        /// <summary>SQLite数据库默认目录。没有设置连接字符串的连接默认创建SQLite连接，数据库放在该目录</summary>
        [Description("SQLite默认目录。没有设置连接字符串的连接默认创建SQLite连接，数据库放在该目录")]
        public String SQLiteDbPath { get; set; } = "";

        /// <summary>备份目录。备份数据库时存放的目录</summary>
        [Description("备份目录。备份数据库时存放的目录")]
        public String BackupPath { get; set; } = "";

        /// <summary>命令超时。查询执行超时时间，默认0秒不限制</summary>
        [Description("命令超时。查询执行超时时间，默认0秒不限制")]
        public Int32 CommandTimeout { get; set; }

        /// <summary>数据层缓存。默认0秒</summary>
        [Description("数据层缓存。默认0秒")]
        public Int32 DataCacheExpire { get; set; }

        /// <summary>实体缓存过期。整表缓存实体列表，默认10秒</summary>
        [Description("实体缓存过期。整表缓存实体列表，默认10秒")]
        public Int32 EntityCacheExpire { get; set; } = 10;

        /// <summary>单对象缓存过期。按主键缓存实体，默认10秒</summary>
        [Description("单对象缓存过期。按主键缓存实体，默认10秒")]
        public Int32 SingleCacheExpire { get; set; } = 10;

        /// <summary>扩展属性过期。扩展属性Extends缓存，默认10秒</summary>
        [Description("扩展属性过期。扩展属性Extends缓存，默认10秒")]
        public Int32 ExtendExpire { get; set; } = 10;

        /// <summary>反向工程。Off 关闭；ReadOnly 只读不执行；On 打开，仅新建；Full 完全，修改删除</summary>
        [Description("反向工程。Off 关闭；ReadOnly 只读不执行；On 打开，仅新建；Full 完全，修改删除")]
        public Migration Migration { get; set; } = Migration.On;
        #endregion

        #region 方法
        /// <summary>加载后检查默认值</summary>
        protected override void OnLoaded()
        {
            if (SQLiteDbPath.IsNullOrEmpty()) SQLiteDbPath = Runtime.IsWeb ? "..\\Data" : ".";
            if (BackupPath.IsNullOrEmpty()) BackupPath = Runtime.IsWeb ? "..\\Backup" : "Backup";

            base.OnLoaded();
        }
        #endregion
    }
}