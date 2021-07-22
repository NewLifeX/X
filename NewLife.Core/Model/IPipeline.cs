using System;
using System.Collections.Generic;
using System.Linq;

namespace NewLife.Model
{
    /// <summary>管道。进站顺序，出站逆序</summary>
    public interface IPipeline
    {
        #region 属性
        #endregion

        #region 基础方法
        /// <summary>添加处理器到末尾</summary>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        void Add(IHandler handler);
        #endregion

        #region 执行逻辑
        /// <summary>读取数据，返回结果作为下一个处理器消息</summary>
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
        #endregion
    }

    /// <summary>管道。进站顺序，出站逆序</summary>
    public class Pipeline : IPipeline
    {
        #region 属性
        /// <summary>处理器集合</summary>
        public IList<IHandler> Handlers { get; } = new List<IHandler>();
        #endregion

        #region 构造
        #endregion

        #region 方法
        /// <summary>添加处理器到末尾</summary>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        public virtual void Add(IHandler handler)
        {
            handler.Next = null;
            handler.Prev = null;

            var hs = Handlers;
            if (hs.Count > 0)
            {
                var last = hs[hs.Count - 1];
                last.Next = handler;
                handler.Prev = last;
            }
            Handlers.Add(handler);
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
        public virtual Object Read(IHandlerContext context, Object message) => Handlers.FirstOrDefault()?.Read(context, message);

        /// <summary>写入数据，逆序过滤消息，返回结果作为下一个处理器消息</summary>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        public virtual Object Write(IHandlerContext context, Object message) => Handlers.LastOrDefault()?.Write(context, message);

        /// <summary>打开连接</summary>
        /// <param name="context">上下文</param>
        public virtual Boolean Open(IHandlerContext context) => Handlers.FirstOrDefault()?.Open(context) ?? true;

        /// <summary>关闭连接</summary>
        /// <param name="context">上下文</param>
        /// <param name="reason">原因</param>
        public virtual Boolean Close(IHandlerContext context, String reason) => Handlers.FirstOrDefault()?.Close(context, reason) ?? true;

        /// <summary>发生错误</summary>
        /// <param name="context">上下文</param>
        /// <param name="exception">异常</param>
        public virtual Boolean Error(IHandlerContext context, Exception exception) => Handlers.FirstOrDefault()?.Error(context, exception) ?? true;
        #endregion
    }
}