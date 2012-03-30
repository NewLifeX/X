using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace NewLife.Serialization
{
    /// <summary>写入字典项事件参数</summary>>
    public class WriteDictionaryEventArgs : WriteIndexEventArgs
    {
        private DictionaryEntry _Value;
        /// <summary>对象</summary>
        public DictionaryEntry Value
        {
            get { return _Value; }
            set { _Value = value; }
        }

        private Type _KeyType;
        /// <summary>键类型</summary>
        public Type KeyType
        {
            get { return _KeyType; }
            set { _KeyType = value; }
        }

        private Type _ValueType;
        /// <summary>值类型</summary>
        public Type ValueType
        {
            get { return _ValueType; }
            set { _ValueType = value; }
        }

        #region 构造
        /// <summary>实例化</summary>>
        /// <param name="value">对象</param>
        /// <param name="keyType">键类型</param>
        /// <param name="valueType">值类型</param>
        /// <param name="index"></param>
        /// <param name="callback"></param>
        public WriteDictionaryEventArgs(DictionaryEntry value, Type keyType, Type valueType, Int32 index, WriteObjectCallback callback)
            : base(index, callback)
        {
            Value = value;
            KeyType = keyType;
            ValueType = valueType;
        }
        #endregion
    }
}