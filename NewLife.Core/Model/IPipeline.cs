namespace NewLife.Model;

/// <summary>管道。进站顺序，出站逆序</summary>
public interface IPipeline
{
    #region 属性
    #endregion

    #region 基础方法
    /// <summary>添加处理器到末尾</summary>
    /// <param name="handler">处理器</param>
    /// <returns></returns>
    void Add(IPipelineHandler handler);

    /// <summary>移除处理器</summary>
    /// <param name="handler">处理器</param>
    /// <returns>是否成功</returns>
    Boolean Remove(IPipelineHandler handler);

    /// <summary>清空所有处理器</summary>
    void Clear();
    #endregion

    #region 执行逻辑
    /// <summary>读取数据，返回结果作为下一个处理器消息</summary>
    /// <param name="context">上下文</param>
    /// <param name="message">消息</param>
    Object? Read(IHandlerContext context, Object message);

    /// <summary>写入数据，返回结果作为下一个处理器消息</summary>
    /// <param name="context">上下文</param>
    /// <param name="message">消息</param>
    Object? Write(IHandlerContext context, Object message);

    /// <summary>打开连接</summary>
    /// <param name="context">上下文</param>
    Boolean Open(IHandlerContext context);

    /// <summary>关闭连接</summary>
    /// <param name="context">上下文</param>
    /// <param name="reason">原因</param>
    Boolean Close(IHandlerContext context, String reason);

    /// <summary>发生错误</summary>
    /// <param name="context">上下文</param>
    /// <param name="exception">异常</param>
    Boolean Error(IHandlerContext context, Exception exception);
    #endregion
}

/// <summary>管道。进站顺序，出站逆序</summary>
public class Pipeline : IPipeline
{
    #region 字段
    private readonly Object _syncRoot = new();
    private IPipelineHandler? _head;
    private IPipelineHandler? _tail;
    #endregion

    #region 属性
    /// <summary>处理器集合</summary>
    public IList<IPipelineHandler> Handlers { get; } = [];

    /// <summary>头部处理器</summary>
    public IPipelineHandler? Head => _head;

    /// <summary>尾部处理器</summary>
    public IPipelineHandler? Tail => _tail;
    #endregion

    #region 构造
    #endregion

    #region 方法
    /// <summary>添加处理器到末尾</summary>
    /// <param name="handler">处理器</param>
    /// <returns></returns>
    public virtual void Add(IPipelineHandler handler)
    {
        if (handler == null) return;

        lock (_syncRoot)
        {
            handler.Next = null;
            handler.Prev = null;

            var last = _tail;
            if (last != null)
            {
                last.Next = handler;
                handler.Prev = last;
            }

            Handlers.Add(handler);

            if (_head == null) _head = handler;
            _tail = handler;
        }
    }

    /// <summary>移除处理器</summary>
    /// <param name="handler">处理器</param>
    /// <returns>是否成功</returns>
    public virtual Boolean Remove(IPipelineHandler handler)
    {
        if (handler == null) return false;

        lock (_syncRoot)
        {
            if (!Handlers.Remove(handler)) return false;

            var prev = handler.Prev;
            var next = handler.Next;

            if (prev != null) prev.Next = next;
            if (next != null) next.Prev = prev;

            if (_head == handler) _head = next;
            if (_tail == handler) _tail = prev;

            handler.Prev = null;
            handler.Next = null;

            return true;
        }
    }

    /// <summary>清空所有处理器</summary>
    public virtual void Clear()
    {
        lock (_syncRoot)
        {
            // 断开链表，避免悬挂引用
            foreach (var h in Handlers)
            {
                h.Prev = null;
                h.Next = null;
            }

            Handlers.Clear();
            _head = null;
            _tail = null;
        }
    }
    #endregion

    #region 执行逻辑
    /// <summary>读取数据，顺序过滤消息，返回结果作为下一个处理器消息</summary>
    /// <remarks>
    /// 最终处理器决定如何使用消息。
    /// 处理得到单个消息时，调用一次下一级处理器，返回下级结果给上一级；
    /// 处理得到多个消息时，调用多次下一级处理器，返回null给上一级；
    /// </remarks>
    /// <param name="context">上下文</param>
    /// <param name="message">消息</param>
    public virtual Object? Read(IHandlerContext context, Object message)
    {
        var head = _head;
        if (head == null)
        {
            // 空管道：直接触发最终处理并返回原消息
            context?.FireRead(message);
            return message;
        }

        return head.Read(context, message);
    }

    /// <summary>写入数据，逆序过滤消息，返回结果作为下一个处理器消息</summary>
    /// <param name="context">上下文</param>
    /// <param name="message">消息</param>
    public virtual Object? Write(IHandlerContext context, Object message)
    {
        var tail = _tail;
        if (tail == null)
        {
            // 空管道：直接写出
            return context != null ? context.FireWrite(message) : message;
        }

        return tail.Write(context, message);
    }

    /// <summary>打开连接（正序）</summary>
    /// <param name="context">上下文</param>
    public virtual Boolean Open(IHandlerContext context)
    {
        var head = _head;
        if (head == null) return true;

        return head.Open(context);
    }

    /// <summary>关闭连接（逆序）</summary>
    /// <param name="context">上下文</param>
    /// <param name="reason">原因</param>
    public virtual Boolean Close(IHandlerContext context, String reason)
    {
        var tail = _tail;
        if (tail == null) return true;

        return tail.Close(context, reason);
    }

    /// <summary>发生错误</summary>
    /// <param name="context">上下文</param>
    /// <param name="exception">异常</param>
    public virtual Boolean Error(IHandlerContext context, Exception exception)
    {
        var head = _head;
        return head?.Error(context, exception) ?? true;
    }
    #endregion
}