using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>读写器事件参数</summary>>
    public class ReaderWriterEventArgs : EventArgs
    {
        #region 属性
        private Boolean _Success;
        /// <summary>是否成功。</summary>
        public Boolean Success
        {
            get { return _Success; }
            set { _Success = value; }
        }

        //private Object _Value;
        ///// <summary>对象</summary>
        //public Object Value
        //{
        //    get { return _Value; }
        //    set { _Value = value; }
        //}

        //private Type _Type;
        ///// <summary>对象类型</summary>
        //public Type Type
        //{
        //    get { return _Type; }
        //    set { _Type = value; }
        //}
        #endregion

        #region 构造
        ///// <summary>
        ///// 实例化
        ///// </summary>
        //public ReaderWriterEventArgs() { }

        ///// <summary>
        ///// 实例化
        ///// </summary>
        //public ReaderWriterEventArgs(Boolean success)
        //{
        //    Success = success;
        //}

        ///// <summary>
        ///// 实例化
        ///// </summary>
        ///// <param name="value"></param>
        ///// <param name="type"></param>
        ///// <param name="callback"></param>
        //public ReaderWriterEventArgs(Object value, Type type, TCallback callback)
        //{
        //    Value = value;
        //    Type = type;
        //    Callback = callback;
        //}

        ///// <summary>
        ///// 实例化
        ///// </summary>
        ///// <param name="value"></param>
        ///// <param name="type"></param>
        ///// <param name="callback"></param>
        ///// <param name="success"></param>
        //public ReaderWriterEventArgs(Object value, Type type, TCallback callback, Boolean success)
        //    : this(value, type, callback)
        //{
        //    Success = success;
        //}
        #endregion

        #region 方法
        ///// <summary>
        ///// 设置成员
        ///// </summary>
        ///// <param name="member"></param>
        ///// <returns></returns>
        //public ReaderWriterEventArgs SetMember(IObjectMemberInfo member)
        //{
        //    Member = member;
        //    return this;
        //}

        ///// <summary>
        ///// 设置成员序号
        ///// </summary>
        ///// <param name="index"></param>
        ///// <returns></returns>
        //public ReaderWriterEventArgs SetIndex(Int32 index)
        //{
        //    Index = index;
        //    return this;
        //}
        #endregion
    }
}