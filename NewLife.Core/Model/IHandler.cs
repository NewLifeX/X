namespace NewLife.Model;

/// <summary>管道处理器。兼容旧版本</summary>
[Obsolete("=>IPipelineHandler")]
public interface IHandler : IPipelineHandler { }

/// <summary>管道处理器</summary>
public interface IPipelineHandler
{
    /// <summary>上一个处理器。逆序处理时的下一个节点</summary>
    IPipelineHandler? Prev { get; set; }

    /// <summary>下一个处理器。正序处理时的下一个节点</summary>
    IPipelineHandler? Next { get; set; }

    /// <summary>读取数据，返回结果作为下一个处理器消息</summary>
    /// <remarks>
    /// 最终处理器决定如何使用消息。
    /// 处理得到单个消息时，调用一次下一级处理器，返回下级结果给上一级；
    /// 处理得到多个消息时，调用多次下一级处理器，返回null给上一级；
    /// </remarks>
    /// <param name="context">处理器上下文</param>
    /// <param name="message">输入消息</param>
    /// <returns>处理后的消息</returns>
    Object? Read(IHandlerContext context, Object message);

    /// <summary>写入数据，返回结果作为下一个处理器消息</summary>
    /// <param name="context">处理器上下文</param>
    /// <param name="message">输出消息</param>
    /// <returns>处理后的消息</returns>
    Object? Write(IHandlerContext context, Object message);

    /// <summary>打开连接</summary>
    /// <param name="context">处理器上下文</param>
    /// <returns>是否成功打开</returns>
    Boolean Open(IHandlerContext context);

    /// <summary>关闭连接</summary>
    /// <param name="context">处理器上下文</param>
    /// <param name="reason">关闭原因</param>
    /// <returns>是否成功关闭</returns>
    Boolean Close(IHandlerContext context, String reason);

    /// <summary>发生错误</summary>
    /// <param name="context">处理器上下文</param>
    /// <param name="exception">异常对象</param>
    /// <returns>是否已处理错误</returns>
    Boolean Error(IHandlerContext context, Exception exception);
}
/// <summary>处理器基类</summary>
public abstract class Handler : IPipelineHandler
{
    /// <summary>上一个处理器。逆序处理时的下一个节点</summary>
    public IPipelineHandler? Prev { get; set; }

    /// <summary>下一个处理器。正序处理时的下一个节点</summary>
    public IPipelineHandler? Next { get; set; }

    /// <summary>读取数据，返回结果作为下一个处理器消息</summary>
    /// <remarks>
    /// 最终处理器决定如何使用消息。
    /// 处理得到单个消息时，调用一次下一级处理器，返回下级结果给上一级；
    /// 处理得到多个消息时，调用多次下一级处理器，返回null给上一级；
    /// </remarks>
    /// <param name="context">处理器上下文</param>
    /// <param name="message">输入消息</param>
    /// <returns>处理后的消息</returns>
    public virtual Object? Read(IHandlerContext context, Object message)
    {
        if (Next != null) return Next.Read(context, message);

        // 最后一个处理器，截断
        context?.FireRead(message);

        return message;
    }

    /// <summary>写入数据，返回结果作为下一个处理器消息</summary>
    /// <param name="context">处理器上下文</param>
    /// <param name="message">输出消息</param>
    /// <returns>处理后的消息</returns>
    public virtual Object? Write(IHandlerContext context, Object message)
    {
        if (Prev != null) return Prev.Write(context, message);

        // 最后一个处理器，截断
        if (context != null) return context.FireWrite(message);

        return message;
    }

    /// <summary>打开连接</summary>
    /// <param name="context">处理器上下文</param>
    /// <returns>是否成功打开</returns>
    public virtual Boolean Open(IHandlerContext context) => Next == null || Next.Open(context);

    /// <summary>关闭连接（逆序）</summary>
    /// <param name="context">处理器上下文</param>
    /// <param name="reason">关闭原因</param>
    /// <returns>是否成功关闭</returns>
    public virtual Boolean Close(IHandlerContext context, String reason) => Prev == null || Prev.Close(context, reason);

    /// <summary>发生错误</summary>
    /// <param name="context">处理器上下文</param>
    /// <param name="exception">异常对象</param>
    /// <returns>是否已处理错误</returns>
    public virtual Boolean Error(IHandlerContext context, Exception exception) => Next == null || Next.Error(context, exception);
}