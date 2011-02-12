using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Messaging;
using System.Xml.Serialization;
using NewLife.IO;
using NewLife.Reflection;

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

        //private String _TypeName;
        ///// <summary>类型名</summary>
        //public String TypeName
        //{
        //    get { return _TypeName; }
        //    set { _TypeName = value; }
        //}

        //[NonSerialized]
        //private Type _EntityType;
        ///// <summary>实体类型</summary>
        //[XmlIgnore]
        //public Type EntityType
        //{
        //    get
        //    {
        //        if (_EntityType == null && !String.IsNullOrEmpty(TypeName)) _EntityType = Type.GetType(TypeName);
        //        return _EntityType;
        //    }
        //    set
        //    {
        //        _EntityType = value;
        //        if (value != null) TypeName = value.FullName;
        //    }
        //}

        private Type _EntityType;
        /// <summary>实体类型</summary>
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

        #region 构造
        /// <summary>
        /// 实例化
        /// </summary>
        public EntityMessage() { }

        /// <summary>
        /// 使用实体对象来实例化
        /// </summary>
        /// <param name="entity"></param>
        public EntityMessage(Object entity)
        {
            if (entity != null)
            {
                Entity = entity;
                EntityType = entity.GetType();
            }
        }
        #endregion

        #region 重载
        /// <summary>
        /// 读取成员
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="target"></param>
        /// <param name="member"></param>
        /// <param name="encodeInt"></param>
        /// <param name="allowNull"></param>
        /// <param name="isProperty"></param>
        /// <param name="value"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        protected override bool ReadMember(BinaryReaderX reader, object target, MemberInfoX member, bool encodeInt, bool allowNull, bool isProperty, out object value, BinaryReaderX.ReadCallback callback)
        {
            return base.ReadMember(reader, target, member, encodeInt, allowNull, isProperty, out value, callback);
        }
        #endregion
    }
}