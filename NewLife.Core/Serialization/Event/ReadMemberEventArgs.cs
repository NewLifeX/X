using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>
    /// 读取成员事件参数
    /// </summary>
    public class ReadMemberEventArgs : ReadIndexEventArgs
    {
        private Object _Value;
        /// <summary>对象</summary>
        public Object Value
        {
            get { return _Value; }
            set { _Value = value; }
        }

        private IObjectMemberInfo _Member;
        /// <summary>成员</summary>
        public IObjectMemberInfo Member
        {
            get { return _Member; }
            set { _Member = value; }
        }

        #region 构造
        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="value">对象</param>
        /// <param name="member">成员</param>
        /// <param name="index">成员序号</param>
        /// <param name="callback"></param>
        public ReadMemberEventArgs(Object value, IObjectMemberInfo member, Int32 index, ReadObjectCallback callback)
            : base(index, callback)
        {
            Value = value;
            Member = member;
        }
        #endregion
    }
}