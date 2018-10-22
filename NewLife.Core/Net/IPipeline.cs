using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Net.Handlers;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Net
{
    /// <summary>管道。进站顺序，出站逆序</summary>
    public interface IPipeline : IEnumerable<IHandler>
    {
        #region 属性
        /// <summary>头部处理器</summary>
        IHandler Head { get; }

        /// <summary>尾部处理器</summary>
        IHandler Tail { get; }
        #endregion

        #region 基础方法
        /// <summary>添加处理器到开头</summary>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        void AddFirst(IHandler handler);

        /// <summary>添加处理器到末尾</summary>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        void AddLast(IHandler handler);

        /// <summary>添加处理器到指定名称之前</summary>
        /// <param name="baseHandler">基准处理器</param>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        void AddBefore(IHandler baseHandler, IHandler handler);

        /// <summary>添加处理器到指定名称之后</summary>
        /// <param name="baseHandler">基准处理器</param>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        void AddAfter(IHandler baseHandler, IHandler handler);

        /// <summary>删除处理器</summary>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        void Remove(IHandler handler);

        /// <summary>创建上下文</summary>
        /// <param name="session">远程会话</param>
        /// <returns></returns>
        IHandlerContext CreateContext(ISocketRemote session);
        #endregion

        #region 执行逻辑
        /// <summary>读取数据，返回结果作为下一个处理器消息</summary>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        void Read(IHandlerContext context, Object message);

        /// <summary>写入数据，返回结果作为下一个处理器消息</summary>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        Object Write(IHandlerContext context, Object message);

        /// <summary>写入数据</summary>
        /// <param name="session">远程会话</param>
        /// <param name="message">消息</param>
        Boolean FireWrite(ISocketRemote session, Object message);

        /// <summary>写入数据</summary>
        /// <param name="session">远程会话</param>
        /// <param name="message">消息</param>
        Task<Object> FireWriteAndWait(ISocketRemote session, Object message);

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
        /// <summary>头部处理器</summary>
        public IHandler Head { get; set; }

        /// <summary>尾部处理器</summary>
        public IHandler Tail { get; set; }
        #endregion

        #region 构造
        #endregion

        #region 方法
        /// <summary>添加处理器到开头</summary>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        public virtual void AddFirst(IHandler handler)
        {
            if (Head == null)
            {
                handler.Next = null;
                handler.Prev = null;
                Head = handler;
                Tail = handler;
            }
            else
                AddBefore(Head, handler);
        }

        /// <summary>添加处理器到末尾</summary>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        public virtual void AddLast(IHandler handler)
        {
            if (Tail == null)
            {
                handler.Next = null;
                handler.Prev = null;
                Head = handler;
                Tail = handler;
            }
            else
                AddAfter(Tail, handler);
        }

        /// <summary>添加处理器到指定名称之前</summary>
        /// <param name="baseHandler">基准处理器</param>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        public virtual void AddBefore(IHandler baseHandler, IHandler handler)
        {
            handler.Next = baseHandler;
            handler.Prev = baseHandler.Prev;
            if (baseHandler.Prev != null) baseHandler.Prev.Next = handler;
            baseHandler.Prev = handler;

            if (baseHandler == Head) Head = handler;
        }

        /// <summary>添加处理器到指定名称之后</summary>
        /// <param name="baseHandler">基准处理器</param>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        public virtual void AddAfter(IHandler baseHandler, IHandler handler)
        {
            handler.Next = baseHandler.Next;
            handler.Prev = baseHandler;
            if (baseHandler.Next != null) baseHandler.Next.Prev = handler;
            baseHandler.Next = handler;

            if (baseHandler == Tail) Tail = handler;
        }

        /// <summary>删除处理器</summary>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        public virtual void Remove(IHandler handler)
        {
            if (handler.Prev != null)
                handler.Prev.Next = handler.Next;
            else
                Head = handler.Next;

            if (handler.Next != null)
                handler.Next.Prev = handler.Prev;
            else
                Tail = handler.Prev;
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
        /// <summary>读取数据，返回结果作为下一个处理器消息</summary>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        public virtual void Read(IHandlerContext context, Object message)
        {
            // 顺序过滤消息
            var rs = Head?.Read(context, message);
            // 最后结果落实消息
            if (rs != null) context.FireRead(rs);
        }

        /// <summary>写入数据，返回结果作为下一个处理器消息</summary>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        public virtual Object Write(IHandlerContext context, Object message)
        {
            // 逆序过滤消息
            return Tail?.Write(context, message);
        }

        /// <summary>落实写入数据</summary>
        /// <param name="session">远程会话</param>
        /// <param name="message">消息</param>
        public virtual Boolean FireWrite(ISocketRemote session, Object message)
        {
            var ctx = CreateContext(session);
            message = Write(ctx, message);

            return ctx.FireWrite(message);
        }

        /// <summary>落实写入数据</summary>
        /// <param name="session">远程会话</param>
        /// <param name="message">消息</param>
        public virtual Task<Object> FireWriteAndWait(ISocketRemote session, Object message)
        {
            var ctx = CreateContext(session);
            var source = new TaskCompletionSource<Object>();
            ctx["TaskSource"] = source;

            message = Write(ctx, message);

            if (!ctx.FireWrite(message)) return TaskEx.FromResult((Object)null);

            return source.Task;
        }

        /// <summary>打开连接</summary>
        /// <param name="context">上下文</param>
        public virtual Boolean Open(IHandlerContext context)
        {
            if (Head == null) return true;

            return Head.Open(context);
        }

        /// <summary>关闭连接</summary>
        /// <param name="context">上下文</param>
        /// <param name="reason">原因</param>
        public virtual Boolean Close(IHandlerContext context, String reason)
        {
            if (Head == null) return true;

            return Head.Close(context, reason);
        }

        /// <summary>发生错误</summary>
        /// <param name="context">上下文</param>
        /// <param name="exception">异常</param>
        public virtual Boolean Error(IHandlerContext context, Exception exception)
        {
            if (Head == null) return true;

            return Head.Error(context, exception);
        }
        #endregion

        #region 枚举器
        /// <summary>枚举器</summary>
        /// <returns></returns>
        public IEnumerator<IHandler> GetEnumerator()
        {
            for (var h = Head; h != null; h = h.Next)
            {
                yield return h;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }
}