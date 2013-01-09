using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using NewLife.Serialization;

namespace NewLife.Messaging
{
    /// <summary>指定类型的实体对象数组消息</summary>
    /// <remarks>
    /// 有些列表对象不适合直接序列化，并且不方便每次都进行转换，（如XCode的EntityList），此时适合用实体数组消息。
    /// 实体对象个数由<see cref="Values"/>决定，以编码整数来存储。
    /// 不写长度，所以<see cref="Message"/>为空时后面不能有其它包
    /// </remarks>
    public class EntitiesMessage : Message, IAccessor
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
        /// <summary>实体列表</summary>
        public IList Values
        {
            get { return _Values ?? (_Values = new List<Object>()); }
            set
            {
                _Values = value;
                if (value != null && value.Count > 0)
                {
                    foreach (var item in value)
                    {
                        if (item != null)
                        {
                            _Type = item.GetType();
                            break;
                        }
                    }
                }
            }
        }

        #region IAccessor 成员
        Boolean IAccessor.Read(IReader reader)
        {
            reader.Depth++;

            Object v = null;
            if (!reader.ReadObject(typeof(Type), ref v, null)) return false;
            Type = v as Type;
            if (Type != null)
            {
                var lt = typeof(List<>).MakeGenericType(Type);

                v = null;
                if (!reader.ReadObject(lt, ref v, null)) return false;
                Values = v as IList;
            }
            reader.Depth--;
            return true;
        }

        Boolean IAccessor.ReadComplete(IReader reader, Boolean success) { return success; }

        Boolean IAccessor.Write(IWriter writer)
        {
            writer.Depth++;
            writer.WriteObject(Type);
            if (Type != null)
            {
                var lt = typeof(List<>).MakeGenericType(Type);
                writer.WriteObject(Values, lt, null);
            }
            writer.Depth--;
            return true;
        }

        Boolean IAccessor.WriteComplete(IWriter writer, Boolean success) { return success; }
        #endregion

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