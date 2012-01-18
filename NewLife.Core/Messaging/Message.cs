using System;
using System.IO;
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
        /// <summary>消息类型</summary>
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
            // 搜索已加载程序集里面的消息类型
            foreach (var item in AssemblyX.FindAllPlugins(typeof(Message), true))
            {
                var msg = TypeX.CreateInstance(item) as Message;
                if (msg != null) container.Register(typeof(Message), item, null, msg.Kind);
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
            // 读取了响应类型和消息类型后，动态创建消息对象
            var kind = (MessageKind)reader.ReadByte();
            var type = ObjectContainer.Current.ResolveType<Message>(kind);
            if (type == null) throw new XException("无法识别的消息类型（Kind={0}）！", kind);
            var msg = reader.ReadObject(type) as Message;

            return msg;
        }

        static void Set(ReaderWriterSetting setting)
        {
            //setting.IsBaseFirst = true;
            //setting.UseObjRef = true;
            setting.UseTypeFullName = false;
        }
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
        #endregion

        #region 重载
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("Kind={0} {1}", Kind, GetType().Name);
        }
        #endregion
    }
}