using System;

namespace NewLife.Net.Handlers
{
    /// <summary>处理器</summary>
    public interface IHandler
    {
        /// <summary>上一个处理器</summary>
        IHandler Prev { get; set; }

        /// <summary>下一个处理器</summary>
        IHandler Next { get; set; }

        /// <summary>读取数据，返回结果作为下一个处理器消息</summary>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        Object Read(IHandlerContext context, Object message);

        ///// <summary>读取数据完成</summary>
        ///// <param name="context">上下文</param>
        ///// <param name="message">最终消息</param>
        //Object ReadComplete(IHandlerContext context, Object message);

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
    public class Handler : IHandler
    {
        /// <summary>上一个处理器</summary>
        public IHandler Prev { get; set; }

        /// <summary>下一个处理器</summary>
        public IHandler Next { get; set; }

        /// <summary>读取数据，返回结果作为下一个处理器消息</summary>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        public virtual Object Read(IHandlerContext context, Object message) => Next == null ? message : Next.Read(context, message);

        ///// <summary>读取数据完成</summary>
        ///// <param name="context">上下文</param>
        ///// <param name="message">最终消息</param>
        //public virtual Object ReadComplete(IHandlerContext context, Object message) => Next?.ReadComplete(context, message);

        /// <summary>写入数据，返回结果作为下一个处理器消息</summary>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        public virtual Object Write(IHandlerContext context, Object message) => Prev == null ? message : Prev.Write(context, message);

        /// <summary>打开连接</summary>
        /// <param name="context">上下文</param>
        public virtual Boolean Open(IHandlerContext context) => Next == null ? true : Next.Open(context);

        /// <summary>关闭连接</summary>
        /// <param name="context">上下文</param>
        /// <param name="reason">原因</param>
        public virtual Boolean Close(IHandlerContext context, String reason) => Next == null ? true : Next.Close(context, reason);

        /// <summary>发生错误</summary>
        /// <param name="context">上下文</param>
        /// <param name="exception">异常</param>
        public virtual Boolean Error(IHandlerContext context, Exception exception) => Next == null ? true : Next.Error(context, exception);
    }
}