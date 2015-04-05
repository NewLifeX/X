using System;
using System.Runtime.Serialization;
using NewLife;

namespace XCode
{
    /// <summary>实体异常</summary>
    public class EntityException : XException
    {
          #region 构造
        /// <summary>初始化</summary>
        public EntityException() { }

        /// <summary>初始化</summary>
        /// <param name="message"></param>
        public EntityException(String message) : base(message) { }

        /// <summary>初始化</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public EntityException(String format, params Object[] args) : base(format, args) { }

        /// <summary>初始化</summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public EntityException(String message, Exception innerException) : base(message, innerException) { }

        /// <summary>初始化</summary>
        /// <param name="innerException"></param>
        public EntityException(Exception innerException) : base((innerException != null ? innerException.Message : null), innerException) { }

        /// <summary>初始化</summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected EntityException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        #endregion
  }
}