using System;

namespace NewLife.Model
{
    /// <summary>处理器</summary>
    public interface IHandler
    {
        /// <summary>上一个处理器</summary>
        IHandler Prev { get; set; }

        /// <summary>下一个处理器</summary>
        IHandler Next { get; set; }

        /// <summary>读取数据，返回结果作为下一个处理器消息</summary>
        /// <remarks>
        /// 最终处理器决定如何使用消息。
        /// 处理得到单个消息时，调用一次下一级处理器，返回下级结果给上一级；
        /// 处理得到多个消息时，调用多次下一级处理器，返回null给上一级；
        /// </remarks>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        Object Read(IHandlerContext context, Object message);

        /// <summary>写入数据，返回结果作为下一个处理器消息</summary>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        Object Write(IHandlerContext context, Object message);

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
    }

    /// <summary>处理器</summary>
    public abstract class Handler : IHandler
    {
        /// <summary>上一个处理器</summary>
        public IHandler Prev { get; set; }

        /// <summary>下一个处理器</summary>
        public IHandler Next { get; set; }

        /// <summary>读取数据，返回结果作为下一个处理器消息</summary>
        /// <remarks>
        /// 最终处理器决定如何使用消息。
        /// 处理得到单个消息时，调用一次下一级处理器，返回下级结果给上一级；
        /// 处理得到多个消息时，调用多次下一级处理器，返回null给上一级；
        /// </remarks>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        public virtual Object Read(IHandlerContext context, Object message)
        {
            if (Next != null) return Next.Read(context, message);

            // 最后一个处理器，截断
            if (context != null) context.FireRead(message);

            return message;
        }

        /// <summary>写入数据，返回结果作为下一个处理器消息</summary>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        public virtual Object Write(IHandlerContext context, Object message)
        {
            if (Prev != null) return Prev.Write(context, message);

            // 最后一个处理器，截断
            if (context != null) return context.FireWrite(message);

            return message;
        }

        /// <summary>打开连接</summary>
        /// <param name="context">上下文</param>
        public virtual Boolean Open(IHandlerContext context) => Next == null || Next.Open(context);

        /// <summary>关闭连接</summary>
        /// <param name="context">上下文</param>
        /// <param name="reason">原因</param>
        public virtual Boolean Close(IHandlerContext context, String reason) => Next == null || Next.Close(context, reason);

        /// <summary>发生错误</summary>
        /// <param name="context">上下文</param>
        /// <param name="exception">异常</param>
        public virtual Boolean Error(IHandlerContext context, Exception exception) => Next == null || Next.Error(context, exception);
    }
}