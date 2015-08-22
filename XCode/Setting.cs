using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Xml;
using XCode.Cache;
using XCode.DataAccessLayer;

namespace XCode
{
    /// <summary>树神设置</summary>
    [DisplayName("树神设置")]
    [XmlConfigFile(@"Config\\XCode.config", 15000)]
    public class Setting : XmlConfig<Setting>
    {
        #region 属性
        private Boolean _Debug;
        /// <summary>是否启用全局调试。默认为不启用</summary>
        [Description("全局调试")]
        public Boolean Debug { get { return _Debug; } set { _Debug = value; } }

        private Boolean _ShowSQL;
        /// <summary>是否输出SQL语句，默认为调试开关Debug</summary>
        [Description("是否输出SQL语句，默认为调试开关Debug")]
        public Boolean ShowSQL { get { return _ShowSQL; } set { _ShowSQL = value; } }

        private String _SQLPath;
        /// <summary>设置SQL输出的单独目录，默认为空，SQL输出到当前日志中。生产环境建议输出到站点外单独的SqlLog目录</summary>
        [Description("设置SQL输出的单独目录，默认为空，SQL输出到当前日志中。生产环境建议输出到站点外单独的SqlLog目录")]
        public String SQLPath { get { return _SQLPath; } set { _SQLPath = value; } }

        private Int32 _TraceSQLTime;
        /// <summary>跟踪SQL执行时间，大于该阀值将输出日志，默认0毫秒不跟踪</summary>
        [Description("跟踪SQL执行时间，大于该阀值将输出日志，默认0毫秒不跟踪")]
        public Int32 TraceSQLTime { get { return _TraceSQLTime; } set { _TraceSQLTime = value; } }

        private String _ConnMaps = "Test2#Test,Test3#Test";
        /// <summary>连接名映射#，表名映射@，把实体类中的Test2和Test3连接名映射到Test去</summary>
        [Description("连接名映射#，表名映射@，把实体类中的Test2和Test3连接名映射到Test去")]
        public String ConnMaps { get { return _ConnMaps; } set { _ConnMaps = value; } }

        private Boolean _CodeDebug;
        /// <summary>是否启用动态代码调试，把动态生成的实体类代码和程序集输出到临时目录，默认不启用</summary>
        [Description("是否启用动态代码调试，把动态生成的实体类代码和程序集输出到临时目录，默认不启用")]
        public Boolean CodeDebug { get { return _CodeDebug; } set { _CodeDebug = value; } }

        private String _ServiceAddress = "http://j.NewLifeX.com/?id=3&amp;f={0}";
        /// <summary>下载数据库驱动的地址，文件名用{0}替代。默认http://j.NewLifeX.com/?id=3&amp;f={0}</summary>
        [Description("下载数据库驱动的地址，文件名用{0}替代。默认http://j.NewLifeX.com/?id=3&amp;f={0}")]
        public String ServiceAddress { get { return _ServiceAddress; } set { _ServiceAddress = value; } }

        private Boolean _CacheZip = true;
        /// <summary>是否缓存数据库驱动Zip包到系统盘。默认缓存</summary>
        [Description("是否缓存数据库驱动Zip包到系统盘。默认缓存")]
        public Boolean CacheZip { get { return _CacheZip; } set { _CacheZip = value; } }

        private Boolean _InitData = true;
        /// <summary>实体类首次访问数据库时，是否执行数据初始化，默认true执行，导数据时建议关闭</summary>
        [Description("实体类首次访问数据库时，是否执行数据初始化，默认true执行，导数据时建议关闭")]
        public Boolean InitData { get { return _InitData; } set { _InitData = value; } }

        private CacheSetting _Cache = new CacheSetting();
        /// <summary>缓存</summary>
        [Description("缓存")]
        public CacheSetting Cache { get { return _Cache; } set { _Cache = value; } }

        private NegativeSetting _Negative = new NegativeSetting();
        /// <summary>反向工程</summary>
        [Description("反向工程")]
        public NegativeSetting Negative { get { return _Negative; } set { _Negative = value; } }

        private ModelSetting _Model = new ModelSetting();
        /// <summary>模型</summary>
        [Description("模型")]
        public ModelSetting Model { get { return _Model; } set { _Model = value; } }

        private OracleSetting _Oracle = new OracleSetting();
        /// <summary>Oracle设置</summary>
        [Description("Oracle设置")]
        public OracleSetting Oracle { get { return _Oracle; } set { _Oracle = value; } }
        #endregion

        #region 方法
        /// <summary>新建时调用</summary>
        protected override void OnNew()
        {
            Debug = Config.GetConfig<Boolean>("XCode.Debug", false);
            ShowSQL = Config.GetConfig<Boolean>("XCode.ShowSQL", Debug);
            SQLPath = Config.GetConfig<String>("XCode.SQLPath");
            TraceSQLTime = Config.GetConfig<Int32>("XCode.TraceSQLTime");

            Cache.Init();
            Negative.Init();
        }
        #endregion
    }

    /// <summary>模型设置</summary>
    public class ModelSetting
    {
        #region 属性
        private Boolean _UseID = true;
        /// <summary>是否ID作为id的格式化，否则使用原名。默认使用ID</summary>
        [Description("是否ID作为id的格式化，否则使用原名。默认使用ID")]
        public Boolean UseID { get { return _UseID; } set { _UseID = value; } }

        private Boolean _AutoCutPrefix = true;
        /// <summary>是否自动去除前缀，第一个_之前。默认启用</summary>
        [Description("是否自动去除前缀，第一个_之前。默认启用")]
        public Boolean AutoCutPrefix { get { return _AutoCutPrefix; } set { _AutoCutPrefix = value; } }

        private Boolean _AutoCutTableName = true;
        /// <summary>是否自动去除字段前面的表名。默认启用</summary>
        [Description("是否自动去除字段前面的表名。默认启用")]
        public Boolean AutoCutTableName { get { return _AutoCutTableName; } set { _AutoCutTableName = value; } }

        private Boolean _AutoFixWord = true;
        /// <summary>是否自动纠正大小写。默认启用</summary>
        [Description("是否自动纠正大小写。默认启用")]
        public Boolean AutoFixWord { get { return _AutoFixWord; } set { _AutoFixWord = value; } }

        private String _FilterPrefixs = "tbl,table";
        /// <summary>格式化表名字段名时，要过滤的前缀。默认tbl,table</summary>
        [Description("格式化表名字段名时，要过滤的前缀。默认tbl,table")]
        public String FilterPrefixs { get { return _FilterPrefixs; } set { _FilterPrefixs = value; } }
        #endregion
    }

    /// <summary>Oracle设置</summary>
    public class OracleSetting
    {
        #region 属性
        private Boolean _UseOwner;
        /// <summary>是否限制只能访问拥有者的信息，默认false</summary>
        [Description("是否限制只能访问拥有者的信息，默认false")]
        public Boolean UseOwner { get { return _UseOwner; } set { _UseOwner = value; } }

        private Boolean _IgnoreCase = true;
        /// <summary>是否忽略大小写，如果不忽略则在表名字段名外面加上双引号，默认true</summary>
        [Description("是否忽略大小写，如果不忽略则在表名字段名外面加上双引号，默认true")]
        public Boolean IgnoreCase { get { return _IgnoreCase; } set { _IgnoreCase = value; } }

        private String _DLLPath;
        /// <summary>属性说明</summary>
        [Description("是否限制只能访问拥有者的信息，默认false")]
        public String DLLPath { get { return _DLLPath; } set { _DLLPath = value; } }
        #endregion
    }
}