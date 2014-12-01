using System;
using System.Runtime.Serialization;
using NewLife.Exceptions;

namespace NewLife.Net
{
    /// <summary>网络异常</summary>
    [Serializable]
    public class NetException : XException
    {
        #region 构造
        /// <summary>初始化</summary>
        public NetException() { }

        /// <summary>初始化</summary>
        /// <param name="message"></param>
        public NetException(String message) : base(message) { }

        /// <summary>初始化</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public NetException(String format, params Object[] args) : base(format, args) { }

        /// <summary>初始化</summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public NetException(String message, Exception innerException) : base(message, innerException) { }

        /// <summary>初始化</summary>
        /// <param name="innerException"></param>
        public NetException(Exception innerException) : base((innerException != null ? innerException.Message : null), innerException) { }

        /// <summary>初始化</summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected NetException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        #endregion
    }

    /// <summary>异常事件参数</summary>
    public class ExceptionEventArgs : EventArgs
    {
        private Exception _Exception;
        /// <summary>异常</summary>
        public Exception Exception { get { return _Exception; } set { _Exception = value; } }
    }
}