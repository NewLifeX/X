using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace NewLife.Model
{
    /// <summary>无锁并行编程模型</summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Actor<T>
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        private BlockingCollection<T> _queue;
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
        /// <param name="boundedCapacity">受限容量。最大可堆积的消息数</param>
        /// <remarks>
        /// 添加消息时自动触发
        /// </remarks>
        public virtual void Start(Int32 boundedCapacity = Int32.MaxValue)
        {
            if (_queue == null) _queue = new BlockingCollection<T>(boundedCapacity);

            // 启动异步
            if (_task == null)
            {
                lock (this)
                {
                    if (_task == null) _task = Task.Run(() => Act());
                }
            }
        }

        /// <summary>通知停止处理</summary>
        public virtual void Stop()
        {
            _queue.CompleteAdding();
        }

        /// <summary>等待任务完成</summary>
        /// <param name="msTimeout"></param>
        /// <returns></returns>
        public virtual Boolean Wait(Int32 msTimeout = -1)
        {
            if (_task == null) return true;

            return _task.Wait(TimeSpan.FromMilliseconds(msTimeout));
        }

        /// <summary>添加消息，驱动内部处理</summary>
        /// <param name="message"></param>
        public virtual void Add(T message)
        {
#if DEBUG
            Log.XTrace.WriteLine("向[{0}]发布消息：{1}", this, message);
#endif

            _queue.Add(message);

            //Start();
        }

        /// <summary>循环消费消息</summary>
        protected virtual void Act()
        {
            while (!_queue.IsCompleted)
            {
                //var msg = _queue.Take();
                if (_queue.TryTake(out var msg, 1_000))
                {
#if DEBUG
                    Log.XTrace.WriteLine("[{0}]收到消息：{1}", this, msg);
#endif
                    OnAct(msg);
                }
            }
        }

        /// <summary>处理消息</summary>
        /// <param name="message"></param>
        protected abstract void OnAct(T message);
        #endregion
    }
}