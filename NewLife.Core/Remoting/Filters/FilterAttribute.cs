using System;

namespace NewLife.Remoting
{
    /// <summary>表示操作和结果筛选器特性的基类。</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public abstract class FilterAttribute : Attribute
    {
        /// <summary>获取或者设置执行操作筛选器的顺序。</summary>
        public Int32 Order { get; set; }
    }
}