using System;
using System.Collections;
using System.Collections.Generic;
using NewLife.Data;

namespace NewLife.Net
{
    /// <summary>管道。进站顺序，出站逆序</summary>
    public interface IPipeline : IEnumerable<IHandler>
    {
        ///// <summary>服务提供者</summary>
        //IServiceProvider Service { get; }

        #region 基础方法
        /// <summary>添加处理器到开头</summary>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        IPipeline AddFirst(IHandler handler);

        /// <summary>添加处理器到末尾</summary>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        IPipeline AddLast(IHandler handler);

        /// <summary>添加处理器到指定名称之前</summary>
        /// <param name="baseHandler">基准处理器</param>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        IPipeline AddBefore(IHandler baseHandler, IHandler handler);

        /// <summary>添加处理器到指定名称之后</summary>
        /// <param name="baseHandler">基准处理器</param>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        IPipeline AddAfter(IHandler baseHandler, IHandler handler);

        /// <summary>删除处理器</summary>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        IPipeline Remove(IHandler handler);

        /// <summary>创建上下文</summary>
        /// <param name="session">远程会话</param>
        /// <returns></returns>
        IHandlerContext CreateContext(ISocketRemote session);
        #endregion

        #region 执行逻辑
        /// <summary>读取数据</summary>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        Boolean Read(IHandlerContext context, Object message);

        /// <summary>写入数据</summary>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        Boolean Write(IHandlerContext context, Object message);

        /// <summary>写入数据</summary>
        /// <param name="session">远程会话</param>
        /// <param name="message">消息</param>
        Boolean FireWrite(ISocketRemote session, Object message);

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
        ///// <summary>服务提供者</summary>
        //public IServiceProvider Service { get; set; }

        /// <summary>处理器集合</summary>
        public IList<IHandler> Handlers { get; } = new List<IHandler>();
        #endregion

        #region 构造
        #endregion

        #region 方法
        /// <summary>添加处理器到开头</summary>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        public virtual IPipeline AddFirst(IHandler handler)
        {
            Handlers.Insert(0, handler);
            return this;
        }

        /// <summary>添加处理器到末尾</summary>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        public virtual IPipeline AddLast(IHandler handler)
        {
            Handlers.Add(handler);
            return this;
        }

        /// <summary>添加处理器到指定名称之前</summary>
        /// <param name="baseHandler">基准处理器</param>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        public virtual IPipeline AddBefore(IHandler baseHandler, IHandler handler)
        {
            var idx = Handlers.IndexOf(baseHandler);
            if (idx > 0) Handlers.Insert(idx, handler);
            return this;
        }

        /// <summary>添加处理器到指定名称之后</summary>
        /// <param name="baseHandler">基准处理器</param>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        public virtual IPipeline AddAfter(IHandler baseHandler, IHandler handler)
        {
            var idx = Handlers.IndexOf(baseHandler);
            if (idx > 0) Handlers.Insert(idx + 1, handler);
            return this;
        }

        /// <summary>删除处理器</summary>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        public virtual IPipeline Remove(IHandler handler)
        {
            Handlers.Remove(handler);
            return this;
        }

        /// <summary>创建上下文</summary>
        /// <param name="session">远程会话</param>
        /// <returns></returns>
        public virtual IHandlerContext CreateContext(ISocketRemote session)
        {
            var context = new HandlerContext
            {
                Pipeline = this,
                Session = session
            };

            return context;
        }
        #endregion

        #region 执行逻辑
        /// <summary>读取数据</summary>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        public virtual Boolean Read(IHandlerContext context, Object message)
        {
            foreach (var handler in Handlers)
            {
                if (!handler.Read(context, message)) return false;

                // 本次结果作为下一次处理对象
                if (context.Result != null) message = context.Result;
            }

            return true;
        }

        /// <summary>写入数据</summary>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        public virtual Boolean Write(IHandlerContext context, Object message)
        {
            // 出站逆序
            for (var i = Handlers.Count - 1; i >= 0; i--)
            {
                var handler = Handlers[i];
                if (!handler.Write(context, message)) return false;

                // 本次结果作为下一次处理对象
                if (context.Result != null) message = context.Result;
            }

            return true;
        }

        /// <summary>写入数据</summary>
        /// <param name="session">远程会话</param>
        /// <param name="message">消息</param>
        public virtual Boolean FireWrite(ISocketRemote session, Object message)
        {
            var ctx = CreateContext(session);
            if (!Write(ctx, message)) return false;

            // 发送一包数据
            if (ctx.Result is Byte[] buf) return session.Send(buf);
            if (ctx.Result is Packet pk) return session.Send(pk);

            // 发送一批数据包
            if (ctx.Result is IEnumerable<Packet> pks)
            {
                foreach (var item in pks)
                {
                    if (!session.Send(item)) return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>打开连接</summary>
        /// <param name="context">上下文</param>
        public virtual Boolean Open(IHandlerContext context)
        {
            foreach (var handler in Handlers)
            {
                if (!handler.Open(context)) return false;
            }

            return true;
        }


        /// <summary>关闭连接</summary>
        /// <param name="context">上下文</param>
        /// <param name="reason">原因</param>
        public virtual Boolean Close(IHandlerContext context, String reason)
        {
            foreach (var handler in Handlers)
            {
                if (!handler.Close(context, reason)) return false;
            }

            return true;
        }


        /// <summary>发生错误</summary>
        /// <param name="context">上下文</param>
        /// <param name="exception">异常</param>
        public virtual Boolean Error(IHandlerContext context, Exception exception)
        {
            foreach (var handler in Handlers)
            {
                if (!handler.Error(context, exception)) return false;
            }

            return true;
        }
        #endregion

        #region 枚举器
        /// <summary>枚举器</summary>
        /// <returns></returns>
        public IEnumerator<IHandler> GetEnumerator() => Handlers.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }
}