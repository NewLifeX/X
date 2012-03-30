using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>读取对象事件参数</summary>
    public class ReadObjectEventArgs : ReaderEventArgs
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
        /// <summary>实例化</summary>
        /// <param name="value">对象</param>
        /// <param name="type">对象类型</param>
        /// <param name="callback"></param>
        public ReadObjectEventArgs(Object value, Type type, ReadObjectCallback callback)
            : base(callback)
        {
            Value = value;
            Type = type;
        }
        #endregion
}
}