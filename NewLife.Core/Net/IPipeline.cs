using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Net.Handlers;

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
        /// <summary>读取数据，返回结果作为下一个处理器消息</summary>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        Object Read(IHandlerContext context, Object message);

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

        #region 扩展
        //Task<Object> AddQueue(ISocketRemote session, Object message);

        //Boolean Match(ISocketRemote session, Object message, Func<Object, Object, Boolean> callback);
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
        /// <summary>读取数据，返回结果作为下一个处理器消息</summary>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        public virtual Object Read(IHandlerContext context, Object message)
        {
            var rs = message;
            foreach (var handler in Handlers)
            {
                // 需要下一次循环时，才使用上一次结果，避免ReadComplete得不到数据
                message = rs;
                if (message is Byte[] buf) message = new Packet(buf);

                rs = handler.Read(context, message);
                if (rs == null) break;
            }

            // 读取完成
            foreach (var handler in Handlers)
            {
                handler.ReadComplete(context, rs);
            }

            return rs;
        }

        /// <summary>写入数据，返回结果作为下一个处理器消息</summary>
        /// <param name="context">上下文</param>
        /// <param name="message">消息</param>
        public virtual Object Write(IHandlerContext context, Object message)
        {
            // 出站逆序
            for (var i = Handlers.Count - 1; i >= 0; i--)
            {
                //if (message is String str) message = new Packet(str.GetBytes());
                if (message is Byte[] buf) message = new Packet(buf);

                message = Handlers[i].Write(context, message);
                if (message == null) return null;
            }

            return message;
        }

        /// <summary>写入数据</summary>
        /// <param name="session">远程会话</param>
        /// <param name="message">消息</param>
        public virtual Boolean FireWrite(ISocketRemote session, Object message)
        {
            var ctx = CreateContext(session);
            return OnFireWrite(ctx, message);
        }

        /// <summary>写入数据</summary>
        /// <param name="session">远程会话</param>
        /// <param name="message">消息</param>
        public virtual Task<Object> FireWriteAndWait(ISocketRemote session, Object message)
        {
            var ctx = CreateContext(session);
            var source = new TaskCompletionSource<Object>();
            ctx["TaskSource"] = source;

            if (!OnFireWrite(ctx, message)) return Task.FromResult((Object)null);

            return source.Task;
        }

        private Boolean OnFireWrite(IHandlerContext ctx, Object message)
        {
            message = Write(ctx, message);
            if (message == null) return false;

            var session = ctx.Session;

            // 发送一包数据
            if (message is Byte[] buf) return session.Send(buf);
            if (message is Packet pk) return session.Send(pk);
            if (message is String str) return session.Send(str.GetBytes());

            // 发送一批数据包
            if (message is IEnumerable<Packet> pks)
            {
                foreach (var item in pks)
                {
                    if (!session.Send(item)) return false;
                }

                return true;
            }

            throw new XException("无法识别消息[{0}]，可能缺少编码处理器", message?.GetType()?.FullName);
            //return false;
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

        #region 扩展
        //public IPacketQueue Queue { get; set; }

        //public virtual Task<Object> AddQueue(ISocketRemote session, Object message)
        //{
        //    if (Queue == null) Queue = new DefaultPacketQueue();

        //    return Queue.Add(session, message, 15000);
        //}

        //public virtual Boolean Match(ISocketRemote session, Object message, Func<Object, Object, Boolean> callback)
        //{
        //    return Queue.Match(session, message, callback);
        //}
        #endregion

        #region 枚举器
        /// <summary>枚举器</summary>
        /// <returns></returns>
        public IEnumerator<IHandler> GetEnumerator() => Handlers.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }
}