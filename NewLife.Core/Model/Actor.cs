using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace NewLife.Model
{
    /// <summary>无锁并行编程模型</summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Actor<T>
    {
        #region 属性
        private BlockingCollection<T> _queue = new BlockingCollection<T>();
        private Task _task;
        #endregion

        #region 方法
        /// <summary>通知开始处理</summary>
        /// <remarks>
        /// 添加消息时自动触发
        /// </remarks>
        public virtual void Start()
        {
            // 启动异步
            if (_task == null)
            {
                lock (_task)
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

        /// <summary>添加消息，驱动内部处理</summary>
        /// <param name="message"></param>
        public virtual void Add(T message)
        {
            _queue.Add(message);

            Start();
        }

        /// <summary>循环消费消息</summary>
        protected virtual void Act()
        {
            while (!_queue.IsCompleted)
            {
                var msg = _queue.Take();
                OnAct(msg);
            }
        }

        /// <summary>处理消息</summary>
        /// <param name="message"></param>
        protected abstract void OnAct(T message);
        #endregion
    }
}