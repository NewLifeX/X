using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace NewLife.Model
{
    /// <summary>无锁并行编程模型</summary>
    public abstract class Actor
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>受限容量。最大可堆积的消息数</summary>
        public Int32 BoundedCapacity { get; set; } = Int32.MaxValue;

        /// <summary>存放消息的邮箱</summary>
        protected BlockingCollection<Object> MailBox { get; set; }

        private Task _task;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public Actor()
        {
            Name = GetType().Name.TrimEnd("Actor");
        }

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
            if (MailBox == null) MailBox = new BlockingCollection<Object>(BoundedCapacity);

            // 启动异步
            if (_task == null)
            {
                lock (this)
                {
                    if (_task == null) _task = Task.Run(() => Act());
                }
            }

            return _task;
        }

        /// <summary>通知停止处理</summary>
        public virtual void Stop()
        {
            MailBox.CompleteAdding();
        }

        /// <summary>添加消息，驱动内部处理</summary>
        /// <param name="message">消息</param>
        public virtual Int32 Add(Object message)
        {
#if DEBUG
            Log.XTrace.WriteLine("向[{0}]发布消息：{1}", this, message);
#endif

            var box = MailBox;
            box.Add(message);

            return box.Count;
        }

        /// <summary>循环消费消息</summary>
        protected virtual void Act()
        {
            var box = MailBox;
            while (!box.IsCompleted)
            {
                var msg = box.Take();
#if DEBUG
                Log.XTrace.WriteLine("[{0}]收到消息：{1}", this, msg);
#endif
                OnAct(msg);
            }
        }

        /// <summary>处理消息</summary>
        /// <param name="message"></param>
        protected abstract void OnAct(Object message);
        #endregion
    }
}