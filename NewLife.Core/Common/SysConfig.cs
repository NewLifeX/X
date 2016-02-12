using System;
using System.ComponentModel;
using System.Linq;
using NewLife.Reflection;
using NewLife.Xml;

namespace NewLife.Common
{
    /// <summary>系统设置。提供系统名称、版本等基本设置。</summary>
    public class SysConfig : SysConfig<SysConfig> { }

    /// <summary>系统设置。提供系统名称、版本等基本设置。泛型基类，可继承扩展。</summary>
    /// <typeparam name="TSetting"></typeparam>
    [DisplayName("系统设置")]
    [XmlConfigFile("Config/Sys.config", 15000)]
    public class SysConfig<TSetting> : XmlConfig<TSetting> where TSetting : SysConfig<TSetting>, new()
    {
        #region 属性
        /// <summary>系统名称</summary>
        [DisplayName("系统名称")]
        [Description("用于标识系统的英文名")]
        public String Name { get; set; }

        /// <summary>系统版本</summary>
        [DisplayName("系统版本")]
        public String Version { get; set; }

        /// <summary>显示名称</summary>
        [DisplayName("显示名称")]
        [Description("用户可见的名称")]
        public String DisplayName { get; set; }

        /// <summary>公司</summary>
        [DisplayName("公司")]
        public String Company { get; set; }

        /// <summary>地址</summary>
        [DisplayName("地址")]
        public String Address { get; set; }

        /// <summary>电话</summary>
        [DisplayName("电话")]
        public String Tel { get; set; }

        /// <summary>传真</summary>
        [DisplayName("传真")]
        public String Fax { get; set; }

        /// <summary>电子邮件</summary>
        [DisplayName("电子邮件")]
        public String EMail { get; set; }

        /// <summary>开发者模式</summary>
        [DisplayName("开发者模式")]
        public Boolean Develop { get; set; }

        /// <summary>启用</summary>
        [DisplayName("启用")]
        public Boolean Enable { get; set; }

        /// <summary>安装时间</summary>
        [DisplayName("安装时间")]
        public DateTime InstallTime { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public SysConfig()
        {
            Develop = true;
            Enable = true;
            InstallTime = DateTime.Now;

            var asmx = SysAssembly;

            Name = asmx != null ? asmx.Name : "NewLife.Cube";
            Version = asmx != null ? asmx.Version : "0.1";
            DisplayName = asmx != null ? (asmx.Title ?? asmx.Name) : "新生命魔方平台";
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
                    .Where(e => e.Title == null || !(e.Title.Contains("新生命") && (e.Title.Contains("库") || e.Title.Contains("框架") || e.Title.Contains("SQLite"))))
                    .OrderByDescending(e => e.Compile).FirstOrDefault();
        }
        #endregion
    }
}