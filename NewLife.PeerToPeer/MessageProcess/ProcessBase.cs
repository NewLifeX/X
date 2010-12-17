using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using NewLife.Web;
using System.IO;
using NewLife.Net.Sockets;
using NewLife.PeerToPeer.Messages;
using NewLife.Messaging;
using System.Threading;
using System.Reflection;

//namespace NewLife.PeerToPeer.Messages
//{

//    /// <summary>
//    /// 消息处理基类
//    /// </summary>
//    public abstract class ProcessBase : IMessageHandler
//    {

//        #region 属性
//        /// <summary>
//        /// 消息唯一编码
//        /// </summary>
//        public int ID
//        {
//            get { return (Int32)MessageType; }
//        }

//        /// <summary>
//        /// 消息类型
//        /// </summary>
//        public abstract MessageTypes MessageType { get; }

//        #endregion


//        #region 构造
//        /// <summary>
//        /// 静态构造函数
//        /// </summary>
//        static ProcessBase()
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

//            Type[] ts = Assembly.GetExecutingAssembly().GetTypes();
//            List<Type> list = new List<Type>();
//            foreach (Type item in ts)
//            {
//                if (!item.IsClass || item.IsAbstract) continue;

//                if (typeof(ProcessBase).IsAssignableFrom(item)) list.Add(item);
//            }
//            if (list == null || list.Count < 1) return;
//            foreach (Type item in list)
//            {
//                ProcessBase msg = Activator.CreateInstance(item) as ProcessBase;
//                MessageHandler.Register(msg.ID, msg, false);
//            }
//        }
//        #endregion


//        #region IMessageHandler 成员

//        /// <summary>
//        /// 创建消息
//        /// </summary>
//        /// <returns></returns>
//        Message IMessageHandler.Create(int messageID)
//        {
//            return null;
//        }

//        Stream IMessageHandler.Process(Message message, Stream stream)
//        {
//            return Process(message, stream);
//        }

//        /// <summary>
//        /// 处理方法
//        /// </summary>
//        /// <param name="message"></param>
//        /// <param name="stream"></param>
//        /// <returns></returns>
//        protected abstract Stream Process(Message message, Stream stream);

//        bool IMessageHandler.IsReusable
//        {
//            get { return true; }
//        }

//        Object ICloneable.Clone()
//        {
//            return MemberwiseClone();
//        }
//        #endregion


//        #region 辅助方法
//        /// <summary>
//        /// 获取客户端IPEndPoint
//        /// </summary>
//        /// <param name="stream"></param>
//        /// <returns></returns>
//        public static IPEndPoint GetEndPoint(Stream stream)
//        {
//            IPAddress address = IPAddress.Any;
//            if (stream is HttpStream)
//            {
//                String ip = (stream as HttpStream).Context.Request.UserHostAddress;

//                IPAddress.TryParse(ip, out address);

//                return new IPEndPoint(address, 0);
//            }
//            else if (stream is SocketStream)
//            {
//                return (stream as SocketStream).RemoteEndPoint;
//            }

//            return new IPEndPoint(address, 0);
//        }
//        #endregion

//    }
//}