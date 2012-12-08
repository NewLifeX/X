using System;

namespace NewLife.Serialization
{
    /// <summary>写入对象事件参数</summary>
    public class WriteObjectEventArgs : WriterEventArgs
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
        public WriteObjectEventArgs(Object value, Type type, WriteObjectCallback callback)
            : base(callback)
        {
            Value = value;
            Type = type;
        }
        #endregion
    }
}