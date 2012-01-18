//using System;
//using System.Collections.Generic;
//using System.Text;
//using NewLife.Messaging;
//using System.Xml.Serialization;
//using NewLife.IO;
//using NewLife.Reflection;

//namespace NewLife.Remoting
//{
//    /// <summary>
//    /// 实体消息
//    /// </summary>
//    public class EntityMessage : RemotingMessage
//    {
//        #region 属性
//        /// <summary>消息类型</summary>
//        public override RemotingMessageType MessageType
//        {
//            get { return RemotingMessageType.Entity; }
//        }

//        //private String _TypeName;
//        ///// <summary>类型名</summary>
//        //public String TypeName
//        //{
//        //    get { return _TypeName; }
//        //    set { _TypeName = value; }
//        //}

//        //[NonSerialized]
//        //private Type _EntityType;
//        ///// <summary>实体类型</summary>
//        //[XmlIgnore]
//        //public Type EntityType
//        //{
//        //    get
//        //    {
//        //        if (_EntityType == null && !String.IsNullOrEmpty(TypeName)) _EntityType = Type.GetType(TypeName);
//        //        return _EntityType;
//        //    }
//        //    set
//        //    {
//        //        _EntityType = value;
//        //        if (value != null) TypeName = value.FullName;
//        //    }
//        //}

//        private Type _EntityType;
//        /// <summary>实体类型</summary>
//        public Type EntityType
//        {
//            get { return _EntityType; }
//            set { _EntityType = value; }
//        }

//        private Object _Entity;
//        /// <summary>实体对象</summary>
//        public Object Entity
//        {
//            get { return _Entity; }
//            set { _Entity = value; }
//        }
//        #endregion

//        #region 构造
//        /// <summary>
//        /// 实例化
//        /// </summary>
//        public EntityMessage() { }

//        /// <summary>
//        /// 使用实体对象来实例化
//        /// </summary>
//        /// <param name="entity"></param>
//        public EntityMessage(Object entity)
//        {
//            if (entity != null)
//            {
//                Entity = entity;
//                EntityType = entity.GetType();
//            }
//        }
//        #endregion

//        #region 重载
//        ///// <summary>
//        ///// 尝试读取目标对象指定成员的值，处理基础类型、特殊类型、基础类型数组、特殊类型数组，通过委托方法处理成员
//        ///// </summary>
//        ///// <remarks>
//        ///// 简单类型在value中返回，复杂类型直接填充target；
//        ///// </remarks>
//        ///// <param name="reader">读取器</param>
//        ///// <param name="target">目标对象</param>
//        ///// <param name="member">成员</param>
//        ///// <param name="type">成员类型，以哪一种类型读取</param>
//        ///// <param name="encodeInt">是否编码整数</param>
//        ///// <param name="allowNull">是否允许空</param>
//        ///// <param name="isProperty">是否处理属性</param>
//        ///// <param name="value">成员值</param>
//        ///// <param name="callback">处理成员的方法</param>
//        ///// <returns>是否读取成功</returns>
//        //protected override Boolean ReadMember(BinaryReaderX reader, object target, MemberInfoX member, Type type, bool encodeInt, bool allowNull, bool isProperty, out object value, BinaryReaderX.ReadCallback callback)
//        //{
//        //    if (member.Member.Name != "_Entity")
//        //        return base.ReadMember(reader, target, member, type, encodeInt, allowNull, isProperty, out value, callback);
//        //    else
//        //        return base.ReadMember(reader, target, member, EntityType, encodeInt, allowNull, isProperty, out value, callback);
//        //}
//        #endregion
//    }
//}