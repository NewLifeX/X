using System;

namespace NewLife.Configuration
{
    /// <summary>配置特性</summary>
    /// <remarks>
    /// 声明配置模型使用哪一种配置提供者，以及所需要的文件名和分类名。
    /// 如未指定提供者，则使用全局默认，此时将根据全局代码配置或环境变量配置使用不同提供者，实现配置信息整体转移。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ConfigAttribute : Attribute
    {
        /// <summary>提供者。内置ini/xml/json/http，一般不指定，使用全局默认</summary>
        public String Provider { get; set; }

        /// <summary>配置名。可以是文件名或分类名</summary>
        public String Name { get; set; }

        /// <summary>指定配置名</summary>
        /// <param name="name">配置名。可以是文件名或分类名</param>
        /// <param name="provider">提供者。内置ini/xml/json/http，一般不指定，使用全局默认</param>
        public ConfigAttribute(String name, String provider = null)
        {
            Provider = provider;
            Name = name;
        }
    }
}