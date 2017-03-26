using System;
using System.Collections.Generic;
using System.Linq;

namespace NewLife.Remoting
{
    /// <summary>表示全局筛选器集合。</summary>
    public static class GlobalFilters
    {
        /// <summary>过滤器类型集合</summary>
        private static List<Object> Filters { get; } = new List<Object>();

        ///// <summary>添加过滤器</summary>
        ///// <typeparam name="T"></typeparam>
        //public static void Add<T>() where T : IActionFilter, new()
        //{
        //    Filters.Add(new T());
        //}

        /// <summary>添加过滤器</summary>
        /// <param name="filter"></param>
        public static void Add(Object filter)
        {
            Filters.Add(filter);
        }

        /// <summary>动作过滤器</summary>
        public static IActionFilter[] ActionFilters { get { return Filters.Where(e => e is IActionFilter).Cast<IActionFilter>().ToArray(); } }

        /// <summary>异常过滤器</summary>
        public static IExceptionFilter[] ExceptionFilters { get { return Filters.Where(e => e is IExceptionFilter).Cast<IExceptionFilter>().ToArray(); } }
    }
}