using System;

namespace NewLife.Json
{
    /// <summary>Json配置文件特性</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class JsonConfigFileAttribute : Attribute
    {
        private String _fileName;
        /// <summary>配置文件名</summary>
        public String FileName { get { return _fileName; } set { _fileName = value; } }

        private Int32 _reloadTime;
        /// <summary>重新加载时间。单位：毫秒</summary>
        public Int32 ReloadTime { get { return _reloadTime; } set { _reloadTime = value; } }

        /// <summary>指定配置文件名</summary>
        /// <param name="fileName"></param>
        public JsonConfigFileAttribute(String fileName) { FileName = fileName; }

        /// <summary>指定配置文件名和重新加载时间（毫秒）</summary>
        /// <param name="fileName"></param>
        /// <param name="reloadTime"></param>
        public JsonConfigFileAttribute(String fileName, Int32 reloadTime)
        {
            FileName = fileName;
            ReloadTime = reloadTime;
        }
    }
}