using System;

namespace System.ComponentModel
{
    /// <summary>描述</summary>
    public class DescriptionAttribute : Attribute
    {
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>实例化</summary>
        /// <param name="name"></param>
        public DescriptionAttribute(String name) { Name = name; }
    }
}