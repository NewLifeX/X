using System.Collections.Generic;

namespace NewLife.Remoting
{
    /// <summary>表示全局筛选器集合。</summary>
    public static class GlobalFilters
    {
        /// <summary>过滤器类型集合</summary>
        public static List<IActionFilter> Filters { get; } = new List<IActionFilter>();

        /// <summary>添加过滤器</summary>
        /// <typeparam name="T"></typeparam>
        public static void Add<T>() where T : IActionFilter, new()
        {
            Filters.Add(new T());
        }
    }
}