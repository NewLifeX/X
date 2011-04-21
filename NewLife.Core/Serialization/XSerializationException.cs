using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using NewLife.Exceptions;
using System.Runtime.Serialization;

namespace NewLife.Serialization
{
    /// <summary>
    /// 序列化异常
    /// </summary>
    [Serializable]
    public class XSerializationException : XException
    {
        private MemberInfo _Member;
        /// <summary>成员</summary>
        public MemberInfo Member
        {
            get { return _Member; }
            //set { _Member = value; }
        }

        #region 构造
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="member"></param>
        public XSerializationException(MemberInfo member) { _Member = member; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="member"></param>
        /// <param name="message"></param>
        public XSerializationException(MemberInfo member, String message) : base(message) { _Member = member; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="member"></param>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public XSerializationException(MemberInfo member, String message, Exception innerException)
            : base(message + (member != null ? "[" + member.MemberType + ":" + member.Name + "]" : null), innerException)
        {
            _Member = member;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="member"></param>
        /// <param name="innerException"></param>
        public XSerializationException(MemberInfo member, Exception innerException)
            : base((innerException != null ? innerException.Message : null) + (member != null ? "[" + member.MemberType + ":" + member.Name + "]" : null), innerException)
        {
            _Member = member;
        }

        ///// <summary>
        ///// 初始化
        ///// </summary>
        ///// <param name="info"></param>
        ///// <param name="context"></param>
        //protected XSerializationException(SerializationInfo info, StreamingContext context)
        //    : base(info, context)
        //{
        //    if (info != null && info.MemberCount > 0)
        //    {

        //    }
        //}
        #endregion
    }
}