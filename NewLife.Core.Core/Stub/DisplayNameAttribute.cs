using System;

namespace System.ComponentModel
{
    /// <summary>显示名</summary>
    public class DisplayNameAttribute : Attribute
    {
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>实例化</summary>
        /// <param name="name"></param>
        public DisplayNameAttribute(String name) { Name = name; }
    }
}