using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using NewLife.IO;

namespace NewLife.Messaging
{
    /// <summary>
    /// 消息基类
    /// </summary>
    public abstract class Message : BinaryAccessor
    {
        #region 属性
        /// <summary>消息唯一编号</summary>
        [XmlIgnore]
        public abstract Int32 ID { get; }
        #endregion

        #region 构造
        static Message()
        {
            // 注册消息的数据流处理器工厂
            StreamHandlerFactory.RegisterFactory(StreamHandlerFactoryName, new MessageStreamHandlerFactory());
        }

        /// <summary>
        /// 数据流工厂名称
        /// </summary>
        public const String StreamHandlerFactoryName = "Message";
        #endregion

        #region 序列化/反序列化
        /// <summary>
        /// 序列化当前消息到流中
        /// </summary>
        /// <param name="stream"></param>
        public void Serialize(Stream stream)
        {
            if (ID <= 0) throw new ArgumentOutOfRangeException("ID", "消息唯一编码" + ID + "无效。");

            BinaryWriterX writer = new BinaryWriterX(stream);
            // 基类写入编号，保证编号在最前面
            writer.WriteEncoded(ID);
            Write(writer);
        }

        /// <summary>
        /// 序列化为字节数组
        /// </summary>
        /// <returns></returns>
        public Byte[] ToArray()
        {
            MemoryStream stream = new MemoryStream();
            Serialize(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static Message Deserialize(Stream stream)
        {
            BinaryReaderX reader = new BinaryReaderX(stream);
            // 读取了响应类型和消息类型后，动态创建消息对象
            Int32 id = reader.ReadEncodedInt32();
            if (id <= 0) throw new Exception("无效的消息唯一编码" + id);

            Message msg = Create(id);
            msg.Read(reader);
            if (id != msg.ID) throw new Exception("反序列化后的消息唯一编码不匹配。");

            return msg;
        }

        ///// <summary>
        ///// 已重载。
        ///// </summary>
        ///// <param name="writer"></param>
        //public override void Write(BinaryWriterX writer)
        //{
        //    base.Write(writer);
        //}
        #endregion

        #region 消息类型对应
        static Dictionary<Int32, IMessageFactory> maps = new Dictionary<Int32, IMessageFactory>();
        /// <summary>
        /// 注册消息工厂，返回原来的消息工厂类型
        /// </summary>
        /// <param name="id"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static IMessageFactory RegisterFactory(Int32 id, IMessageFactory factory)
        {
            lock (maps)
            {
                if (maps.ContainsKey(id))
                {
                    IMessageFactory mf = maps[id];
                    maps[id] = factory;
                    return mf;
                }
                else
                {
                    maps.Add(id, factory);
                    return factory;
                }
            }
        }

        /// <summary>
        /// 根据消息编号创建消息实例
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Message Create(Int32 id)
        {
            IMessageFactory mf = maps[id];
            if (mf == null) throw new InvalidOperationException("未注册的消息" + id);

            return mf.Create(id);
        }

        /// <summary>
        /// 根据消息编号创建消息处理器
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static IMessageHandler CreateHandler(Int32 id)
        {
            IMessageFactory mf = maps[id];
            if (mf == null) throw new InvalidOperationException("未注册的消息" + id);

            return mf.CreateHandler(id);
        }

        /// <summary>
        /// 是否支持指定类型的消息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal static Boolean Support(Int32 id)
        {
            return maps.ContainsKey(id);
        }
        #endregion

        #region 处理流程
        /// <summary>
        /// 处理消息
        /// </summary>
        /// <param name="stream"></param>
        public static void Process(Stream stream) { Process(stream, MessageExceptionOption.Ignore); }

        /// <summary>
        /// 处理消息
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="option"></param>
        public static void Process(Stream stream, MessageExceptionOption option)
        {
            switch (option)
            {
                case MessageExceptionOption.Ignore:
                    try
                    {
                        ProcessInternal(stream);
                    }
                    catch { }
                    break;
                case MessageExceptionOption.Throw:
                    ProcessInternal(stream);
                    break;
                case MessageExceptionOption.SaveAsMessage:
                    try
                    {
                        ProcessInternal(stream);
                    }
                    catch (Exception ex)
                    {
                        ExceptionMessage message = new ExceptionMessage(ex);
                        message.Serialize(stream);
                    }
                    break;
                default:
                    break;
            }
        }

        static void ProcessInternal(Stream stream)
        {
            Message message = Deserialize(stream);
            IMessageHandler handler = CreateHandler(message.ID);
            if (handler != null)
                handler.Process(message, stream);
            else
            {
                // 事件毕竟是很糟糕的设计，将来不能这么做
                //if (_Received != null) _Received(null, new EventArgs<Message, Stream>(message, stream));
                if (Received != null) Received(null, new EventArgs<Message, Stream>(message, stream));
            }
        }

        //private static event EventHandler<EventArgs<Message, Stream>> _Received;
        ///// <summary>
        ///// 消息到达时触发
        ///// </summary>
        //public static event EventHandler<EventArgs<Message, Stream>> Received
        //{
        //    add
        //    {
        //        if (value != null)
        //        {
        //            WeakEventHandler<EventArgs<Message, Stream>> weakHandler = new WeakEventHandler<EventArgs<Message, Stream>>(value, delegate(EventHandler<EventArgs<Message, Stream>> handler) { _Received -= handler; }, false);
        //            //_Received += weakHandler;
        //            weakHandler.Combine(ref _Received);
        //        }
        //    }
        //    remove
        //    {
        //        if (value != null)
        //        {
        //            //_Received -= value;
        //            WeakEventHandler<EventArgs<Message, Stream>>.Remove(ref _Received, value);
        //        }
        //    }
        //}

        /// <summary>
        /// 消息到达时触发
        /// </summary>
        public static event EventHandler<EventArgs<Message, Stream>> Received;
        #endregion

        #region 重载
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("ID={0}", ID);
        }
        #endregion
    }
}