using System;
using System.Xml.Serialization;

namespace NewLife.Messaging
{
    /// <summary>指定类型的对象消息</summary>
    /// <remarks>
    /// 一般用于打包单个对象，理论上，这是一个万能消息。
    /// 需要注意的是：本消息的设计，允许通讯双方使用不同的类，只要这两个类继承相同的接口或者抽象类。
    /// </remarks>
    public class EntityMessage : Message
    {
        /// <summary>消息类型</summary>
        [XmlIgnore]
        public override MessageKind Kind { get { return MessageKind.Entity; } }

        private Type _Type;
        /// <summary>实体类型。可以是接口或抽象类型（要求对象容器能识别）</summary>
        public Type Type { get { return _Type; } set { _Type = value; } }

        private Object _Value;
        /// <summary>对象值</summary>
        public Object Value { get { return _Value; } set { _Value = value; if (value != null && Type == null)Type = value.GetType(); } }
    }
}