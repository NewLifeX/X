using System.Xml.Serialization;

namespace NewLife.Messaging
{
    /// <summary>空消息</summary>
    public class NullMessage : Message
    {
        /// <summary>消息类型</summary>
        [XmlIgnore]
        public override MessageKind Kind { get { return MessageKind.Null; } }
    }
}