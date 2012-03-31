using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace NewLife.Messaging
{
    /// <summary>指定类型的实体对象数组消息</summary>
    /// <remarks>
    /// 实体对象个数由<see cref="Values"/>决定，以编码整数来存储。
    /// </remarks>
    public class EntitiesMessage : Message
    {
        /// <summary>消息类型</summary>
        [XmlIgnore]
        public override MessageKind Kind { get { return MessageKind.Entities; } }

        private Type _Type;
        /// <summary>实体类型。可以是接口或抽象类型（要求对象容器能识别）</summary>
        public Type Type
        {
            get
            {
                if (_Type == null && _Values != null && _Values.Count > 0) _Type = _Values[0].GetType();
                return _Type;
            }
            set { _Type = value; }
        }

        private IList _Values;
        /// <summary>对象值</summary>
        public IList Values { get { return _Values ?? (_Values = new List<Object>()); } set { _Values = value; if (value != null && value.Count > 0)_Type = value[0].GetType(); } }

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            var vs = Values;
            if (vs != null)
                return String.Format("{0} Type={1} Count={2}", base.ToString(), Type, vs.Count);
            else
                return base.ToString();
        }
        #endregion
    }
}