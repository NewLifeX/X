using System;

namespace NewLife.Remoting
{
    /// <summary>标识Api</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class ApiAttribute : Attribute
    {
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>实例化</summary>
        /// <param name="name"></param>
        public ApiAttribute(String name) => Name = name;
    }
}