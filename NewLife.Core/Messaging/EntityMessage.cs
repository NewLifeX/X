using System;
using System.Collections;
using System.Xml.Serialization;
using NewLife.Serialization;

namespace NewLife.Messaging
{
    /// <summary>指定类型的对象消息</summary>
    /// <remarks>
    /// 一般用于打包单个对象，理论上，这是一个万能消息。
    /// 需要注意的是：本消息的设计，允许通讯双方使用不同的类，只要这两个类继承相同的接口或者抽象类。
    /// </remarks>
    public class EntityMessage : Message, IAccessor
    {
        /// <summary>消息类型</summary>
        [XmlIgnore]
        public override MessageKind Kind { get { return MessageKind.Entity; } }

        private Type _Type;
        /// <summary>实体类型。可以是接口或抽象类型（要求对象容器能识别）</summary>
        public Type Type { get { if (_Type == null && _Value != null)_Type = _Value.GetType(); return _Type; } set { _Type = value; } }

        private Object _Value;
        /// <summary>对象值</summary>
        public Object Value { get { return _Value; } set { _Value = value; if (value != null) Type = value.GetType(); } }

        #region IAccessor 成员
        Boolean IAccessor.Read(IReader reader)
        {
            reader.OnMemberReading += new EventHandler<ReadMemberEventArgs>(reader_OnMemberReading);
            return false;
        }

        void reader_OnMemberReading(object sender, ReadMemberEventArgs e)
        {
            var reader = sender as IReader;
            if (reader.CurrentObject == this && e.Member.Name == "_Value")
            {
                e.Type = Type;
                reader.OnMemberReading -= new EventHandler<ReadMemberEventArgs>(reader_OnMemberReading);
            }
        }

        Boolean IAccessor.ReadComplete(IReader reader, Boolean success) { return success; }

        Boolean IAccessor.Write(IWriter writer)
        {
            writer.OnMemberWriting += new EventHandler<WriteMemberEventArgs>(writer_OnMemberWriting);
            return false;
        }

        void writer_OnMemberWriting(object sender, WriteMemberEventArgs e)
        {
            var writer = sender as IWriter;
            if (writer.CurrentObject == this && e.Member.Name == "_Value")
            {
                e.Type = Type; 
                writer.OnMemberWriting -= new EventHandler<WriteMemberEventArgs>(writer_OnMemberWriting);
            }
        }

        Boolean IAccessor.WriteComplete(IWriter writer, Boolean success) { return success; }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            var type = Type;
            if (type != null && typeof(IList).IsAssignableFrom(type))
                return String.Format("{0} {1}[{2}]", base.ToString(), type, Value == null ? 0 : (Value as IList).Count);
            else if (type == typeof(String))
            {
                var str = "" + Value;
                return String.Format("{0} {1}", base.ToString(), str.Length < 50 ? str : str.Substring(0, 47) + "...");
            }
            else
                return String.Format("{0} {1}", base.ToString(), Value);
        }
        #endregion
    }
}