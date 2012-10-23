using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Xml
{
    /// <summary>Xml配置文件特性</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class XmlConfigFileAttribute : Attribute
    {
        private String _FileName;
        /// <summary>配置文件名</summary>
        public String FileName { get { return _FileName; } set { _FileName = value; } }

        //private Boolean _AutoReload;
        ///// <summary>当文件改变时是否重新加载</summary>
        //public Boolean AutoReload { get { return _AutoReload; } set { _AutoReload = value; } }

        private Int32 _ReloadTime;
        /// <summary>重新加载时间。单位：秒</summary>
        public Int32 ReloadTime { get { return _ReloadTime; } set { _ReloadTime = value; } }

        /// <summary>指定配置文件名</summary>
        /// <param name="fileName"></param>
        public XmlConfigFileAttribute(String fileName) { FileName = fileName; }

        ///// <summary>指定配置文件名和是否自动重新加载</summary>
        ///// <param name="fileName"></param>
        ///// <param name="autoReload"></param>
        //public XmlConfigFileAttribute(String fileName, Boolean autoReload)
        //{
        //    FileName = fileName;
        //    AutoReload = autoReload;
        //}

        /// <summary>指定配置文件名和重新加载时间</summary>
        /// <param name="fileName"></param>
        /// <param name="reloadTime"></param>
        public XmlConfigFileAttribute(String fileName, Int32 reloadTime)
        {
            FileName = fileName;
            ReloadTime = reloadTime;
        }
    }
}
