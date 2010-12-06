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
    public abstract class Message : BinaryAccessor, ICloneable
    {
        #region 属性
        /// <summary>消息唯一编号</summary>
        [XmlIgnore]
        public abstract Int32 ID { get; }
        #endregion

        #region 构造
        static Message()
        {
            // 注册消息的数据流处理器
            StreamHandler.Register(StreamHandlerName, new MessageStreamHandler(), false);
        }

        /// <summary>
        /// 数据流处理器名称
        /// </summary>
        public const String StreamHandlerName = "Message";
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
            //writer.WriteEncoded(ID);
            writer.Write((Byte)ID);
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
            //Int32 id = reader.ReadEncodedInt32();
            Int32 id = reader.ReadByte();
            if (id <= 0) throw new Exception("无效的消息唯一编码" + id);

            Message msg = MessageHandler.CreateMessage(id);
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

        #region 处理流程
        #endregion

        #region 克隆
        Object ICloneable.Clone()
        {
            return MemberwiseClone();
        }
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