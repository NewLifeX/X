using System;
using NewLife.Linq;
using NewLife.Reflection;
using NewLife.Xml;

namespace NewLife.Common
{
    /// <summary>系统设置。提供系统名称、版本等基本设置。</summary>
    public class SysConfig : SysConfig<SysConfig> { }

    /// <summary>系统设置。提供系统名称、版本等基本设置。泛型基类，可继承扩展。</summary>
    /// <typeparam name="TSetting"></typeparam>
    [XmlConfigFile("config/sys.config", 15000)]
    public class SysConfig<TSetting> : XmlConfig<TSetting> where TSetting : SysConfig<TSetting>, new()
    {
        #region 属性
        private String _Name;
        /// <summary>系统名称</summary>
        public String Name { get { return _Name; } set { _Name = value; } }

        private String _Version;
        /// <summary>系统版本</summary>
        public String Version { get { return _Version; } set { _Version = value; } }

        private String _DisplayName;
        /// <summary>显示名称</summary>
        public String DisplayName { get { return _DisplayName; } set { _DisplayName = value; } }

        private String _Company;
        /// <summary>公司</summary>
        public String Company { get { return _Company; } set { _Company = value; } }

        private String _Address;
        /// <summary>地址</summary>
        public String Address { get { return _Address; } set { _Address = value; } }

        private String _Tel;
        /// <summary>电话</summary>
        public String Tel { get { return _Tel; } set { _Tel = value; } }

        private String _Fax;
        /// <summary>传真</summary>
        public String Fax { get { return _Fax; } set { _Fax = value; } }

        private String _EMail;
        /// <summary>电子邮件</summary>
        public String EMail { get { return _EMail; } set { _EMail = value; } }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public SysConfig()
        {
            var asmx = SysAssembly;

            Name = asmx != null ? asmx.Name : "NewLifePlatform";
            Version = asmx != null ? asmx.Version : "0.1";
            DisplayName = asmx != null ? (asmx.Title ?? asmx.Name) : "新生命管理平台";
            Company = asmx != null ? asmx.Company : "新生命开发团队";
            Address = "新生命开发团队";

            if (String.IsNullOrEmpty(DisplayName)) DisplayName = "系统设置";
        }

        /// <summary>系统主程序集</summary>
        private static AssemblyX SysAssembly;

        static SysConfig()
        {
            SysAssembly = AssemblyX.Entry;
            if (SysAssembly == null)
                SysAssembly = AssemblyX.GetMyAssemblies()
                    .Where(e => e.Title == null || !(e.Title.Contains("新生命") && (e.Title.Contains("库") || e.Title.Contains("框架"))))
                    .OrderByDescending(e => e.Compile).FirstOrDefault();
        }
        #endregion
    }
}