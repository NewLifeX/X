using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Messaging;
using System.Xml.Serialization;

namespace NewLife.Remoting
{
    /// <summary>
    /// 实体消息
    /// </summary>
    public class EntityMessage : RemotingMessage
    {
        #region 属性
        /// <summary>消息类型</summary>
        public override RemotingMessageType MessageType
        {
            get { return RemotingMessageType.Entity; }
        }

        private String _TypeName;
        /// <summary>类型名</summary>
        public String TypeName
        {
            get { return _TypeName; }
            set { _TypeName = value; }
        }

        private Type _EntityType;
        /// <summary>实体类型</summary>
        [XmlIgnore]
        public Type EntityType
        {
            get { return _EntityType; }
            set { _EntityType = value; }
        }

        private Object _Entity;
        /// <summary>实体对象</summary>
        public Object Entity
        {
            get { return _Entity; }
            set { _Entity = value; }
        }
        #endregion
    }
}