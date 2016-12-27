using System;

namespace NewLife.Data
{
    /// <summary>数据过滤器</summary>
    public interface IFilter
    {
        /// <summary>下一个过滤器</summary>
        IFilter Next { get; }

        /// <summary>对封包执行过滤器</summary>
        /// <param name="context"></param>
        void Execute(FilterContext context);
    }

    /// <summary>过滤器上下文</summary>
    public class FilterContext
    {
        /// <summary>封包</summary>
        public virtual Packet Packet { get; set; }
    }

    /// <summary>过滤器助手</summary>
    public static class FilterHelper
    {
        /// <summary>在链条里面查找指定类型的过滤器</summary>
        /// <param name="filter"></param>
        /// <param name="filterType"></param>
        /// <returns></returns>
        public static IFilter Find(this IFilter filter, Type filterType)
        {
            if (filter == null || filterType == null) return null;

            if (filter.GetType() == filterType) return filter;

            return filter.Next?.Find(filterType);
        }

        ///// <summary>在开头插入过滤器</summary>
        ///// <param name="filter"></param>
        ///// <param name="newFilter"></param>
        ///// <returns></returns>
        //public static IFilter Add(this IFilter filter, IFilter newFilter)
        //{
        //    if (filter == null || newFilter == null) return filter;

        //    newFilter.Next = filter;

        //    return newFilter;
        //}
    }

    /// <summary>数据过滤器基类</summary>
    public abstract class FilterBase : IFilter
    {
        /// <summary>下一个过滤器</summary>
        public IFilter Next { get; set; }

        ///// <summary>实例化过滤器</summary>
        ///// <param name="next"></param>
        //public FilterBase(IFilter next) { Next = next; }

        /// <summary>对封包执行过滤器</summary>
        /// <param name="context"></param>
        public virtual void Execute(FilterContext context)
        {
            if (!OnExecute(context) || context.Packet == null) return;

            Next?.Execute(context);
        }

        /// <summary>执行过滤</summary>
        /// <param name="context"></param>
        /// <returns>返回是否执行下一个过滤器</returns>
        protected abstract Boolean OnExecute(FilterContext context);
    }
}