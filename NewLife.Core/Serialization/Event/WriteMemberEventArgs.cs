using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>写入成员事件参数</summary>
    public class WriteMemberEventArgs : WriterEventArgs
    {
        private String _Name;
        /// <summary>成员名称</summary>
        public String Name { get { return _Name; } set { _Name = value; } }

        private Object _Value;
        /// <summary>对象</summary>
        public Object Value { get { return _Value; } set { _Value = value; } }

        private Type _Type;
        /// <summary>对象类型</summary>
        public Type Type { get { return _Type; } set { _Type = value; } }

        private Int32 _Index;
        /// <summary>成员序号</summary>
        public Int32 Index { get { return _Index; } set { _Index = value; } }

        #region 构造
        /// <summary>实例化</summary>
        /// <param name="name">成员名字</param>
        /// <param name="value">对象</param>
        /// <param name="type">对象类型</param>
        /// <param name="index">成员序号</param>
        /// <param name="callback"></param>
        public WriteMemberEventArgs(String name, Object value, Type type, Int32 index, WriteObjectCallback callback)
            : base(callback)
        {
            Name = name;
            Value = value;
            Type = type;
            Index = index;
        }
        #endregion
    }
}