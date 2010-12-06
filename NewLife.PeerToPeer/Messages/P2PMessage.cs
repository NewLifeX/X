using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using NewLife.Messaging;
using NewLife.Reflection;
using NewLife.IO;
using System.IO;
using NewLife.Web;
using NewLife.Net.Sockets;

namespace NewLife.PeerToPeer.Messages
{
    /// <summary>
    /// P2P消息
    /// </summary>
    public abstract class P2PMessage : Message, IMessageHandler
    {
        #region 属性
        /// <summary>
        /// 消息唯一编码
        /// </summary>
        public override int ID
        {
            get { return (Int32)MessageType; }
        }

        /// <summary>
        /// 消息类型
        /// </summary>
        public abstract MessageTypes MessageType { get; }

        private Guid _Token;
        /// <summary>标识</summary>
        public Guid Token
        {
            get { return _Token; }
            set { _Token = value; }
        }
        #endregion

        #region 构造
        ///// <summary>
        ///// 静态构造函数
        ///// </summary>
        //static P2PMessage()
        //{
        //    Init();
        //}

        /// <summary>
        /// 初始化，用于注册所有消息
        /// </summary>
        public static void Init()
        {
            Type[] ts = Assembly.GetExecutingAssembly().GetTypes();
            List<Type> list = new List<Type>();
            foreach (Type item in ts)
            {
                if (!item.IsClass || item.IsAbstract) continue;

                if (typeof(P2PMessage).IsAssignableFrom(item)) list.Add(item);
            }
            if (list == null || list.Count < 1) return;
            foreach (Type item in list)
            {
                P2PMessage msg = Activator.CreateInstance(item) as P2PMessage;
                MessageHandler.Register(msg.ID, msg, false);
            }
        }
        #endregion

        #region IMessageHandler 成员
        private Message _Message;
        Message IMessageHandler.Create(int messageID)
        {
            if (_Message == null) _Message = Create();

            return (_Message as ICloneable).Clone() as Message;
        }

        /// <summary>
        /// 创建消息
        /// </summary>
        /// <returns></returns>
        protected abstract P2PMessage Create();

        void IMessageHandler.Process(Message message, Stream stream)
        {
            throw new NotImplementedException();
        }

        bool IMessageHandler.IsReusable
        {
            get { return true; }
        }

        Object ICloneable.Clone()
        {
            return MemberwiseClone();
        }
        #endregion

        #region 处理消息
        ///// <summary>
        ///// 处理消息，返回处理结果（如果有）
        ///// </summary>
        ///// <param name="remoteEP"></param>
        ///// <returns></returns>
        //public virtual P2PMessage Process(EndPoint remoteEP) { return null; }
        #endregion

        #region 重载
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return MessageType.ToString();
        }

        ///// <summary>
        ///// 已重载。
        ///// </summary>
        ///// <param name="target"></param>
        ///// <param name="writer"></param>
        ///// <param name="member"></param>
        ///// <param name="encodeInt"></param>
        ///// <param name="allowNull"></param>
        //protected override void WriteMember(Object target, BinaryWriterX writer, MemberInfoX member, bool encodeInt, bool allowNull)
        //{
        //    //if (member.Type == typeof(IPEndPoint))
        //    //{
        //    //    IPEndPoint ep = member.GetValue(this) as IPEndPoint;
        //    //    Write(ep, writer, encodeInt, allowNull, member.Member.MemberType == MemberTypes.Property);

        //    //    if (encodeInt)
        //    //        writer.WriteEncoded(ep.Port);
        //    //    else
        //    //        writer.Write(ep.Port);

        //    //    return;
        //    //}

        //    if (member.Type == typeof(IPAddress))
        //    {
        //        IPAddress ip = member.GetValue(target) as IPAddress;
        //        Byte[] buffer = ip.GetAddressBytes();
        //        writer.WriteEncoded(buffer.Length);
        //        writer.Write(buffer);

        //        return;
        //    }

        //    base.WriteMember(target, writer, member, encodeInt, allowNull);
        //}

        ///// <summary>
        ///// 已重载。
        ///// </summary>
        ///// <param name="target"></param>
        ///// <param name="reader"></param>
        ///// <param name="member"></param>
        ///// <param name="encodeInt"></param>
        ///// <param name="allowNull"></param>
        //protected override void ReadMember(Object target, BinaryReaderX reader, MemberInfoX member, bool encodeInt, bool allowNull)
        //{
        //    //if (member.Type == typeof(IPEndPoint))
        //    //{
        //    //    Int32 p = 0;
        //    //    if (!encodeInt)
        //    //        p = reader.ReadInt32();
        //    //    else
        //    //        p = reader.ReadEncodedInt32();
        //    //    Byte[] buffer = reader.ReadBytes(p);

        //    //    IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
        //    //    ep.Address = new IPAddress(buffer);

        //    //    if (encodeInt)
        //    //        ep.Port = reader.ReadEncodedInt32();
        //    //    else
        //    //        ep.Port = reader.ReadInt32();

        //    //    return;
        //    //}

        //    if (member.Type == typeof(IPAddress))
        //    {
        //        Int32 p = 0;
        //        p = reader.ReadEncodedInt32();
        //        Byte[] buffer = reader.ReadBytes(p);

        //        IPAddress address = new IPAddress(buffer);
        //        member.SetValue(target, address);

        //        return;
        //    }

        //    base.ReadMember(target, reader, member, encodeInt, allowNull);
        //}

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected override object CreateInstance(Type type)
        {
            if (type == typeof(IPEndPoint)) return new IPEndPoint(IPAddress.Any, 0);

            return base.CreateInstance(type);
        }

        /// <summary>
        /// 接收消息外理
        /// </summary>
        /// <param name="msg"></param>
        public static void ReceivedMessageProcess(Object sender, EventArgs<Message, Stream> e, Stream outStream)
        {
            P2PMessage msg = e.Arg1 as P2PMessage;
            if (msg == null) return;

            switch (msg.MessageType)
            {
                case MessageTypes.Test:
                    break;
                case MessageTypes.Ping:
                    PingMessage.ReceivedMessageProcess(msg as PingMessage, e);
                    break;
                case MessageTypes.FindTorrent:
                    break;
                case MessageTypes.Text:
                    break;
                default:
                    break;
            }

        }
        #endregion
    }

    /// <summary>
    /// 消息基类
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public abstract class Message<TMessage> : P2PMessage
        where TMessage : Message<TMessage>, new()
    {
        #region 属性
        //private static MessageTypes _MessageType;
        ///// <summary>
        ///// 消息编号
        ///// </summary>
        //public override Int32 ID
        //{
        //    get { return (Int32)_MessageType; }
        //}
        #endregion

        #region 构造
        ///// <summary>
        ///// 静态构造函数，向PeerMessage注册当前消息类型
        ///// </summary>
        //static Message()
        //{
        //    Register();
        //}

        ////private static Int32 hasReg = 0;
        ///// <summary>
        ///// 注册
        ///// </summary>
        //public static void Register()
        //{
        //    //if (Interlocked.CompareExchange(ref hasReg, 1, 0) == 1) return;

        //    //Type type = typeof(TRequest);
        //    //String name = type.Name;
        //    //if (name.EndsWith("Message")) name = name.Substring(0, name.Length - "Message".Length);
        //    //String[] names = Enum.GetNames(typeof(MessageTypes));
        //    //if (Array.IndexOf(names, name) >= 0)
        //    //{
        //    //    MessageTypes mt = (MessageTypes)Enum.Parse(typeof(MessageTypes), name, true);
        //    //    TRequest msg = new TRequest();
        //    //    RegisterFactory((Int32)mt, msg);
        //    //}
        //    //if (Array.IndexOf(names, name + "Response") >= 0)
        //    //{
        //    //    MessageTypes mt = (MessageTypes)Enum.Parse(typeof(MessageTypes), name + "Response", true);
        //    //    TResponse msg = new TResponse();
        //    //    RegisterFactory((Int32)mt, msg);
        //    //}

        //    //String[] names = Enum.GetNames(typeof(MessageTypes));
        //    //foreach (String item in names)
        //    //{
        //    //    Type type = null;
        //    //    if (item.EndsWith("Response"))
        //    //    {
        //    //        String name = item.Substring(0, item.Length - "Response".Length);
        //    //        name += ".Response";
        //    //        type = Assembly.GetCallingAssembly().GetType(name, false);
        //    //    }
        //    //    else
        //    //    {
        //    //        type = Assembly.GetCallingAssembly().GetType(item, false);
        //    //    }
        //    //    if (type == null) continue;

        //    //    MessageTypes mt = (MessageTypes)Enum.Parse(typeof(MessageTypes), item, true);
        //    //    RegisterFactory((Int32)mt, Activator.CreateInstance(type) as IMessageFactory);
        //    //}
        //}
        #endregion

        #region 处理
        ///// <summary>
        ///// 处理消息
        ///// </summary>
        ///// <returns></returns>
        //protected virtual TResponse Process() { return default(TResponse); }

        ///// <summary>
        ///// 处理消息
        ///// </summary>
        ///// <param name="stream"></param>
        ///// <returns></returns>
        //public static TResponse Process(Stream stream)
        //{
        //    TMessage request = Deserialize(stream) as TMessage;
        //    TResponse response = request.Process();
        //    response.Token = request.Token;
        //    return response;
        //}
        #endregion

        #region IMessageFactory 成员
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        protected override P2PMessage Create()
        {
            return new TMessage();
        }
        #endregion
    }
}