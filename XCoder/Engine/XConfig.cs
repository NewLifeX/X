using System;
using System.ComponentModel;
using System.Text;
using NewLife.Xml;

namespace XCoder
{
    [XmlConfigFile("Setting.config")]
    public class XConfig : XmlConfig<XConfig>
    {
        #region 属性
        private String _ConnName;
        /// <summary>链接名</summary>
        [DisplayName("链接名")]
        public String ConnName
        {
            get
            {
                if (String.IsNullOrEmpty(_ConnName)) _ConnName = "ConnName";
                return _ConnName;
            }
            set { _ConnName = value; }
        }

        private String _Prefix;
        /// <summary>前缀</summary>
        [DisplayName("前缀")]
        public String Prefix { get { return _Prefix; } set { _Prefix = value; } }

        private String _NameSpace;
        /// <summary>命名空间</summary>
        [DisplayName("命名空间")]
        public String NameSpace
        {
            get { return String.IsNullOrEmpty(_NameSpace) ? EntityConnName : _NameSpace; }
            set { _NameSpace = value; }
        }

        private String _TemplateName;
        /// <summary>模板名</summary>
        [DisplayName("模板名")]
        public String TemplateName { get { return _TemplateName; } set { _TemplateName = value; } }

        private String _OutputPath;
        /// <summary>输出目录</summary>
        [DisplayName("输出目录")]
        public String OutputPath
        {
            get { return String.IsNullOrEmpty(_OutputPath) ? EntityConnName : _OutputPath; }
            set { _OutputPath = value; }
        }

        private Boolean _Override = true;
        /// <summary>是否覆盖目标文件</summary>
        [DisplayName("是否覆盖目标文件")]
        public Boolean Override { get { return _Override; } set { _Override = value; } }

        private String _EntityConnName;
        /// <summary>实体链接名</summary>
        [DisplayName("实体链接名")]
        public String EntityConnName
        {
            get { return String.IsNullOrEmpty(_EntityConnName) ? ConnName : _EntityConnName; }
            set { _EntityConnName = value; }
        }

        private String _BaseClass;
        /// <summary>实体基类</summary>
        [DisplayName("实体基类")]
        public String BaseClass
        {
            get
            {
                if (String.IsNullOrEmpty(_BaseClass)) _BaseClass = "Entity";
                return _BaseClass;
            }
            set { _BaseClass = value; }
        }

        private Boolean _RenderGenEntity;
        /// <summary>生成泛型实体类</summary>
        [DisplayName("生成泛型实体类")]
        public Boolean RenderGenEntity { get { return _RenderGenEntity; } set { _RenderGenEntity = value; } }

        private Boolean _NeedFix = true;
        /// <summary>是否需要修正。默认true，将根据配置删除前缀、自动化大小写和完善注释等</summary>
        [DisplayName("是否需要修正。默认true，将根据配置删除前缀、自动化大小写和完善注释等")]
        public Boolean NeedFix { get { return _NeedFix; } set { _NeedFix = value; } }

        private Boolean _AutoCutPrefix;
        /// <summary>自动去除前缀</summary>
        [DisplayName("自动去除前缀")]
        public Boolean AutoCutPrefix
        {
            get { return _AutoCutPrefix; }
            set { _AutoCutPrefix = value; }
        }

        private Boolean _CutTableName;
        /// <summary>是否自动去除字段前面的表名</summary>
        [DisplayName("是否自动去除字段前面的表名")]
        public Boolean AutoCutTableName { get { return _CutTableName; } set { _CutTableName = value; } }

        private Boolean _AutoFixWord;
        /// <summary>自动纠正大小写</summary>
        [DisplayName("自动纠正大小写")]
        public Boolean AutoFixWord
        {
            get { return _AutoFixWord; }
            set { _AutoFixWord = value; }
        }

        private Boolean _UseCNFileName;
        /// <summary>使用中文文件名</summary>
        [DisplayName("使用中文文件名")]
        public Boolean UseCNFileName { get { return _UseCNFileName; } set { _UseCNFileName = value; } }

        private Boolean _UseID;
        /// <summary>强制使用ID</summary>
        [DisplayName("强制使用ID")]
        public Boolean UseID { get { return _UseID; } set { _UseID = value; } }

        private Boolean _UseHeadTemplate;
        /// <summary>使用头部模版</summary>
        [DisplayName("使用头部模版")]
        public Boolean UseHeadTemplate
        {
            get { return _UseHeadTemplate; }
            set { _UseHeadTemplate = value; }
        }

        private String _HeadTemplate;
        /// <summary>头部模版</summary>
        [DisplayName("头部模版")]
        public String HeadTemplate
        {
            get { return _HeadTemplate; }
            set { _HeadTemplate = value; }
        }

        private Boolean _Debug;
        /// <summary>调试</summary>
        [DisplayName("调试")]
        public Boolean Debug { get { return _Debug; } set { _Debug = value; } }

        private SerializableDictionary<String, String> _Items;
        /// <summary> 字典属性</summary>
        [DisplayName("数据字典")]
        public SerializableDictionary<String, String> Items { get { return _Items ?? (_Items = new SerializableDictionary<string, string>()); } set { _Items = value; } }
        #endregion

        #region 全局
        private DateTime _LastUpdate;
        /// <summary>最后更新时间</summary>
        [DisplayName("最后更新时间")]
        public DateTime LastUpdate { get { return _LastUpdate; } set { _LastUpdate = value; } }

        private String _LastTool;
        /// <summary>最后一个使用的工具</summary>
        [DisplayName("最后一个使用的工具")]
        public String LastTool { get { return _LastTool; } set { _LastTool = value; } }
        #endregion

        #region 加载/保存
        //public static XConfig Load()
        //{
        //    if (!File.Exists(DefaultFile)) return Create();

        //    var xml = new NewLife.Xml.XmlReaderX();
        //    using (var xr = XmlReader.Create(DefaultFile))
        //    {
        //        try
        //        {
        //            Object obj = null;
        //            xml.Reader = xr;
        //            if (xml.ReadObject(typeof(XConfig), ref obj, null) && obj != null)
        //            {
        //                return obj as XConfig;
        //            }
        //            return Create();
        //            //return xml.Deserialize(stream) as XConfig;
        //        }
        //        catch { return Create(); }
        //    }
        //}

        public XConfig()
        {
            //var config = new XConfig();

            var sb = new StringBuilder();
            sb.AppendLine("/*");
            sb.AppendLine(" * XCoder v<#=Version#>");
            sb.AppendLine(" * 作者：<#=Environment.UserName + \"/\" + Environment.MachineName#>");
            sb.AppendLine(" * 时间：<#=DateTime.Now.ToString(\"yyyy-MM-dd HH:mm:ss\")#>");
            sb.AppendLine(" * 版权：版权所有 (C) 新生命开发团队 2002~<#=DateTime.Now.ToString(\"yyyy\")#>");
            sb.AppendLine("*/");
            HeadTemplate = sb.ToString();
        }

        //public void Save()
        //{
        //    if (!String.IsNullOrEmpty(HeadTemplate)) HeadTemplate = HeadTemplate.Replace("\n", Environment.NewLine);

        //    if (File.Exists(DefaultFile)) File.Delete(DefaultFile);

        //    var xml = new NewLife.Xml.XmlWriterX();

        //    using (var writer = XmlWriter.Create(DefaultFile))
        //    {
        //        xml.Writer = writer;
        //        xml.WriteObject(this, typeof(XConfig), null);
        //    }
        //}

        //static String DefaultFile = "XCoder.xml";
        #endregion
    }
}