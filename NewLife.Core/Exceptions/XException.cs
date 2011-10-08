using System;
using System.Runtime.Serialization;

namespace NewLife.Exceptions
{
    /// <summary>
    /// X组件异常
    /// </summary>
    [Serializable]
    public class XException : ApplicationException
    {
        #region 构造
        /// <summary>
        /// 初始化
        /// </summary>
        public XException() { }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="message"></param>
        public XException(String message) : base(message) { }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public XException(String format, params Object[] args) : base(String.Format(format, args)) { }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public XException(String message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="innerException"></param>
        public XException(Exception innerException) : base((innerException != null ? innerException.Message : null), innerException) { }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected XException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        #endregion
    }
}