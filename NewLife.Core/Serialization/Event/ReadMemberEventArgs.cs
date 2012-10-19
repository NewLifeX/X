using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>读取成员事件参数</summary>
    public class ReadMemberEventArgs : ReaderEventArgs
    {
        private Object _Value;
        /// <summary>对象</summary>
        public Object Value
        {
            get { return _Value; }
            set { _Value = value; }
        }

        private Type _Type;
        /// <summary>对象类型</summary>
        public Type Type
        {
            get { return _Type; }
            set { _Type = value; }
        }

        private IObjectMemberInfo _Member;
        /// <summary>成员</summary>
        public IObjectMemberInfo Member
        {
            get { return _Member; }
            set { _Member = value; }
        }

        private Int32 _Index;
        /// <summary>成员序号</summary>
        public Int32 Index
        {
            get { return _Index; }
            set { _Index = value; }
        }

        #region 构造
        /// <summary>实例化</summary>
        /// <param name="value">对象</param>
        /// <param name="type">对象类型</param>
        /// <param name="member">成员</param>
        /// <param name="index">成员序号</param>
        /// <param name="callback"></param>
        public ReadMemberEventArgs(Object value, Type type, IObjectMemberInfo member, Int32 index, ReadObjectCallback callback)
            : base(callback)
        {
            Value = value;
            Type = type;
            Member = member;
            Index = index;
        }
        #endregion
    }
}