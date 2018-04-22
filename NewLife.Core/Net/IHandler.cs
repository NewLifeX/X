using System;
using System.Collections.Generic;
using NewLife.Collections;

namespace NewLife.Net
{
    /// <summary>处理器</summary>
    public interface IHandler
    {
        /// <summary>读取数据</summary>
        /// <param name="ctx">上下文</param>
        /// <param name="message">消息</param>
        void Read(HandlerContext ctx, Object message);

        /// <summary>写入数据</summary>
        /// <param name="ctx">上下文</param>
        /// <param name="message">消息</param>
        void Write(HandlerContext ctx, Object message);

        /// <summary>打开连接</summary>
        /// <param name="ctx">上下文</param>
        /// <param name="reason">原因</param>
        void Open(HandlerContext ctx, String reason);

        /// <summary>关闭连接</summary>
        /// <param name="ctx">上下文</param>
        /// <param name="reason">原因</param>
        void Close(HandlerContext ctx, String reason);

        /// <summary>发生错误</summary>
        /// <param name="ctx">上下文</param>
        /// <param name="exception">异常</param>
        void Error(HandlerContext ctx, Exception exception);
    }

    /// <summary>处理器</summary>
    public class Handler : IHandler
    {
        /// <summary>读取数据</summary>
        /// <param name="ctx">上下文</param>
        /// <param name="message">消息</param>
        public virtual void Read(HandlerContext ctx, Object message) { }

        /// <summary>写入数据</summary>
        /// <param name="ctx">上下文</param>
        /// <param name="message">消息</param>
        public virtual void Write(HandlerContext ctx, Object message) { }

        /// <summary>打开连接</summary>
        /// <param name="ctx">上下文</param>
        /// <param name="reason">原因</param>
        public virtual void Open(HandlerContext ctx, String reason) { }

        /// <summary>关闭连接</summary>
        /// <param name="ctx">上下文</param>
        /// <param name="reason">原因</param>
        public virtual void Close(HandlerContext ctx, String reason) { }

        /// <summary>发生错误</summary>
        /// <param name="ctx">上下文</param>
        /// <param name="exception">异常</param>
        public virtual void Error(HandlerContext ctx, Exception exception) { }
    }

    /// <summary>处理器上下文</summary>
    public class HandlerContext
    {
        #region 属性
        /// <summary>管道</summary>
        public IPipeline Pipeline { get; set; }

        /// <summary>远程连接</summary>
        public ISocketRemote Socket { get; set; }

        /// <summary>数据项</summary>
        public IDictionary<String, Object> Items { get; } = new NullableDictionary<String, Object>();

        /// <summary>设置 或 获取 数据项</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Object this[String key] { get => Items[key]; set => Items[key] = value; }
        #endregion
    }
}