using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using NewLife.Log;
#if NET4
using System.Linq;
#else
using NewLife.Linq;
#endif
using NewLife.Serialization;

namespace NewLife.Messaging
{
    /// <summary>消息提供者接口</summary>
    /// <remarks>
    /// 同步结构使用<see cref="SendAndReceive"/>；
    /// 异步结构使用<see cref="Send"/>和<see cref="OnReceived"/>；
    /// 异步结构中也可以使用<see cref="SendAndReceive"/>，但是因为通过事件量完成，会极为不稳定。
    /// 
    /// 如果只需要操作某个通道的消息，可通过<see cref="M:IMessageConsumer Register(Byte channel)"/>实现。
    /// 
    /// <see cref="SendAndReceive"/>适合客户端的大多数情况，比如同步Http、同步Tcp。
    /// 如果内部实现是异步模型，则等待指定时间获取异步返回的第一条消息，该消息不再触发消息到达事件<see cref="OnReceived"/>。
    /// </remarks>
    public interface IMessageProvider
    {
        /// <summary>最大消息大小，超过该大小将分包发送。0表示不限制。</summary>
        Int32 MaxMessageSize { get; set; }

        /// <summary>是否自动组合<see cref="GroupMessage"/>消息。</summary>
        Boolean AutoJoinGroup { get; set; }

        /// <summary>组合组消息超时时间，毫秒。对于<see cref="GroupMessage"/>，如果后续包不能在当前时间之内到达，则认为超时，放弃该组。</summary>
        Int32 JoinGroupTimeout { get; set; }

        /// <summary>发送并接收消息。主要用于应答式的请求和响应。该方法的实现不是线程安全的，使用时一定要注意。</summary>
        /// <remarks>如果内部实现是异步模型，则等待指定时间获取异步返回的第一条消息，该消息不再触发消息到达事件<see cref="OnReceived"/>。</remarks>
        /// <param name="message"></param>
        /// <param name="millisecondsTimeout">等待的毫秒数，或为 <see cref="F:System.Threading.Timeout.Infinite" /> (-1)，表示无限期等待。默认0表示不等待</param>
        /// <returns></returns>
        Message SendAndReceive(Message message, Int32 millisecondsTimeout = 0);

        /// <summary>发送消息。如果有响应，可在消息到达事件<see cref="OnReceived"/>中获得。</summary>
        /// <param name="message"></param>
        void Send(Message message);

        /// <summary>消息到达时触发</summary>
        event EventHandler<MessageEventArgs> OnReceived;

        /// <summary>注册消息消费者，仅消费指定范围的消息</summary>
        /// <param name="start">消息范围的起始</param>
        /// <param name="end">消息范围的结束</param>
        /// <returns>消息消费者</returns>
        [Obsolete("请采用消息消费者接口IMessageConsumer Register(Byte channel)！")]
        IMessageProvider Register(MessageKind start, MessageKind end);

        /// <summary>注册消息消费者，仅消费指定范围的消息</summary>
        /// <param name="kinds">消息类型的集合</param>
        /// <returns>消息消费者</returns>
        [Obsolete("请采用消息消费者接口IMessageConsumer Register(Byte channel)！")]
        IMessageProvider Register(MessageKind[] kinds);

        /// <summary>注册消息消费者，仅消费指定通道的消息</summary>
        /// <param name="channel">通道</param>
        /// <returns>消息消费者</returns>
        IMessageConsumer Register(Byte channel);
    }

    /// <summary>消息消费接口</summary>
    public interface IMessageConsumer
    {
        /// <summary>发送并接收消息。主要用于应答式的请求和响应。该方法的实现不是线程安全的，使用时一定要注意。</summary>
        /// <remarks>如果内部实现是异步模型，则等待指定时间获取异步返回的第一条消息，该消息不再触发消息到达事件<see cref="OnReceived"/>。</remarks>
        /// <param name="message"></param>
        /// <param name="millisecondsTimeout">等待的毫秒数，或为 <see cref="F:System.Threading.Timeout.Infinite" /> (-1)，表示无限期等待。默认0表示不等待</param>
        /// <returns></returns>
        Message SendAndReceive(Message message, Int32 millisecondsTimeout = 0);

        /// <summary>发送消息。如果有响应，可在消息到达事件<see cref="OnReceived"/>中获得。</summary>
        /// <param name="message"></param>
        void Send(Message message);

        /// <summary>消息到达时触发</summary>
        event EventHandler<MessageEventArgs> OnReceived;

        /// <summary>通道</summary>
        Byte Channel { get; }

        /// <summary>消息提供者</summary>
        IMessageProvider Provider { get; }
    }

    /// <summary>消息事件参数</summary>
    public class MessageEventArgs : EventArgs
    {
        private Message _Message;
        /// <summary>消息</summary>
        public Message Message { get { return _Message; } set { _Message = value; } }

        /// <summary>实例化</summary>
        /// <param name="message"></param>
        public MessageEventArgs(Message message) { Message = message; }
    }

    interface IMessageProvider2 : IMessageProvider
    {
        /// <summary>收到消息时调用该方法</summary>
        /// <param name="message">消息</param>
        /// <param name="remoteIdentity">远程标识</param>
        void Process(Message message, Object remoteIdentity = null);
    }

    /// <summary>消息提供者基类</summary>
    public abstract class MessageProvider : DisposeBase, IMessageProvider2
    {
        #region 属性
        private Int32 _MaxMessageSize;
        /// <summary>最大消息大小，超过该大小将分包发送。0表示不限制。</summary>
        public Int32 MaxMessageSize { get { return _MaxMessageSize; } set { _MaxMessageSize = value; } }

        private Boolean _AutoJoinGroup;
        /// <summary>是否自动组合<see cref="GroupMessage"/>消息。</summary>
        public Boolean AutoJoinGroup { get { return _AutoJoinGroup; } set { _AutoJoinGroup = value; } }

        private Int32 _JoinGroupTimeout;
        /// <summary>组合组消息超时时间，毫秒。对于<see cref="GroupMessage"/>，如果后续包不能在当前时间之内到达，则认为超时，放弃该组。</summary>
        public Int32 JoinGroupTimeout { get { return _JoinGroupTimeout; } set { _JoinGroupTimeout = value; } }

        private IMessageProvider _Parent;
        /// <summary>消息提供者</summary>
        public IMessageProvider Parent { get { return _Parent; } set { _Parent = value; } }

        private MessageKind[] _Kinds;
        /// <summary>响应的消息类型集合</summary>
        public MessageKind[] Kinds { get { return _Kinds; } set { _Kinds = value; } }
        #endregion

        #region 基本收发
        /// <summary>发送消息。如果有响应，可在消息到达事件<see cref="OnReceived"/>中获得。这里会实现大消息分包。</summary>
        /// <param name="message"></param>
        public virtual void Send(Message message)
        {
            var ms = message.GetStream();
            WriteLog("发送消息 [{0}] {1}", ms.Length, message);
            if (MaxMessageSize <= 0 || ms.Length < MaxMessageSize)
                OnSend(ms);
            else
            {
                var mg = new MessageGroup();
                mg.Split(ms, MaxMessageSize, message.Header);
                var count = 0;
                foreach (var item in mg)
                {
                    if (item.Index == 1) count = item.Count;
                    ms = item.GetStream();
                    WriteLog("发送分组 Identity={0} {1}/{2} [{3}] [{4}]", item.Identity, item.Index, count, item.Data == null ? 0 : item.Data.Length, ms.Length);
                    Debug.Assert(item.Index == count || ms.Length == MaxMessageSize, "分拆的组消息大小不合适！");
                    OnSend(ms);
                }
            }
        }

        /// <summary>发送数据流。</summary>
        /// <param name="stream"></param>
        protected abstract void OnSend(Stream stream);

        /// <summary>收到消息时调用该方法</summary>
        /// <param name="stream">数据流</param>
        /// <param name="state">用户状态</param>
        /// <param name="remoteIdentity">远程标识</param>
        public virtual void Process(Stream stream, Object state, Object remoteIdentity = null)
        {
            var len = 0L;
            // 如果大小大于一个数据包大小，就认为这有一个完整的数据包
            while ((len = stream.Length - stream.Position) > 0)
            {
                // 只有数据流大小小于包大小时，才忽略异常
                var msg = Message.Read(stream, RWKinds.Binary, MaxMessageSize > 0 && len < MaxMessageSize);
                // 如果返回空，表示不是完整的消息
                if (msg == null)
                {
                    //XTrace.WriteLine("数据流中无法读取消息 {0},{1}=>{2}", stream.Position, stream.Length, stream.Length - stream.Position);
                    break;
                }
                if (msg is CompressionMessage) msg = (msg as CompressionMessage).Message;

                msg.UserState = state;
                Process(msg, remoteIdentity);
            }
        }

        /// <summary>收到消息时调用该方法</summary>
        /// <param name="message">消息</param>
        /// <param name="remoteIdentity">远程标识</param>
        public virtual void Process(Message message, Object remoteIdentity = null)
        {
            if (message == null) return;

            // 检查消息范围
            if (Kinds != null && Array.IndexOf<MessageKind>(Kinds, message.Kind) < 0) return;

            if (message.Kind == MessageKind.Group && AutoJoinGroup)
            {
                message = JoinGroup(message as GroupMessage);
                // 如果为空，表明还没完成组合，直接返回
                if (message == null) return;
            }

            WriteLog("接收消息 {0}", message);

            // 为Receive准备的事件，只用一次
            EventHandler<MessageEventArgs> handler;
            do
            {
                handler = innerOnReceived;
            }
            while (handler != null && Interlocked.CompareExchange<EventHandler<MessageEventArgs>>(ref innerOnReceived, null, handler) != handler);

            if (handler != null) handler(this, new MessageEventArgs(message));

            if (OnReceived != null) OnReceived(this, new MessageEventArgs(message));

            // 记录已过期的，要删除
            var list = new List<WeakReference<IMessageProvider2>>();
            var cs = Consumers;
            foreach (var item in cs)
            {
                IMessageProvider2 mp;
                if (item.TryGetTarget(out mp) && mp != null)
                    mp.Process(message);
                else
                    list.Add(item);
            }

            if (list.Count > 0)
            {
                lock (cs)
                {
                    foreach (var item in list)
                    {
                        if (cs.Contains(item)) cs.Remove(item);
                    }
                }
            }

            // 记录已过期的，要删除
            var list2 = new List<WeakReference<MessageConsumer2>>();
            var cs2 = Consumers2;
            foreach (var item in cs2)
            {
                MessageConsumer2 mp;
                if (item.TryGetTarget(out mp) && mp != null)
                    mp.Process(message);
                else
                    list2.Add(item);
            }

            if (list2.Count > 0)
            {
                lock (cs2)
                {
                    foreach (var item in list2)
                    {
                        if (cs2.Contains(item)) cs2.Remove(item);
                    }
                }
            }
        }

        private Dictionary<String, MessageGroup> groups = new Dictionary<String, MessageGroup>();
        /// <summary>组合组消息</summary>
        /// <param name="message">消息</param>
        /// <param name="remoteIdentity">远程标识</param>
        /// <returns></returns>
        protected virtual Message JoinGroup(GroupMessage message, Object remoteIdentity = null)
        {
            var key = String.Format("{0}#{1}", remoteIdentity, message.Identity);

            MessageGroup mg = null;
            if (!groups.TryGetValue(key, out mg))
            {
                mg = new MessageGroup();
                mg.Identity = message.Identity;
                groups.Add(key, mg);
            }

            // 加入到组，如果返回false，表示未收到所有消息
            if (!mg.Add(message))
            {
                WriteLog("接收分组 Identity={0} {1}/{2} [{3}] 已完成：{4}/{5}", message.Identity, message.Index, message.Count, message.Data == null ? 0 : message.Data.Length, mg.Count, mg.Total);

                return null;
            }

            WriteLog("接收分组 Identity={0} {1}/{2} [{3}] 已完成：{4}/{5}", message.Identity, message.Index, message.Count, message.Data == null ? 0 : message.Data.Length, mg.Count, mg.Total);

            // 否则，表示收到所有消息
            groups.Remove(key);

            // 读取真正的消息
            return mg.GetMessage();
        }

        /// <summary>消息到达时触发。这里将得到所有消息</summary>
        public event EventHandler<MessageEventArgs> OnReceived;
        event EventHandler<MessageEventArgs> innerOnReceived;

        /// <summary>发送并接收消息。主要用于应答式的请求和响应。</summary>
        /// <remarks>如果内部实现是异步模型，则等待指定时间获取异步返回的第一条消息，该消息不再触发消息到达事件<see cref="OnReceived"/>。</remarks>
        /// <param name="message"></param>
        /// <param name="millisecondsTimeout">等待的毫秒数，或为 <see cref="F:System.Threading.Timeout.Infinite" /> (-1)，表示无限期等待。默认0表示不等待</param>
        /// <returns></returns>
        public virtual Message SendAndReceive(Message message, Int32 millisecondsTimeout = 0)
        {
            Send(message);

            var _wait = new AutoResetEvent(true);
            _wait.Reset();

            Message msg = null;
            innerOnReceived += (s, e) => { msg = e.Message; _wait.Set(); };

            //if (!_wait.WaitOne(millisecondsTimeout, true)) return null;

            _wait.WaitOne(millisecondsTimeout, false);
            _wait.Close();

            return msg;
        }
        #endregion

        #region 新的消费者模型
        private List<WeakReference<MessageConsumer2>> _Consumers2 = new List<WeakReference<MessageConsumer2>>();
        /// <summary>消费者集合</summary>
        private List<WeakReference<MessageConsumer2>> Consumers2 { get { return _Consumers2; } }

        /// <summary>注册消息消费者，仅消费指定通道的消息</summary>
        /// <param name="channel">通道</param>
        /// <returns>消息消费者</returns>
        public virtual IMessageConsumer Register(Byte channel)
        {
            if (channel <= 0 || channel >= 0x80) throw new ArgumentOutOfRangeException("channel", "通道必须在0到128之间！");

            var mc = new MessageConsumer2();
            mc.Channel = channel;
            mc.Provider = this;

            lock (Consumers2)
            {
                Consumers2.Add(mc);
            }
            mc.OnDisposed += (s, e) => { lock (Consumers2) { Consumers2.Remove(s as MessageConsumer2); } };

            return mc;
        }

        class MessageConsumer2 : DisposeBase, IMessageConsumer
        {
            #region 属性
            private Byte _Channel;
            /// <summary>通道</summary>
            public Byte Channel { get { return _Channel; } set { _Channel = value; } }

            private IMessageProvider _Provider;
            /// <summary>消息提供者</summary>
            public IMessageProvider Provider { get { return _Provider; } set { _Provider = value; } }
            #endregion

            #region 基本收发
            /// <summary>发送消息</summary>
            /// <param name="message"></param>
            public void Send(Message message)
            {
                message.Header.Channel = Channel;
                Provider.Send(message);
            }

            /// <summary>发送并接收消息。主要用于应答式的请求和响应。</summary>
            /// <remarks>如果内部实现是异步模型，则等待指定时间获取异步返回的第一条消息，该消息不再触发消息到达事件<see cref="OnReceived"/>。</remarks>
            /// <param name="message"></param>
            /// <param name="millisecondsTimeout">等待的毫秒数，或为 <see cref="F:System.Threading.Timeout.Infinite" /> (-1)，表示无限期等待。默认0表示不等待</param>
            /// <returns></returns>
            public virtual Message SendAndReceive(Message message, Int32 millisecondsTimeout = 0)
            {
                message.Header.Channel = Channel;
                return Provider.SendAndReceive(message);
            }

            /// <summary>收到消息时调用该方法</summary>
            /// <param name="message"></param>
            public void Process(Message message)
            {
                if (message == null) return;

                // 检查消息范围
                if (!message.Header.UseHeader || message.Header.Channel != Channel) return;

                // 为Receive准备的事件，只用一次
                EventHandler<MessageEventArgs> handler;
                do
                {
                    handler = innerOnReceived;
                }
                while (handler != null && Interlocked.CompareExchange<EventHandler<MessageEventArgs>>(ref innerOnReceived, null, handler) != handler);

                if (handler != null) handler(this, new MessageEventArgs(message));

                if (OnReceived != null) OnReceived(this, new MessageEventArgs(message));
            }

            /// <summary>消息到达时触发。这里将得到所有消息</summary>
            public event EventHandler<MessageEventArgs> OnReceived;
            event EventHandler<MessageEventArgs> innerOnReceived;
            #endregion
        }
        #endregion

        #region 注册消费者
        /// <summary>注册消息消费者，仅消费指定范围的消息</summary>
        /// <param name="start">消息范围的起始</param>
        /// <param name="end">消息范围的结束</param>
        /// <returns>消息消费者</returns>
        [Obsolete("请采用消息消费者接口！")]
        public virtual IMessageProvider Register(MessageKind start, MessageKind end)
        {
            if (start > end) throw new ArgumentOutOfRangeException("start", "起始不能大于结束！");
            //return Register(Enumerable.Range(start, end - start + 1).Select(e => (MessageKind)e).ToArray());
            var list = new List<MessageKind>();
            for (MessageKind i = start; i <= end; i++)
            {
                list.Add(i);
            }
            return Register(list.ToArray());
        }

        /// <summary>注册消息消费者，仅消费指定范围的消息</summary>
        /// <param name="kinds">消息类型的集合</param>
        /// <returns>消息消费者</returns>
        [Obsolete("请采用消息消费者接口！")]
        public virtual IMessageProvider Register(MessageKind[] kinds)
        {
            if (kinds == null || kinds.Length < 1) throw new ArgumentNullException("kinds");
            kinds = kinds.Distinct().OrderBy(e => e).ToArray();
            if (kinds == null || kinds.Length < 1) throw new ArgumentNullException("kinds");

            // 检查注册范围是否有效
            var ks = Kinds;
            if (ks != null)
            {
                foreach (var item in kinds)
                {
                    if (Array.IndexOf<MessageKind>(ks, item) < 0) throw new ArgumentOutOfRangeException("kinds", "当前消息提供者不支持Kind=" + item + "的消息！");
                }
            }

            var mc = new MessageConsumer() { Parent = this, Kinds = kinds };
            lock (Consumers)
            {
                Consumers.Add(mc);
            }
            mc.OnDisposed += (s, e) => Consumers.Remove(s as MessageConsumer);
            return mc;
        }

        private List<WeakReference<IMessageProvider2>> _Consumers = new List<WeakReference<IMessageProvider2>>();
        /// <summary>消费者集合</summary>
        private List<WeakReference<IMessageProvider2>> Consumers { get { return _Consumers; } }
        #endregion

        #region 消息消费者
        class MessageConsumer : MessageProvider
        {
            /// <summary>发送消息</summary>
            /// <param name="message"></param>
            public override void Send(Message message) { Parent.Send(message); }

            /// <summary>发送数据流。</summary>
            /// <param name="stream"></param>
            protected override void OnSend(Stream stream) { }
        }
        #endregion

        #region 日志
        [Conditional("DEBUG")]
        static void WriteLog(String format, params Object[] args)
        {
            if (XTrace.Debug) XTrace.WriteLine(format, args);
        }
        #endregion
    }
}