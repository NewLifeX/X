using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NewLife.Linq;

namespace NewLife.Messaging
{
    /// <summary>消息提供者接口</summary>
    public interface IMessageProvider
    {
        /// <summary>发送消息</summary>
        /// <param name="message"></param>
        void Send(Message message);

        /// <summary>消息到达时触发。这里将得到所有消息</summary>
        event EventHandler<EventArgs<Message>> OnReceived;

        /// <summary>接收消息。这里将得到所有消息</summary>
        /// <param name="millisecondsTimeout">等待的毫秒数，或为 <see cref="F:System.Threading.Timeout.Infinite" /> (-1)，表示无限期等待。默认0表示不等待</param>
        /// <returns></returns>
        Message Receive(Int32 millisecondsTimeout = 0);

        /// <summary>注册消息消费者，仅消费指定范围的消息</summary>
        /// <param name="start">消息范围的起始</param>
        /// <param name="end">消息范围的结束</param>
        /// <returns>消息消费者</returns>
        IMessageConsumer Register(Byte start, Byte end);

        /// <summary>注册消息消费者，仅消费指定范围的消息</summary>
        /// <param name="kinds">消息类型的集合</param>
        /// <returns>消息消费者</returns>
        IMessageConsumer Register(Byte[] kinds);
    }

    /// <summary>消息消费者接口，仅响应指定范围的消息</summary>
    public interface IMessageConsumer
    {
        /// <summary>发送消息</summary>
        /// <param name="message"></param>
        void Send(Message message);

        /// <summary>接收消息</summary>
        /// <param name="millisecondsTimeout">等待的毫秒数，或为 <see cref="F:System.Threading.Timeout.Infinite" /> (-1)，表示无限期等待。默认0表示不等待</param>
        /// <returns></returns>
        Message Receive(Int32 millisecondsTimeout = 0);

        /// <summary>消息到达时触发</summary>
        event EventHandler OnReceived;
    }

    /// <summary>消息提供者基类</summary>
    public abstract class MessageProvider : DisposeBase, IMessageProvider
    {
        #region 基本收发
        /// <summary>发送消息</summary>
        /// <param name="message"></param>
        public abstract void Send(Message message);

        /// <summary>收到消息时调用该方法</summary>
        /// <param name="message"></param>
        protected virtual void OnReceive(Message message)
        {
            if (message == null) return;

            if (_wait != null)
            {
                _Message = message;
                _wait.Set();
            }

            if (OnReceived != null) OnReceived(this, new EventArgs<Message>(message));

            foreach (var item in Consumers)
            {
                item.OnReceive(message);
            }
        }

        /// <summary>消息到达时触发。这里将得到所有消息</summary>
        public event EventHandler<EventArgs<Message>> OnReceived;

        AutoResetEvent _wait;
        Message _Message;

        /// <summary>接收消息。这里将得到所有消息</summary>
        /// <param name="millisecondsTimeout">等待的毫秒数，或为 <see cref="F:System.Threading.Timeout.Infinite" /> (-1)，表示无限期等待。默认0表示不等待</param>
        /// <returns></returns>
        public virtual Message Receive(Int32 millisecondsTimeout = 0)
        {
            var msg = _Message;
            _Message = null;
            if (msg != null) return msg;

            if (_wait == null) _wait = new AutoResetEvent(true);
            _wait.Reset();

            if (!_wait.WaitOne(millisecondsTimeout, true)) return null;

            msg = _Message;
            _Message = null;
            return msg != null ? msg : null;
        }

        /// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）。
        /// 因为该方法只会被调用一次，所以该参数的意义不太大。</param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            if (_wait != null) _wait.Close();
        }
        #endregion

        #region 注册消费者
        /// <summary>注册消息消费者，仅消费指定范围的消息</summary>
        /// <param name="start">消息范围的起始</param>
        /// <param name="end">消息范围的结束</param>
        /// <returns>消息消费者</returns>
        public virtual IMessageConsumer Register(Byte start, Byte end)
        {
            if (start > end) throw new ArgumentOutOfRangeException("start", "起始不能大于结束！");
            return Register(Enumerable.Range(start, end - start + 1).Select(e => (Byte)e).ToArray());
        }

        /// <summary>注册消息消费者，仅消费指定范围的消息</summary>
        /// <param name="kinds">消息类型的集合</param>
        /// <returns>消息消费者</returns>
        public virtual IMessageConsumer Register(Byte[] kinds)
        {
            if (kinds == null || kinds.Length < 1) throw new ArgumentNullException("kinds");
            kinds = kinds.Distinct().OrderBy(e => e).ToArray();
            if (kinds == null || kinds.Length < 1) throw new ArgumentNullException("kinds");

            var mc = new MessageConsumer() { Provider = this, Kinds = kinds };
            Consumers.Add(mc);
            mc.OnDisposed += (s, e) => Consumers.Remove(s as MessageConsumer);
            return mc;
        }

        private List<MessageConsumer> _Consumers;
        /// <summary>消费者集合</summary>
        private List<MessageConsumer> Consumers { get { return _Consumers ?? (_Consumers = new List<MessageConsumer>()); } set { _Consumers = value; } }
        #endregion

        #region 消息消费者
        class MessageConsumer : DisposeBase, IMessageConsumer
        {
            #region 属性
            private IMessageProvider _Provider;
            /// <summary>消息提供者</summary>
            public IMessageProvider Provider { get { return _Provider; } set { _Provider = value; } }

            private Byte[] _Kinds;
            /// <summary>响应的消息类型集合</summary>
            public Byte[] Kinds { get { return _Kinds; } set { _Kinds = value; } }

            private Queue<Message> _Queue;
            /// <summary>消息队列</summary>
            public Queue<Message> Queue { get { return _Queue ?? (_Queue = new Queue<Message>()); } set { _Queue = value; } }

            AutoResetEvent _wait;
            #endregion

            #region 方法
            /// <summary>发送消息</summary>
            /// <param name="message"></param>
            public void Send(Message message) { Provider.Send(message); }

            public void OnReceive(Message message)
            {
                if (Array.IndexOf(Kinds, message.Kind) < 0) return;

                Queue.Enqueue(message);

                if (_wait != null) _wait.Set();

                if (Queue.Count > 0 && OnReceived != null) OnReceived(this, EventArgs.Empty);
            }

            /// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
            /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）。
            /// 因为该方法只会被调用一次，所以该参数的意义不太大。</param>
            protected override void OnDispose(bool disposing)
            {
                base.OnDispose(disposing);

                if (_wait != null) _wait.Close();
            }
            #endregion

            #region IMessageConsumer 成员

            public Message Receive(int millisecondsTimeout = 0)
            {
                if (Queue.Count > 0) return Queue.Dequeue();
                if (millisecondsTimeout == 0) return null;

                if (_wait == null) _wait = new AutoResetEvent(true);
                _wait.Reset();

                if (!_wait.WaitOne(millisecondsTimeout, true)) return null;

                return Queue.Count > 0 ? Queue.Dequeue() : null;
            }

            public event EventHandler OnReceived;

            #endregion
        }
        #endregion
    }
}