using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace NewLife.Serialization
{
    /// <summary>
    /// 写入字典项事件参数
    /// </summary>
    public class WriteDictionaryEventArgs : WriteIndexEventArgs
    {
        private DictionaryEntry _Value;
        /// <summary>对象</summary>
        public DictionaryEntry Value
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
        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="value">对象</param>
        /// <param name="type">对象类型</param>
        /// <param name="index"></param>
        /// <param name="callback"></param>
        public WriteDictionaryEventArgs(DictionaryEntry value, Type type, Int32 index, WriteObjectCallback callback)
            : base(index, callback)
        {
            Value = value;
            Type = type;
        }
        #endregion
    }
}