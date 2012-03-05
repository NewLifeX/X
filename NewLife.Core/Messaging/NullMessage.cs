using System.Xml.Serialization;

namespace NewLife.Messaging
{
    /// <summary>空消息。最短消息，只占一个字节</summary>
    public class NullMessage : Message
    {
        /// <summary>消息类型</summary>
        [XmlIgnore]
        public override MessageKind Kind { get { return MessageKind.Null; } }
    }
}