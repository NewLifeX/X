using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Configuration;

namespace NewLife.Messaging
{
    /// <summary>
    /// 消息处理器接口
    /// </summary>
    public interface IMessageHandler : ICloneable
    {
        /// <summary>
        /// 创建指定编号的消息
        /// </summary>
        /// <param name="messageID"></param>
        /// <returns></returns>
        Message Create(Int32 messageID);

        /// <summary>
        /// 处理消息
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="stream">数据流，已经从里面读取消息实体</param>
        /// <returns>转发给下一个处理器的数据流，如果不想让后续处理器处理，返回空</returns>
        Stream Process(Message message, Stream stream);

        /// <summary>
        /// 是否可以重用。
        /// </summary>
        Boolean IsReusable { get; }
    }

    /// <summary>
    /// 消息处理器
    /// </summary>
    public abstract class MessageHandler : IMessageHandler
    {
        #region 接口
        /// <summary>
        /// 创建消息
        /// </summary>
        /// <param name="messageID"></param>
        /// <returns></returns>
        public abstract Message Create(int messageID);

        /// <summary>
        /// 处理消息
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="stream">数据流，已经从里面读取消息实体</param>
        /// <returns></returns>
        public abstract Stream Process(Message message, Stream stream);

        /// <summary>
        /// 是否可以重用
        /// </summary>
        public virtual Boolean IsReusable { get { return false; } }

        Object ICloneable.Clone()
        {
            return MemberwiseClone();
        }
        #endregion

        #region 构造
        static MessageHandler()
        {
            Message.Init();

            LoadConfig();

            ExceptionMessage msg = new ExceptionMessage();
            Register(msg.ID, new DefaultMessageHandler(), false);
        }
        #endregion

        #region 映射
        static Dictionary<Int32, LinkedList<IMessageHandler>> maps = new Dictionary<Int32, LinkedList<IMessageHandler>>();
        /// <summary>
        /// 注册数据流处理器。
        /// 数据流到达时将进入指定通道的每一个处理器。
        /// 不同通道名称的处理器互不干扰。
        /// 不提供注册到指定位置的功能，如果需要，再以多态方式实现。
        /// </summary>
        /// <param name="id">通道名称，用于区分数据流总线</param>
        /// <param name="handler">数据流处理器</param>
        /// <param name="cover">是否覆盖原有同类型处理器</param>
        public static void Register(Int32 id, IMessageHandler handler, Boolean cover)
        {
            LinkedList<IMessageHandler> list = null;

            // 在字典中查找
            if (!maps.TryGetValue(id, out list))
            {
                lock (maps)
                {
                    if (!maps.TryGetValue(id, out list))
                    {
                        list = new LinkedList<IMessageHandler>();
                        maps.Add(id, list);
                    }
                }
            }

            // 修改处理器链表
            lock (list)
            {
                if (list.Contains(handler))
                {
                    if (cover)
                    {
                        // 一个处理器，只用一次，如果原来使用过，需要先移除。
                        // 一个处理器的多次注册，可用于改变处理顺序，使得自己排在更前面。
                        list.Remove(handler);
                        list.AddFirst(handler);
                    }
                }
                else
                {
                    list.AddFirst(handler);
                }
            }
        }

        /// <summary>
        /// 查询注册，返回指定通道的处理器数组。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static IMessageHandler[] QueryRegister(Int32 id)
        {
            if (maps == null || maps.Count < 1) return null;
            LinkedList<IMessageHandler> list = null;
            if (!maps.TryGetValue(id, out list)) return null;
            lock (maps)
            {
                if (!maps.TryGetValue(id, out list)) return null;

                IMessageHandler[] fs = new IMessageHandler[list.Count];
                list.CopyTo(fs, 0);
                return fs;
            }
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

        #region 配置
        const String handlerKey = "NewLife.MessageHandler_";

        /// <summary>
        /// 获取配置文件指定的处理器
        /// </summary>
        /// <returns></returns>
        static Dictionary<Int32, List<Type>> GetHandler()
        {
            NameValueCollection nvs = Config.AppSettings;
            if (nvs == null || nvs.Count < 1) return null;

            Dictionary<Int32, List<Type>> dic = new Dictionary<Int32, List<Type>>();
            // 遍历设置项
            foreach (String appName in nvs)
            {
                // 必须以指定名称开始
                if (!appName.StartsWith(handlerKey, StringComparison.OrdinalIgnoreCase)) continue;

                // 通道名称
                String name = appName.Substring(handlerKey.Length + 1);
                Int32 id = 0;
                if (!Int32.TryParse(name, out id)) throw new InvalidDataException("错误的消息编号" + id + "！");

                String str = nvs[appName];
                if (String.IsNullOrEmpty(str)) continue;

                String[] ss = str.Split(new Char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                //List<Type> list = dic.ContainsKey(id) ? dic[id] : new List<Type>();
                List<Type> list = null;
                if (!dic.TryGetValue(id, out list))
                {
                    list = new List<Type>();
                    dic.Add(id, list);
                }
                foreach (String item in ss)
                {
                    Type type = TypeX.GetType(item, true);
                    list.Add(type);
                }
                //foreach (String item in ss)
                //{
                //    Type type = Type.GetType(item);
                //    list.Add(type);
                //}
            }
            return dic.Count > 0 ? dic : null;
        }

        /// <summary>
        /// 从配置文件中加载工厂
        /// </summary>
        static void LoadConfig()
        {
            try
            {
                Dictionary<Int32, List<Type>> ts = GetHandler();
                if (ts == null || ts.Count < 1) return;

                foreach (Int32 item in ts.Keys)
                {
                    // 倒序。后注册的处理器先处理，为了迎合写在前面的处理器优先处理，故倒序！
                    for (int i = ts[item].Count - 1; i >= 0; i--)
                    {
                        IMessageHandler handler = Activator.CreateInstance(ts[item][i]) as IMessageHandler;
                        Register(item, handler, true);
                    }
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteLine("从配置文件加载消息处理器出错！" + ex.ToString());
            }
        }
        #endregion

        #region 消息
        /// <summary>
        /// 创建指定编号的消息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Message CreateMessage(Int32 id)
        {
            // 复制到数组，以免线程冲突
            IMessageHandler[] fs = QueryRegister(id);
            if (fs == null || fs.Length < 1) throw new InvalidOperationException("没有找到" + id + "的处理器！");

            foreach (IMessageHandler item in fs)
            {
                IMessageHandler handler = item;
                Message msg = handler.Create(id);
                if (msg != null) return msg;
            }

            //return null;
            throw new InvalidDataException("无效的消息唯一编码" + id + "！");
        }
        #endregion

        #region 处理数据流
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
                    catch (Exception ex)
                    {
                        if (XTrace.Debug) XTrace.WriteException(ex);
                    }
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
            Message message = Message.Deserialize(stream);

            IMessageHandler[] fs = QueryRegister(message.ID);

            if (fs != null && fs.Length > 0)
            {
                foreach (IMessageHandler item in fs)
                {
                    IMessageHandler handler = item;
                    if (!handler.IsReusable) handler = item.Clone() as IMessageHandler;
                    stream = handler.Process(message, stream);
                    if (stream == null) break;
                }
            }
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

        #region 核心事件
        /// <summary>
        /// 异常发生时触发
        /// </summary>
        public static event EventHandler<EventArgs<Message, Stream>> Error;

        /// <summary>
        /// 空消息发生时触发
        /// </summary>
        public static event EventHandler<EventArgs<Message, Stream>> Null;

        /// <summary>
        /// 异常消息处理器
        /// </summary>
        class DefaultMessageHandler : MessageHandler
        {
            public override Message Create(int messageID)
            {
                if (messageID == 0xFF) return new ExceptionMessage();
                if (messageID == 0xFE) return new NullMessage();

                return null;
            }

            public override Stream Process(Message message, Stream stream)
            {
                if (message is ExceptionMessage && Error != null) Error(this, new EventArgs<Message, Stream>(message, stream));
                if (message is NullMessage && Null != null) Null(this, new EventArgs<Message, Stream>(message, stream));

                return stream;
            }
        }
        #endregion
    }
}