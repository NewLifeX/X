using System;

namespace NewLife.Configuration
{
    /// <summary>配置文件特性</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ConfigFileAttribute : Attribute
    {
        /// <summary>配置文件名</summary>
        public String FileName { get; set; }

        /// <summary>指定配置文件名</summary>
        /// <param name="fileName"></param>
        public ConfigFileAttribute(String fileName) => FileName = fileName;
    }
}