using System;
using System.Runtime.Serialization;
using NewLife.Exceptions;

namespace XCode.Exceptions
{
    /// <summary>
    /// XCode异常
    /// </summary>
    [Serializable]
    public class XCodeException : XException
    {
        #region 构造
        /// <summary>
        /// 初始化
        /// </summary>
        public XCodeException() { }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="message"></param>
        public XCodeException(String message) : base(message) { }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public XCodeException(String format, params Object[] args) : base(format, args) { }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public XCodeException(String message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="innerException"></param>
        public XCodeException(Exception innerException) : base((innerException != null ? innerException.Message : null), innerException) { }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected XCodeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        #endregion
    }
}