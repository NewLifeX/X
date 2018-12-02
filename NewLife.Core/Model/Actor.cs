using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace NewLife.Model
{
    /// <summary>无锁并行编程模型</summary>
    public interface IActor
    {
        /// <summary>添加消息，驱动内部处理</summary>
        /// <param name="message">消息</param>
        /// <param name="sender">发送者</param>
        /// <returns>返回待处理消息数</returns>
        Int32 Tell(Object message, IActor sender = null);
    }

    /// <summary>Actor上下文</summary>
    public class ActorContext
    {
        /// <summary>发送者</summary>
        public IActor Sender { get; set; }

        /// <summary>消息</summary>
        public Object Message { get; set; }
    }

    /// <summary>无锁并行编程模型</summary>
    public abstract class Actor : IActor
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>受限容量。最大可堆积的消息数</summary>
        public Int32 BoundedCapacity { get; set; } = Int32.MaxValue;

        /// <summary>存放消息的邮箱</summary>
        protected BlockingCollection<ActorContext> MailBox { get; set; }

        private Task _task;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public Actor() => Name = GetType().Name.TrimEnd("Actor");

        /// <summary>已重载。显示名称</summary>
        /// <returns></returns>
        public override String ToString() => Name;
        #endregion

        #region 方法
        /// <summary>通知开始处理</summary>
        /// <remarks>
        /// 添加消息时自动触发
        /// </remarks>
        public virtual Task Start()
        {
            if (MailBox == null) MailBox = new BlockingCollection<ActorContext>(BoundedCapacity);

            // 启动异步
            if (_task == null)
            {
                lock (this)
                {
#if NET4
                    if (_task == null) _task = TaskEx.Run(() => Loop());
#else
                    if (_task == null) _task = Task.Run(() => Loop());
#endif
                }
            }

            return _task;
        }

        /// <summary>通知停止处理</summary>
        public virtual void Stop() => MailBox.CompleteAdding();

        /// <summary>添加消息，驱动内部处理</summary>
        /// <param name="message">消息</param>
        /// <param name="sender">发送者</param>
        /// <returns>返回待处理消息数</returns>
        public virtual Int32 Tell(Object message, IActor sender = null)
        {
#if DEBUG
            Log.XTrace.WriteLine("[{0}]=>[{1}]：{2}", sender, this, message);
#endif

            var box = MailBox;
            box.Add(new ActorContext { Sender = sender, Message = message });

            return box.Count;
        }

        /// <summary>循环消费消息</summary>
        protected virtual void Loop()
        {
            var box = MailBox;
            while (!box.IsCompleted)
            {
                var ctx = box.Take();
#if DEBUG
                Log.XTrace.WriteLine("[{0}]<=[{1}]：{2}", this, ctx.Sender, ctx.Message);
#endif
                Receive(ctx);
            }
        }

        /// <summary>处理消息</summary>
        /// <param name="context">上下文</param>
        protected abstract void Receive(ActorContext context);
        #endregion
    }
}