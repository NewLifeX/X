using System;
using System.Collections.Generic;
using NewLife.Collections;

namespace NewLife.Net
{
    /// <summary>处理器</summary>
    public interface IHandler
    {
        /// <summary>读取数据</summary>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        Boolean Read(IHandlerContext context, Object message);

        /// <summary>写入数据</summary>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        Boolean Write(IHandlerContext context, Object message);

        /// <summary>打开连接</summary>
        /// <param name="context">上下文</param>
        /// <param name="reason">原因</param>
        Boolean Open(IHandlerContext context, String reason);

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
        /// <summary>读取数据</summary>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        public virtual Boolean Read(IHandlerContext context, Object message) => true;

        /// <summary>写入数据</summary>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        public virtual Boolean Write(IHandlerContext context, Object message) => true;

        /// <summary>打开连接</summary>
        /// <param name="context">上下文</param>
        /// <param name="reason">原因</param>
        public virtual Boolean Open(IHandlerContext context, String reason) => true;

        /// <summary>关闭连接</summary>
        /// <param name="context">上下文</param>
        /// <param name="reason">原因</param>
        public virtual Boolean Close(IHandlerContext context, String reason) => true;

        /// <summary>发生错误</summary>
        /// <param name="context">上下文</param>
        /// <param name="exception">异常</param>
        public virtual Boolean Error(IHandlerContext context, Exception exception) => true;
    }
}