using System.Xml.Serialization;
using System.IO;
using NewLife.Serialization;

namespace NewLife.Messaging
{
    /// <summary>空消息。最短消息，只占一个字节</summary>
    public class NullMessage : Message
    {
        /// <summary>消息类型</summary>
        [XmlIgnore]
        public override MessageKind Kind { get { return MessageKind.Null; } }

        /// <summary>已重载。</summary>
        /// <param name="stream">数据流</param>
        /// <param name="rwkind">序列化类型</param>
        protected override void OnWrite(Stream stream, RWKinds rwkind) { }

        /// <summary>已重载。</summary>
        /// <param name="stream">数据流</param>
        /// <param name="rwkind">序列化类型</param>
        protected override bool OnRead(Stream stream, RWKinds rwkind) { return true; }
    }
}