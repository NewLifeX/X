using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>写入枚举项事件参数</summary>>
    public class WriteItemEventArgs : WriteIndexEventArgs
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

        #region 构造
        /// <summary>实例化</summary>>
        /// <param name="value">对象</param>
        /// <param name="type">对象类型</param>
        /// <param name="index">序号</param>
        /// <param name="callback"></param>
        public WriteItemEventArgs(Object value, Type type, Int32 index, WriteObjectCallback callback)
            : base(index, callback)
        {
            Value = value;
            Type = type;
        }
        #endregion
    }
}