//using System;
//using System.Collections.Generic;
//using System.Text;
//using NewLife.Messaging;
//using System.IO;
//using System.Threading;
//using System.Reflection;

//namespace NewLife.Remoting
//{
//    /// <summary>
//    /// 远程调用消息处理器
//    /// </summary>
//    public class RemotingMessageHandler : MessageHandler
//    {
//        #region 构造
//        /// <summary>
//        /// 静态构造函数
//        /// </summary>
//        static RemotingMessageHandler()
//        {
//            Init();
//        }

//        private static Int32 _Inited = 0;
//        /// <summary>
//        /// 初始化，用于注册所有消息
//        /// </summary>
//        public static void Init()
//        {
//            // 只执行一次，防止多线程冲突
//            if (Interlocked.CompareExchange(ref _Inited, 1, 0) != 0) return;

//            // 注册每一条消息的处理器
//            RemotingMessageHandler handler = new RemotingMessageHandler();
//            foreach (RemotingMessageType item in Enum.GetValues(typeof(RemotingMessageType)))
//            {
//                MessageHandler.Register((Int32)item, handler, false);
//            }
//        }
//        #endregion

//        /// <summary>
//        /// 建立消息
//        /// </summary>
//        /// <param name="messageID"></param>
//        /// <returns></returns>
//        public override Message Create(int messageID)
//        {
//            if (!Enum.IsDefined(typeof(RemotingMessageType), messageID)) return null;

//            RemotingMessageType rmt = (RemotingMessageType)messageID;
//            RemotingMessage message = null;
//            switch (rmt)
//            {
//                case RemotingMessageType.Method:
//                    message = new MethodMessage();
//                    break;
//                case RemotingMessageType.Entity:
//                    message = new EntityMessage();
//                    break;
//                default:
//                    break;
//            }
//            return message;
//        }

//        /// <summary>
//        /// 处理消息
//        /// </summary>
//        /// <param name="message"></param>
//        /// <param name="stream"></param>
//        /// <returns></returns>
//        public override Stream Process(Message message, Stream stream)
//        {
//            RemotingMessage rm = message as RemotingMessage;
//            if (rm != null) ProcessRemoting(rm, stream);
//            return stream;
//        }

//        void ProcessRemoting(RemotingMessage message, Stream stream)
//        {
//            switch (message.MessageType)
//            {
//                case RemotingMessageType.Method:
//                    ProcessMethod(message as MethodMessage, stream);
//                    break;
//                case RemotingMessageType.Entity:
//                    ProcessEntity(message as EntityMessage, stream);
//                    break;
//                default:
//                    break;
//            }
//        }

//        void ProcessMethod(MethodMessage message, Stream stream)
//        {

//        }

//        void ProcessEntity(EntityMessage message, Stream stream)
//        {
//            BinaryWriter writer = new BinaryWriter(stream);
//            String str = String.Format("你要请求的实体是：[{0}]{1}", message.EntityType, message.Entity);
//            writer.Write(str);
//        }
//    }
//}
