using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>
    /// 读写器基类
    /// </summary>
    public abstract class ReaderWriterBase : NewLife.DisposeBase, IReaderWriter
    {
        #region 属性
        private Encoding _Encoding;
        /// <summary>字符串编码</summary>
        public virtual Encoding Encoding
        {
            get { return _Encoding ?? (_Encoding = Encoding.UTF8); }
            set { _Encoding = value; }
        }

        private Int32 _Depth;
        /// <summary>层次深度</summary>
        public Int32 Depth
        {
            get { return _Depth; }
            set { _Depth = value; }
        }
        #endregion

        #region 获取成员
        /// <summary>
        /// 获取需要序列化的成员
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <returns>需要序列化的成员</returns>
        public IObjectMemberInfo[] GetMembers(Type type, Object value)
        {
            if (type == null) throw new ArgumentNullException("type");

            IObjectMemberInfo[] mis = OnGetMembers(type, value);

            if (OnGotMembers != null)
            {
                EventArgs<Type, Object, IObjectMemberInfo[]> e = new EventArgs<Type, Object, IObjectMemberInfo[]>(type, value, mis);
                OnGotMembers(this, e);
                mis = e.Arg3;
            }

            return mis;
        }

        /// <summary>
        /// 获取需要序列化的成员（属性或字段）
        /// </summary>
        /// <param name="type">指定类型</param>
        /// <param name="value">对象</param>
        /// <returns>需要序列化的成员</returns>
        protected virtual IObjectMemberInfo[] OnGetMembers(Type type, Object value)
        {
            if (type == null) throw new ArgumentNullException("type");

            return ObjectInfo.GetMembers(type, value, false);
        }

        /// <summary>
        /// 获取指定类型中需要序列化的成员时触发。使用者可以修改、排序要序列化的成员。
        /// </summary>
        public event EventHandler<EventArgs<Type, Object, IObjectMemberInfo[]>> OnGotMembers;
        #endregion

        #region 对象默认值
        /// <summary>
        /// 判断一个对象的某个成员是否默认值
        /// </summary>
        /// <param name="value"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        internal static Boolean IsDefault(Object value, IObjectMemberInfo member)
        {
            if (value == null) return false;

            Object def = ObjectInfo.GetDefaultObject(value.GetType());

            return Object.Equals(member[value], member[def]);
        }
        #endregion

        #region 释放
        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);
        }
        #endregion
    }
}