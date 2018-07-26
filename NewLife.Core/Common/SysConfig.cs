using System;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
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
        public String Name { get; set; } = "";

        /// <summary>系统版本</summary>
        [DisplayName("系统版本")]
        public String Version { get; set; } = "";

        /// <summary>显示名称</summary>
        [DisplayName("显示名称")]
        [Description("用户可见的名称")]
        public String DisplayName { get; set; } = "";

        /// <summary>公司</summary>
        [DisplayName("公司")]
        public String Company { get; set; } = "";

        /// <summary>开发者模式</summary>
        [DisplayName("开发者模式")]
        public Boolean Develop { get; set; } = true;

        /// <summary>启用</summary>
        [DisplayName("启用")]
        public Boolean Enable { get; set; } = true;

        /// <summary>安装时间</summary>
        [DisplayName("安装时间")]
        public DateTime InstallTime { get; set; } = DateTime.Now;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public SysConfig()
        {
        }

#if !__CORE__
        /// <summary>新建配置</summary>
        protected override void OnNew()
        {
            var asmx = SysAssembly;

            Name = asmx?.Name ?? "NewLife.Cube";
            Version = asmx?.Version ?? "0.1";
            DisplayName = (asmx?.Title ?? asmx?.Name) ?? "魔方平台";
            Company = asmx?.Company ?? "新生命开发团队";
            //Address = "新生命开发团队";

            if (DisplayName.IsNullOrEmpty()) DisplayName = "系统设置";
        }

        /// <summary>系统主程序集</summary>
        public static AssemblyX SysAssembly
        {
            get
            {
                try
                {
                    var list = AssemblyX.GetMyAssemblies();
                    //if (list.Count > 1) list = list.Where(e => e.Title.IsNullOrEmpty() || !(e.Title.Contains("新生命") && (e.Title.Contains("库") || e.Title.Contains("框架") || e.Title.Contains("SQLite")))).ToList();

                    // 最后编译那一个
                    list = list.OrderByDescending(e => e.Compile)
                        .ThenByDescending(e => e.Name.EndsWithIgnoreCase(".Web"))
                        .ToList();

                    return list.FirstOrDefault();
                }
                catch { return null; }
            }
        }
#endif
        #endregion
    }
}