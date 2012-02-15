using System;
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
        [ConfigurationProperty("enabled", DefaultValue = true)]
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
        [ConfigurationProperty("directory", DefaultValue = "~/Templates")]
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
        /// 自定义过滤器,用于避免特定请求不使用Url重写
        /// </summary>
        /// <remarks>
        /// 格式是完整的类名后跟随方法名,方法需要符合Func&lt;string,string,HttpApplication,bool&gt; 委托的签名
        /// </remarks>
        [ConfigurationProperty("customfilter", DefaultValue = "")]
        public string CustomFilter
        {
            get
            {
                return (string)this["customfilter"];
            }
            set
            {
                this["customfilter"] = value;
            }
        }

        /// <summary>
        /// 模板Url映射配置集合
        /// </summary>
        [ConfigurationProperty("urls", IsDefaultCollection = false)]
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