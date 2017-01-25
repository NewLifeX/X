using System;
using System.ComponentModel;
using NewLife.Xml;

namespace XTemplate
{
    /// <summary>XTemplate设置</summary>
    [DisplayName("XTemplate设置")]
    [XmlConfigFile(@"Config\XTemplate.config", 15000)]
    public class Setting : XmlConfig<Setting>
    {
        #region 属性
        /// <summary>是否启用调试。默认不启用</summary>
        [Description("是否启用调试，默认不启用")]
        public Boolean Debug { get; set; }

        /// <summary>模版引用的程序集，多个用逗号或分号隔开</summary>
        [Description("模版引用的程序集，多个用逗号或分号隔开")]
        public String References { get; set; } = "";

        /// <summary>模版引用的命名空间，多个用逗号或分号隔开</summary>
        [Description("模版引用的命名空间，多个用逗号或分号隔开")]
        public String Imports { get; set; } = "";

        /// <summary>模版基类名</summary>
        [Description("模版基类名")]
        public String BaseClassName { get; set; } = "";
        #endregion

        #region 方法
        /// <summary>实例化设置</summary>
        public Setting()
        {
        }
        #endregion
    }
}