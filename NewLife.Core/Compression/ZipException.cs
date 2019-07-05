#if NET4
using System;
using System.Runtime.Serialization;
using NewLife;

namespace System.IO.Compression
{
    /// <summary>Zip异常</summary>
    [Serializable]
    public class ZipException : XException
    {
        #region 构造
        /// <summary>初始化</summary>
        public ZipException() { }

        /// <summary>初始化</summary>
        /// <param name="message"></param>
        public ZipException(String message) : base(message) { }

        /// <summary>初始化</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public ZipException(String format, params Object[] args) : base(format, args) { }

        /// <summary>初始化</summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ZipException(String message, Exception innerException) : base(message, innerException) { }

        /// <summary>初始化</summary>
        /// <param name="innerException"></param>
        public ZipException(Exception innerException) : base((innerException != null ? innerException.Message : null), innerException) { }

        ///// <summary>初始化</summary>
        ///// <param name="info"></param>
        ///// <param name="context"></param>
        //protected ZipException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        #endregion
    }
}
#endif