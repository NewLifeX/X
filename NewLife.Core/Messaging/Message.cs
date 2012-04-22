using System;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using NewLife.Exceptions;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Messaging
{
    /// <summary>消息实体基类</summary>
    /// <remarks>
    /// 用消息实体来表达行为和数据，更加直观。
    /// 同时，指定一套序列化和反序列化机制，实现消息实体与传输形式（二进制数据、XML、Json）的互相转换。
    /// 
    /// 消息实体仿照Windows消息来设计，拥有一部分系统内置消息，同时运行用户自定义消息
    /// </remarks>
    public abstract class Message
    {
        #region 属性
        [NonSerialized]
        private MessageHeader _Header;
        /// <summary>消息头。</summary>
        [XmlIgnore]
        public MessageHeader Header { get { return _Header ?? (_Header = new MessageHeader()); } set { _Header = value; } }

        /// <summary>消息类型</summary>
        /// <remarks>第一个字节的第一位决定是否存在消息头。</remarks>
        [XmlIgnore]
        public abstract MessageKind Kind { get; }
        #endregion

        #region 构造、注册
        static Message()
        {
            Init();
        }

        /// <summary>初始化</summary>
        static void Init()
        {
            var container = ObjectContainer.Current;
            var asm = Assembly.GetExecutingAssembly();
            // 搜索已加载程序集里面的消息类型
            foreach (var item in AssemblyX.FindAllPlugins(typeof(Message), true))
            {
                var msg = TypeX.CreateInstance(item) as Message;
                if (msg != null)
                {
                    if (item.Assembly != asm && msg.Kind < MessageKind.UserDefine) throw new XException("不允许{0}采用小于{1}的保留编码{2}！", item.FullName, MessageKind.UserDefine, msg.Kind);

                    container.Register(typeof(Message), item, null, msg.Kind);
                }
                //if (msg != null) container.Register<Message>(msg, msg.Kind);
            }
        }
        #endregion

        #region 序列化/反序列化
        /// <summary>序列化当前消息到流中</summary>
        /// <param name="stream"></param>
        public void Write(Stream stream)
        {
            var writer = new BinaryWriterX(stream);
            Set(writer.Settings);

            if (Debug)
            {
                writer.Debug = true;
                writer.EnableTraceStream();
            }

            // 判断并写入消息头
            if (_Header != null && _Header.UseHeader) Header.Write(writer.Stream);

            // 基类写入编号，保证编号在最前面
            writer.Write((Byte)Kind);
            writer.WriteObject(this);
        }

        /// <summary>序列化为数据流</summary>
        /// <returns></returns>
        public Stream GetStream()
        {
            var ms = new MemoryStream();
            Write(ms);
            ms.Position = 0;
            return ms;
        }

        /// <summary>从流中读取消息</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static Message Read(Stream stream)
        {
            var reader = new BinaryReaderX(stream);
            Set(reader.Settings);

            if (Debug)
            {
                reader.Debug = true;
                reader.EnableTraceStream();
            }

            // 检查第一个字节
            var first = (Byte)reader.Reader.PeekChar();
            MessageHeader header = null;
            // 第一个字节的最高位决定是否扩展
            if (MessageHeader.IsValid(first))
            {
                header = new MessageHeader();
                header.Read(reader.Stream);
            }

            // 读取了响应类型和消息类型后，动态创建消息对象
            var kind = (MessageKind)(reader.ReadByte() & 0x0F);
            var type = ObjectContainer.Current.ResolveType<Message>(kind);
            if (type == null) throw new XException("无法识别的消息类型（Kind={0}）！", kind);

            Message msg;
            if (stream.Position == stream.Length)
                msg = TypeX.CreateInstance(type, null) as Message;
            else
            {
                try
                {
                    msg = reader.ReadObject(type) as Message;
                }
                catch (Exception ex) { throw new XException(String.Format("无法从数据流中读取{0}（Kind={1}）消息！{2}", type.Name, kind, ex.Message), ex); }
            }
            msg.Header = header;
            return msg;
        }

        /// <summary>从流中读取消息</summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static TMessage Read<TMessage>(Stream stream) where TMessage : Message
        {
            return Read(stream) as TMessage;
        }

        static void Set(BinarySettings setting)
        {
            //setting.IsBaseFirst = true;
            setting.EncodeInt = true;
            setting.UseObjRef = true;
            setting.UseTypeFullName = false;
        }

        [ThreadStatic]
        private static Boolean _Debug;
        /// <summary>是否调试，输出序列化过程</summary>
        public static Boolean Debug { get { return _Debug; } set { _Debug = value; } }
        #endregion

        #region 方法
        /// <summary>探测消息类型，不移动流指针</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static MessageKind PeekKind(Stream stream)
        {
            Int32 n = stream.ReadByte();
            stream.Seek(-1, SeekOrigin.Current);
            return (MessageKind)n;
        }

        /// <summary>从源消息克隆设置和可序列化成员数据</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public virtual Message CopyFrom(Message msg)
        {
            if (msg != null)
            {
                // 遍历可序列化成员
                foreach (var item in ObjectInfo.GetMembers(this.GetType()))
                {
                    item[this] = item[msg];
                }
            }
            return this;
        }
        #endregion

        #region 重载
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Kind < MessageKind.UserDefine)
                return String.Format("Kind={0}", Kind);
            else
                return String.Format("Kind={0} Type={1}", Kind, this.GetType().Name);
        }
        #endregion
    }
}