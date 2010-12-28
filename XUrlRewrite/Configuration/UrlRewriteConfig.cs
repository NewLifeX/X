using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace XUrlRewrite.Configuration
{
    /// <summary>
    /// 模板配置根
    /// </summary>
    public class UrlRewriteConfig : ConfigurationSection
    {
        /// <summary>
        /// 全局开关
        /// </summary>
        [ConfigurationProperty("enabled",DefaultValue=true)]
        public Boolean Enabled
        {
            get
            {
                return (Boolean)this["enabled"];
            }
            set
            {
                this["enabled"] = value;
            }
        }
        /// <summary>
        /// 模板文件目录
        /// </summary>
        [ConfigurationProperty("directory",DefaultValue="~/Templates")]
        public String Directory
        {
            get
            {
                return (String)this["directory"];
            }
            set
            {
                this["directory"] = value;
            }
        }
        /// <summary>
        /// 模板Url映射配置集合
        /// </summary>
        [ConfigurationProperty("urls",IsDefaultCollection=false)]
        [ConfigurationCollection(typeof(UrlCollection))]
        public UrlCollection Urls
        {
            get
            {
                return (UrlCollection)this["urls"];
            }
            set
            {
                this["urls"] = value;
            }
        }
    }
}
