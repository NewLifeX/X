namespace NewLife.Data;

/// <summary>数据过滤器（责任链模式）</summary>
/// <remarks>
/// 过滤器通过 <see cref="Next"/> 构成单向链表，按顺序对 <see cref="FilterContext"/> 进行处理。
/// 实现方应保证自身无状态或自行处理线程安全。推荐只在热点路径使用最少分配与装箱。
/// </remarks>
public interface IFilter
{
    /// <summary>下一个过滤器</summary>
    IFilter? Next { get; }

    /// <summary>对封包执行过滤器</summary>
    /// <param name="context">过滤上下文（不可为 null）</param>
    void Execute(FilterContext context);
}

/// <summary>过滤器上下文</summary>
/// <remarks>
/// 封装过滤过程中传递的环境数据。默认仅包含 <see cref="Packet"/>。
/// 约定：当 <see cref="Packet"/> 为 null 时，表示链路应停止继续传递（主动丢弃或已被消费）。
/// 内存管理请参考 <see cref="IPacket"/> 的所有权说明。
/// </remarks>
public class FilterContext
{
    /// <summary>封包</summary>
    /// <remarks>可由过滤器修改（如切片、聚合、替换），为 null 表示终止后续过滤。</remarks>
    public virtual IPacket? Packet { get; set; }
}

/// <summary>过滤器助手</summary>
public static class FilterHelper
{
    /// <summary>在链条里面查找指定类型的过滤器</summary>
    /// <param name="filter">起始过滤器</param>
    /// <param name="filterType">待查找的过滤器类型</param>
    /// <returns>首个匹配的过滤器；未找到返回 null</returns>
    public static IFilter? Find(this IFilter filter, Type filterType)
    {
        if (filter == null || filterType == null) return null;

        if (filter.GetType() == filterType) return filter;

        return filter.Next?.Find(filterType);
    }

    /// <summary>在链条里面查找指定类型的过滤器</summary>
    /// <typeparam name="TFilter">过滤器类型</typeparam>
    /// <param name="filter">起始过滤器</param>
    /// <returns>首个匹配的过滤器；未找到返回 null</returns>
    public static TFilter? Find<TFilter>(this IFilter filter) where TFilter : class, IFilter
        => (TFilter?)Find(filter, typeof(TFilter));

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
/// <remarks>
/// 实现 <see cref="Execute(FilterContext)"/> 的通用流程控制：
/// - 调用 <see cref="OnExecute(FilterContext)"/> 执行实际逻辑
/// - 若返回 true 且 <see cref="FilterContext.Packet"/> 非空，则传递给 <see cref="Next"/>
/// - 若返回 false 或封包为空，终止责任链
/// </remarks>
public abstract class FilterBase : IFilter
{
    /// <summary>下一个过滤器</summary>
    public IFilter? Next { get; set; }

    ///// <summary>实例化过滤器</summary>
    ///// <param name="next"></param>
    //public FilterBase(IFilter next) { Next = next; }

    /// <summary>对封包执行过滤器</summary>
    /// <param name="context">过滤上下文（不可为 null）</param>
    public virtual void Execute(FilterContext context)
    {
        if (!OnExecute(context) || context.Packet == null) return;

        Next?.Execute(context);
    }

    /// <summary>执行过滤</summary>
    /// <param name="context">过滤上下文</param>
    /// <returns>返回是否执行下一个过滤器（true 继续；false 终止）</returns>
    protected abstract Boolean OnExecute(FilterContext context);
}