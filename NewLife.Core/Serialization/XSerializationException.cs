using System;

namespace NewLife.Serialization
{
    /// <summary>序列化异常</summary>
    [Serializable]
    public class XSerializationException : XException
    {
        private String _Member;
        /// <summary>成员</summary>
        public String Member { get { return _Member; } }

        private Object _Value;
        /// <summary>对象值</summary>
        public Object Value { get { return _Value; } set { _Value = value; } }

        #region 构造
        /// <summary>初始化</summary>
        /// <param name="member"></param>
        public XSerializationException(String member) { _Member = member; }

        /// <summary>初始化</summary>
        /// <param name="member"></param>
        /// <param name="message"></param>
        public XSerializationException(String member, String message) : base(message) { _Member = member; }

        /// <summary>初始化</summary>
        /// <param name="member"></param>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public XSerializationException(String member, String message, Exception innerException)
            : base(message + (member != null ? "[Member:" + member + "]" : null), innerException)
        {
            _Member = member;
        }

        /// <summary>初始化</summary>
        /// <param name="member"></param>
        /// <param name="innerException"></param>
        public XSerializationException(String member, Exception innerException)
            : base((innerException != null ? innerException.Message : null) + (member != null ? "[Member:" + member + "]" : null), innerException)
        {
            _Member = member;
        }
        #endregion
    }
}