using System;
using System.IO.Compression;
using System.Xml.Serialization;
using NewLife.Serialization;

namespace NewLife.Messaging
{
    ///// <summary>消息压缩算法</summary>
    //public enum CompressionMethod : byte
    //{
    //    /// <summary>压缩算法</summary>
    //    Deflate
    //}

    /// <summary>经过压缩的消息</summary>
    public class CompressionMessage : Message, IAccessor
    {
        /// <summary>消息类型</summary>
        [XmlIgnore]
        public override MessageKind Kind { get { return MessageKind.Entity; } }

        //private CompressionMethod _Method;
        ///// <summary>压缩算法</summary>
        //public CompressionMethod Method { get { return _Method; } set { _Method = value; } }

        [NonSerialized]
        private Message _Message;
        /// <summary>内部消息对象</summary>
        public Message Message { get { return _Message; } set { _Message = value; } }

        #region IAccessor 成员
        Boolean IAccessor.Read(IReader reader) { return false; }

        Boolean IAccessor.ReadComplete(IReader reader, Boolean success)
        {
            // 读取消息。对剩下的数据流，进行解压缩后，读取成为另一个消息
            using (var stream = new DeflateStream(reader.Stream, CompressionMode.Decompress, true))
            {
                Message = Read(stream);
            }

            return success;
        }

        Boolean IAccessor.Write(IWriter writer) { return false; }

        Boolean IAccessor.WriteComplete(IWriter writer, Boolean success)
        {
            // 写入消息。把消息写入压缩流，压缩后写入到输出流
            using (var stream = new DeflateStream(writer.Stream, CompressionMode.Compress, true))
            {
                Message.Write(stream);
            }

            return success;
        }
        #endregion
    }
}