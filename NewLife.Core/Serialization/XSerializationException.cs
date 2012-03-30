using System;
using NewLife.Exceptions;

namespace NewLife.Serialization
{
    /// <summary>序列化异常</summary>
    [Serializable]
    public class XSerializationException : XException
    {
        private IObjectMemberInfo _Member;
        /// <summary>成员</summary>
        public IObjectMemberInfo Member
        {
            get { return _Member; }
            //set { _Member = value; }
        }

        private Object _Value;
        /// <summary>对象值</summary>
        public Object Value { get { return _Value; } set { _Value = value; } }

        #region 构造
        /// <summary>初始化</summary>
        /// <param name="member"></param>
        public XSerializationException(IObjectMemberInfo member) { _Member = member; }

        /// <summary>初始化</summary>
        /// <param name="member"></param>
        /// <param name="message"></param>
        public XSerializationException(IObjectMemberInfo member, String message) : base(message) { _Member = member; }

        /// <summary>初始化</summary>
        /// <param name="member"></param>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public XSerializationException(IObjectMemberInfo member, String message, Exception innerException)
            : base(message + (member != null ? "[Member:" + member.Name + "]" : null), innerException)
        {
            _Member = member;
        }

        /// <summary>初始化</summary>
        /// <param name="member"></param>
        /// <param name="innerException"></param>
        public XSerializationException(IObjectMemberInfo member, Exception innerException)
            : base((innerException != null ? innerException.Message : null) + (member != null ? "[Member:" + member.Name + "]" : null), innerException)
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